using UnityEngine;

/// <summary>
/// Billboard marker placed above the fish trap in world space to indicate its location.
/// Bobs up and down to attract attention and changes color/sprite when a fish is caught.
/// Requires FishTrapWorld on the same GameObject.
/// </summary>
[RequireComponent(typeof(FishTrapWorld))]
public class TrapWorldMarker : MonoBehaviour
{
    [Header("Marker Transform")]
    [Tooltip("Jarak tambahan di atas puncak mesh perangkap (local space). Nilai 0 = tepat di atas perangkap.")]
    [SerializeField] private float heightOffset = 0.05f;
    [SerializeField] private float markerSize = 0.35f;

    [Header("Sprites")]
    [Tooltip("Sprite shown when the trap is empty (waiting for fish).")]
    [SerializeField] private Sprite emptyTrapSprite;

    [Tooltip("Sprite shown when a fish has been caught.")]
    [SerializeField] private Sprite fishCaughtSprite;

    [Header("State Colors")]
    [SerializeField] private Color emptyColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color capturedColor = new Color(0.3f, 1f, 0.45f, 1f);

    [Header("Bob Animation")]
    [SerializeField] private float bobSpeed = 1.5f;
    [SerializeField] private float bobAmount = 0.12f;

    private SpriteRenderer markerRenderer;
    private Transform markerTransform;
    private Camera mainCamera;
    private float bobPhase;
    private float baseLocalY;

    private void Awake()
    {
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
        BuildMarker();
    }

    private void Start()
    {
        mainCamera = Camera.main;

        if (markerTransform != null)
        {
            // Hitung posisi Y puncak mesh perangkap dalam local space, lalu tambahkan heightOffset
            // sehingga marker selalu dimulai tepat di atas perangkap
            float meshTopLocalY = GetMeshTopLocalY();
            baseLocalY = meshTopLocalY + heightOffset;

            Vector3 pos = markerTransform.localPosition;
            pos.y = baseLocalY;
            markerTransform.localPosition = pos;
        }
    }

    /// <summary>
    /// Menghitung posisi Y puncak mesh perangkap dalam local space transform ini,
    /// dengan mengecualikan SpriteRenderer marker itu sendiri.
    /// </summary>
    private float GetMeshTopLocalY()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        Bounds? combined = null;
        foreach (Renderer r in renderers)
        {
            // Abaikan SpriteRenderer milik marker agar tidak mempengaruhi perhitungan
            if (r == markerRenderer) continue;
            if (combined == null)
                combined = r.bounds;
            else
            {
                Bounds b = combined.Value;
                b.Encapsulate(r.bounds);
                combined = b;
            }
        }

        if (combined == null) return 0f;

        // Konversi world Y puncak mesh ke local Y transform ini
        // InverseTransformPoint sudah memperhitungkan posisi, rotasi, dan skala
        Vector3 worldTopPoint = new Vector3(transform.position.x, combined.Value.max.y, transform.position.z);
        return transform.InverseTransformPoint(worldTopPoint).y;
    }

    private void BuildMarker()
    {
        var markerGO = new GameObject("_TrapLocationMarker");
        markerGO.transform.SetParent(transform, false);
        markerGO.transform.localPosition = Vector3.zero; // posisi Y dihitung ulang di Start()
        markerGO.transform.localScale = Vector3.one * markerSize;

        markerRenderer = markerGO.AddComponent<SpriteRenderer>();
        markerRenderer.color = emptyColor;
        markerRenderer.sortingOrder = 20;
        markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        markerRenderer.receiveShadows = false;

        markerRenderer.sprite = emptyTrapSprite != null
            ? emptyTrapSprite
            : CreateFallbackSprite();

        markerTransform = markerGO.transform;
    }

    private void LateUpdate()
    {
        if (markerTransform == null) return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        // Billboard: always face the camera
        if (mainCamera != null)
        {
            Vector3 toCamera = markerTransform.position - mainCamera.transform.position;
            if (toCamera.sqrMagnitude > 0.001f)
            {
                markerTransform.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
            }
        }

        // Bob hanya ke atas dari baseLocalY: remap Sin dari (-1,1) ke (0,1) lalu kalikan bobAmount
        // Ini memastikan marker tidak pernah turun ke bawah posisi dasarnya (puncak mesh)
        float bob = (Mathf.Sin(Time.time * bobSpeed + bobPhase) + 1f) * 0.5f * bobAmount;
        Vector3 local = markerTransform.localPosition;
        local.y = baseLocalY + bob;
        markerTransform.localPosition = local;
    }

    /// <summary>Updates the marker visual to reflect whether a fish has been caught.</summary>
    public void SetCapturedState(bool hasFish)
    {
        if (markerRenderer == null) return;

        markerRenderer.color = hasFish ? capturedColor : emptyColor;

        Sprite targetSprite = hasFish ? fishCaughtSprite : emptyTrapSprite;
        if (targetSprite != null)
            markerRenderer.sprite = targetSprite;
    }

    /// <summary>Creates a simple white circle sprite used as a fallback when no sprite is assigned.</summary>
    private static Sprite CreateFallbackSprite()
    {
        const int size = 64;
        const float radius = 28f;
        var center = new Vector2(size / 2f, size / 2f);

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.name = "TrapMarker_Fallback";
        var pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);

                // Smooth circle edge with anti-aliasing
                float alpha = Mathf.Clamp01(radius - dist + 1f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private void OnDestroy()
    {
        if (markerTransform != null && markerTransform.gameObject != null)
            Destroy(markerTransform.gameObject);
    }
}
