using UnityEngine;

public enum DOStatus
{
    Safe,
    Danger,
    Critical,
    Zero
}

public class RasWaterSimulator : MonoBehaviour
{
    private const float TEMP_RANDOM_INTERVAL = 180f;
    private const float TEMP_LOW_FEED_THRESHOLD = 21f;
    private const float TEMP_CHAIN_THRESHOLD = 27f;
    private const float TEMP_CRITICAL_THRESHOLD = 31f;

    private const float SAL_LOW_THRESHOLD = 32f;
    private const float SAL_HIGH_THRESHOLD = 35f;

    private const float PH_LOW_FEED_THRESHOLD = 6f;
    private const float PH_NH3_PRODUCTION_THRESHOLD = 8f;

    private const float NH3_SAFE_MAX = 0.1f;
    private const float NH3_DANGER_MAX = 0.5f;

    private const float DO_DANGER_THRESHOLD = 5f;
    private const float DO_CRITICAL_THRESHOLD = 4f;

    private const float NH3_PER_FISH_PER_SECOND = 0.0001f;
    private const float NH3_DEAD_FISH_PER_MIN = 0.05f;
    private const float NH3_PER_FED_PELLET = 0.002f;

    private const float DO_LOSS_SAL_HIGH_PER_SECOND = 0.002f;
    private const float DO_LOSS_TEMP_PER_DEG_PER_SECOND = 0.5f;
    private const float DO_LOSS_TEMP_CRITICAL_PER_SECOND = 0.017f;
    private const float DO_LOSS_NH3_DANGER_PER_FISH_PER_SECOND = 0.001f;
    private const float DO_LOSS_NH3_CRITICAL_PER_FISH_PER_SECOND = 0.002f;

    private const float SAL_RISE_HIGH_TEMP_PER_SECOND = 0.006f;
    private const float PH_RISE_HIGH_TEMP_PER_SECOND = 0.001f;
    private const float SAL_RISE_CRITICAL_TEMP_PER_SECOND = 0.008f;
    private const float PH_RISE_CRITICAL_TEMP_PER_SECOND = 0.004f;

    private const float FOOD_LOAD_DECAY_PER_SEC = 1f / 120f;
    private const float FOOD_NH3_PER_UNIT_PER_SEC = 0.0005f;
    private const float FOOD_PH_DROP_PER_UNIT_PER_SEC = 0.00004f;

    private const float FEED_EFFICIENCY_LOW = 0.40f;
    private const float FEED_NORMAL = 1.00f;

    private WaterQualityState water;
    private bool coolerActive;
    private bool fishAmmoniaProductionActive;
    private float foodLoad;
    private float temperatureRandomTimer;
    public float FoodLoad => foodLoad;

    public void Initialize(WaterQualityState waterState, bool isCoolerInstalled)
    {
        water = waterState;
        coolerActive = isCoolerInstalled;
        fishAmmoniaProductionActive = false;
        foodLoad = 0f;
        temperatureRandomTimer = 0f;
    }

    public void SetCoolerActive(bool active) => coolerActive = active;

    public void AddFoodLoad(float amount)
    {
        if (amount <= 0f) return;
        foodLoad += amount;
    }

    public void ConsumeFoodLoad(float amount)
    {
        if (amount <= 0f) return;
        foodLoad = Mathf.Max(0f, foodLoad - amount);
    }

    public void RegisterFedFish(float consumedFoodUnits = 1f)
    {
        fishAmmoniaProductionActive = true;
        AddAmmonia(NH3_PER_FED_PELLET * Mathf.Max(0f, consumedFoodUnits));
    }

    public void StartFishAmmoniaProduction() => fishAmmoniaProductionActive = true;

    public void StopFishAmmoniaProduction() => fishAmmoniaProductionActive = false;

    public void StopAmmoniaProduction()
    {
        fishAmmoniaProductionActive = false;
        foodLoad = 0f;
    }

    public void AddAmmonia(float amount)
    {
        if (water == null || amount <= 0f) return;
        water.ammonia += amount;
        water.Clamp();
    }

    public void Tick(float dt, int livingFishCount, int deadFishCount)
    {
        if (water == null) return;

        TickTemperatureRandomWalk(dt);
        TickTemperatureEffects(dt);
        TickSalinityEffects(dt);
        TickAmmoniaFromFish(dt, livingFishCount);
        TickAmmoniaFromDeadFish(dt, deadFishCount);
        TickFoodDecomposition(dt);
        TickDOFromAmmonia(dt, livingFishCount);

        water.Clamp();
    }

    public DOStatus GetDOStatus()
    {
        if (water == null) return DOStatus.Safe;
        if (water.oxygen <= 0f) return DOStatus.Zero;
        if (water.oxygen < DO_CRITICAL_THRESHOLD) return DOStatus.Critical;
        if (water.oxygen < DO_DANGER_THRESHOLD) return DOStatus.Danger;
        return DOStatus.Safe;
    }

    public float GetFeedEfficiency()
    {
        if (water == null) return FEED_NORMAL;
        return IsFeedEffectivenessReduced() ? FEED_EFFICIENCY_LOW : FEED_NORMAL;
    }

    public bool IsAmmoniaProductionDoubled() => water != null && water.ph > PH_NH3_PRODUCTION_THRESHOLD;

    public bool IsFeedEffectivenessReduced()
    {
        if (water == null) return false;
        return water.ph < PH_LOW_FEED_THRESHOLD || water.temperature < TEMP_LOW_FEED_THRESHOLD;
    }

    public float GetHungerDecayMultiplier()
    {
        return IsFeedEffectivenessReduced() ? 2f : 1f;
    }

    private void TickTemperatureRandomWalk(float dt)
    {
        temperatureRandomTimer += dt;
        while (temperatureRandomTimer >= TEMP_RANDOM_INTERVAL)
        {
            temperatureRandomTimer -= TEMP_RANDOM_INTERVAL;
            float delta = Random.Range(-10, 11) / 10f;
            water.temperature = Mathf.Round((water.temperature + delta) * 10f) / 10f;
        }
    }

    private void TickTemperatureEffects(float dt)
    {
        if (water.temperature <= TEMP_CHAIN_THRESHOLD)
            return;

        if (water.temperature > TEMP_CRITICAL_THRESHOLD)
        {
            water.oxygen -= DO_LOSS_TEMP_CRITICAL_PER_SECOND * dt;
            water.salinity += SAL_RISE_CRITICAL_TEMP_PER_SECOND * dt;
            water.ph += PH_RISE_CRITICAL_TEMP_PER_SECOND * dt;
            return;
        }

        float excessDeg = Mathf.Max(0f, water.temperature - TEMP_CHAIN_THRESHOLD);
        if (excessDeg > 0f)
            water.oxygen -= DO_LOSS_TEMP_PER_DEG_PER_SECOND * excessDeg * dt;

        water.salinity += SAL_RISE_HIGH_TEMP_PER_SECOND * dt;
        water.ph += PH_RISE_HIGH_TEMP_PER_SECOND * dt;
    }

    private void TickSalinityEffects(float dt)
    {
        if (water.salinity > SAL_HIGH_THRESHOLD)
            water.oxygen -= DO_LOSS_SAL_HIGH_PER_SECOND * dt;
    }

    private void TickAmmoniaFromFish(float dt, int livingFishCount)
    {
        if (!fishAmmoniaProductionActive || livingFishCount <= 0) return;

        float productionMultiplier = IsAmmoniaProductionDoubled() ? 2f : 1f;
        water.ammonia += NH3_PER_FISH_PER_SECOND * productionMultiplier * livingFishCount * dt;
    }

    private void TickAmmoniaFromDeadFish(float dt, int deadFishCount)
    {
        if (deadFishCount <= 0) return;
        float productionMultiplier = IsAmmoniaProductionDoubled() ? 2f : 1f;
        water.ammonia += (NH3_DEAD_FISH_PER_MIN / 60f) * productionMultiplier * deadFishCount * dt;
    }

    private void TickFoodDecomposition(float dt)
    {
        if (foodLoad <= 0f) return;

        water.ammonia += FOOD_NH3_PER_UNIT_PER_SEC * foodLoad * dt;
        water.ph -= FOOD_PH_DROP_PER_UNIT_PER_SEC * foodLoad * dt;
        foodLoad = Mathf.Max(0f, foodLoad - FOOD_LOAD_DECAY_PER_SEC * foodLoad * dt);
    }

    private void TickDOFromAmmonia(float dt, int livingFishCount)
    {
        float measuredAmmonia = water.ammonia;
        int oxygenDemandLoad = Mathf.Max(1, livingFishCount);

        if (measuredAmmonia > NH3_DANGER_MAX)
        {
            water.oxygen -= DO_LOSS_NH3_CRITICAL_PER_FISH_PER_SECOND * oxygenDemandLoad * dt;
        }
        else if (measuredAmmonia > NH3_SAFE_MAX)
        {
            water.oxygen -= DO_LOSS_NH3_DANGER_PER_FISH_PER_SECOND * oxygenDemandLoad * dt;
        }
    }
}
