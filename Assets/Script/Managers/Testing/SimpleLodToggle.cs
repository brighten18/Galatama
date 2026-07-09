using UnityEngine;

namespace GALATAMA.MainMenu
{
    public class SimpleLodToggle : MonoBehaviour
    {
        [SerializeField] private bool lodEnabled = true;
        [SerializeField] private KeyCode toggleKey = KeyCode.L;

        private void Start()
        {
            ApplyLodState();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                lodEnabled = !lodEnabled;
                ApplyLodState();
            }
        }

        [ContextMenu("Apply LOD State")]
        public void ApplyLodState()
        {
            LODGroup[] lodGroups = FindObjectsOfType<LODGroup>(true);

            for (int i = 0; i < lodGroups.Length; i++)
            {
                ApplyToGroup(lodGroups[i], lodEnabled);
            }

            Debug.Log("[SimpleLodToggle] LOD " + (lodEnabled ? "ON" : "OFF") + " | Groups: " + lodGroups.Length);
        }

        private static void ApplyToGroup(LODGroup lodGroup, bool enabled)
        {
            if (lodGroup == null)
            {
                return;
            }

            LOD[] lods = lodGroup.GetLODs();
            if (lods == null || lods.Length == 0)
            {
                lodGroup.enabled = enabled;
                return;
            }

            if (enabled)
            {
                SetAllRenderers(lods, true);
                lodGroup.enabled = true;
                lodGroup.ForceLOD(-1);
                return;
            }

            lodGroup.enabled = false;
            SetOnlyLod0(lods);
        }

        private static void SetAllRenderers(LOD[] lods, bool isEnabled)
        {
            for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                Renderer[] renderers = lods[lodIndex].renderers;
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer != null)
                    {
                        renderer.enabled = isEnabled;
                    }
                }
            }
        }

        private static void SetOnlyLod0(LOD[] lods)
        {
            for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                bool shouldBeEnabled = lodIndex == 0;
                Renderer[] renderers = lods[lodIndex].renderers;

                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer != null)
                    {
                        renderer.enabled = shouldBeEnabled;
                    }
                }
            }
        }
    }
}
