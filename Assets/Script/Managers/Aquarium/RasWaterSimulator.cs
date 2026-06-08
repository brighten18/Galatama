using UnityEngine;

public enum DOStatus
{
    Safe,
    Danger,
    Critical
}

public class RasWaterSimulator : MonoBehaviour
{
    private const float TEMP_RANDOM_INTERVAL = 300f;
    private const float TEMP_CHAIN_THRESHOLD = 27f;
    private const float TEMP_DO_LOSS_THRESHOLD = 28f;

    private const float SAL_LOW_THRESHOLD = 32f;
    private const float SAL_HIGH_THRESHOLD = 35f;

    private const float PH_LOW_FEED_THRESHOLD = 6.5f;
    private const float PH_NH3_TOXIC_THRESHOLD = 8.5f;

    private const float NH3_SAFE_MAX = 0.1f;
    private const float NH3_DANGER_MAX = 0.5f;

    private const float DO_DANGER_THRESHOLD = 6f;
    private const float DO_CRITICAL_THRESHOLD = 5f;

    private const float NH3_PER_FISH_PER_5_MIN = 0.02f;
    private const float NH3_LOW_SAL_BONUS_PER_FISH_PER_5_MIN = 0.02f;
    private const float NH3_DEAD_FISH_PER_MIN = 0.05f;
    private const float NH3_AFTER_FEEDING_PER_FISH_PER_5_MIN = 0.03f;
    private const float FED_FISH_AMMONIA_DURATION = 300f;

    private const float DO_LOSS_SAL_HIGH_PER_2_MIN = 0.2f;
    private const float DO_LOSS_TEMP_PER_DEG_PER_30_SEC = 0.5f;
    private const float DO_LOSS_NH3_SAFE_PER_4_MIN = 0.01f;
    private const float DO_LOSS_NH3_DANGER_PER_FISH_PER_5_MIN = 0.02f;
    private const float DO_LOSS_NH3_CRITICAL_PER_FISH_PER_5_MIN = 0.04f;

    private const float SAL_RISE_HIGH_TEMP_PER_3_MIN = 1f;
    private const float PH_RISE_HIGH_TEMP_PER_3_MIN = 0.1f;

    private const float FOOD_LOAD_DECAY_PER_SEC = 1f / 120f;
    private const float FOOD_NH3_PER_UNIT_PER_SEC = 0.0002f;
    private const float FOOD_PH_DROP_PER_UNIT_PER_SEC = 0.00004f;

    private const float FEED_EFFICIENCY_LOW = 0.40f;
    private const float FEED_NORMAL = 1.00f;

    private WaterQualityState water;
    private bool coolerActive;
    private float foodLoad;
    private float temperatureRandomTimer;
    private readonly System.Collections.Generic.List<float> fedFishAmmoniaTimers =
        new System.Collections.Generic.List<float>();

    public float FoodLoad => foodLoad;

    public void Initialize(WaterQualityState waterState, bool isCoolerInstalled)
    {
        water = waterState;
        coolerActive = isCoolerInstalled;
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

    public void RegisterFedFish()
    {
        fedFishAmmoniaTimers.Add(FED_FISH_AMMONIA_DURATION);
    }

    public void Tick(float dt, int livingFishCount, int deadFishCount)
    {
        if (water == null) return;

        TickTemperatureRandomWalk(dt);
        TickTemperatureEffects(dt);
        TickSalinityEffects(dt);
        TickAmmoniaFromFish(dt, livingFishCount);
        TickAmmoniaFromDeadFish(dt, deadFishCount);
        TickAmmoniaAfterFeeding(dt);
        TickFoodDecomposition(dt);
        TickDOFromAmmonia(dt, livingFishCount);

        water.Clamp();
    }

    public DOStatus GetDOStatus()
    {
        if (water == null) return DOStatus.Safe;
        if (water.oxygen < DO_CRITICAL_THRESHOLD) return DOStatus.Critical;
        if (water.oxygen < DO_DANGER_THRESHOLD) return DOStatus.Danger;
        return DOStatus.Safe;
    }

    public float GetFeedEfficiency()
    {
        if (water == null) return FEED_NORMAL;

        bool tooCold = water.temperature < 21f;
        bool lowPh = water.ph < PH_LOW_FEED_THRESHOLD;
        return (tooCold || lowPh) ? FEED_EFFICIENCY_LOW : FEED_NORMAL;
    }

    public bool IsNH3ToxicityDoubled() => water != null && water.ph > PH_NH3_TOXIC_THRESHOLD;

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

        float excessDeg = Mathf.Max(0f, water.temperature - TEMP_DO_LOSS_THRESHOLD);
        if (excessDeg > 0f)
            water.oxygen -= (DO_LOSS_TEMP_PER_DEG_PER_30_SEC / 30f) * excessDeg * dt;

        water.salinity += (SAL_RISE_HIGH_TEMP_PER_3_MIN / 180f) * dt;
        water.ph += (PH_RISE_HIGH_TEMP_PER_3_MIN / 180f) * dt;
    }

    private void TickSalinityEffects(float dt)
    {
        if (water.salinity > SAL_HIGH_THRESHOLD)
            water.oxygen -= (DO_LOSS_SAL_HIGH_PER_2_MIN / 120f) * dt;
    }

    private void TickAmmoniaFromFish(float dt, int livingFishCount)
    {
        if (livingFishCount <= 0) return;

        float ammoniaPerFish = NH3_PER_FISH_PER_5_MIN;
        if (water.salinity < SAL_LOW_THRESHOLD)
            ammoniaPerFish += NH3_LOW_SAL_BONUS_PER_FISH_PER_5_MIN;

        water.ammonia += (ammoniaPerFish / 300f) * livingFishCount * dt;
    }

    private void TickAmmoniaFromDeadFish(float dt, int deadFishCount)
    {
        if (deadFishCount <= 0) return;
        water.ammonia += (NH3_DEAD_FISH_PER_MIN / 60f) * deadFishCount * dt;
    }

    private void TickAmmoniaAfterFeeding(float dt)
    {
        for (int i = fedFishAmmoniaTimers.Count - 1; i >= 0; i--)
        {
            water.ammonia += (NH3_AFTER_FEEDING_PER_FISH_PER_5_MIN / 300f) * dt;
            fedFishAmmoniaTimers[i] -= dt;

            if (fedFishAmmoniaTimers[i] <= 0f)
                fedFishAmmoniaTimers.RemoveAt(i);
        }
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
        bool toxicityDoubled = IsNH3ToxicityDoubled();
        float toxicityMultiplier = toxicityDoubled ? 2f : 1f;
        float measuredAmmonia = water.ammonia;

        if (measuredAmmonia <= NH3_SAFE_MAX)
        {
            water.oxygen -= (DO_LOSS_NH3_SAFE_PER_4_MIN / 240f) * toxicityMultiplier * dt;
        }
        else if (measuredAmmonia < NH3_DANGER_MAX)
        {
            water.oxygen -= (DO_LOSS_NH3_DANGER_PER_FISH_PER_5_MIN / 300f) * toxicityMultiplier * Mathf.Max(1, livingFishCount) * dt;
        }
        else
        {
            water.oxygen -= (DO_LOSS_NH3_CRITICAL_PER_FISH_PER_5_MIN / 300f) * toxicityMultiplier * Mathf.Max(1, livingFishCount) * dt;
        }
    }
}
