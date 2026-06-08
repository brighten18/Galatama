using UnityEngine;

namespace GALATAMA.MainMenu
{
    public static class LodSettingsUtility
    {
        public static int ApplyLodModeToAllGroups(bool lodEnabled)
        {
            LODGroup[] lodGroups = Object.FindObjectsOfType<LODGroup>(true);
            int appliedCount = 0;

            for (int i = 0; i < lodGroups.Length; i++)
            {
                if (lodGroups[i] == null)
                {
                    continue;
                }

                ApplyLodMode(lodGroups[i], lodEnabled);
                appliedCount++;
            }

            return appliedCount;
        }

        public static void ApplyLodMode(LODGroup lodGroup, bool lodEnabled)
        {
            if (lodGroup == null)
            {
                return;
            }

            LOD[] lods = lodGroup.GetLODs();
            if (lods == null || lods.Length == 0)
            {
                lodGroup.enabled = lodEnabled;
                return;
            }

            if (lodEnabled)
            {
                SetAllRenderersEnabled(lods, true);
                lodGroup.enabled = true;
                lodGroup.ForceLOD(-1);
                return;
            }

            lodGroup.ForceLOD(-1);
            lodGroup.enabled = false;
            SetOnlyLod0Enabled(lods);
        }

        private static void SetAllRenderersEnabled(LOD[] lods, bool enabled)
        {
            for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                Renderer[] renderers = lods[lodIndex].renderers;
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.enabled = enabled;
                }
            }
        }

        private static void SetOnlyLod0Enabled(LOD[] lods)
        {
            for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                bool rendererEnabled = lodIndex == 0;
                Renderer[] renderers = lods[lodIndex].renderers;

                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.enabled = rendererEnabled;
                }
            }
        }
    }
}
