using System.Collections.Generic;
using UnityEngine;

namespace GALATAMA.MainMenu
{
    public static class LodSettingsUtility
    {
        public const string PrefLodEnabled = "settings.lodEnabled";

        private struct TerrainTreeSettings
        {
            public bool drawTreesAndFoliage;
            public float treeDistance;
            public float treeBillboardDistance;
            public float treeCrossFadeLength;
            public int treeMaximumFullLODCount;
        }

        private static readonly Dictionary<int, TerrainTreeSettings> terrainTreeSettingsCache = new Dictionary<int, TerrainTreeSettings>();

        public struct LodVerificationResult
        {
            public bool expectedEnabled;
            public int totalGroups;
            public int matchingGroups;
            public int mismatchedGroups;
            public int enabledGroups;
            public int disabledGroups;
            public int totalTerrainsWithTrees;
            public int matchingTerrainsWithTrees;
            public int mismatchedTerrainsWithTrees;
            public bool isFullyApplied;
        }

        public static bool GetSavedLodEnabled()
        {
            return PlayerPrefs.GetInt(PrefLodEnabled, 1) == 1;
        }

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

            Terrain[] terrains = Object.FindObjectsOfType<Terrain>(true);
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (!HasTerrainTrees(terrain))
                {
                    continue;
                }

                ApplyTerrainTreeLodMode(terrain, lodEnabled);
                appliedCount++;
            }

            return appliedCount;
        }

        public static LodVerificationResult VerifyLodMode(bool expectedEnabled)
        {
            LODGroup[] lodGroups = Object.FindObjectsOfType<LODGroup>(true);
            LodVerificationResult result = new LodVerificationResult
            {
                expectedEnabled = expectedEnabled,
                totalGroups = lodGroups.Length
            };

            for (int i = 0; i < lodGroups.Length; i++)
            {
                LODGroup lodGroup = lodGroups[i];
                if (lodGroup == null)
                {
                    continue;
                }

                bool isMatching = IsLodGroupInExpectedMode(lodGroup, expectedEnabled);
                if (lodGroup.enabled)
                {
                    result.enabledGroups++;
                }
                else
                {
                    result.disabledGroups++;
                }

                if (isMatching)
                {
                    result.matchingGroups++;
                }
                else
                {
                    result.mismatchedGroups++;
                }
            }

            Terrain[] terrains = Object.FindObjectsOfType<Terrain>(true);
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (!HasTerrainTrees(terrain))
                {
                    continue;
                }

                result.totalTerrainsWithTrees++;
                if (IsTerrainInExpectedMode(terrain, expectedEnabled))
                {
                    result.matchingTerrainsWithTrees++;
                }
                else
                {
                    result.mismatchedTerrainsWithTrees++;
                }
            }

            bool groupsApplied = result.totalGroups == 0 || result.mismatchedGroups == 0;
            bool terrainsApplied = result.totalTerrainsWithTrees == 0 || result.mismatchedTerrainsWithTrees == 0;
            result.isFullyApplied = groupsApplied && terrainsApplied;
            return result;
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

        public static void ApplyTerrainTreeLodMode(Terrain terrain, bool lodEnabled)
        {
            if (!HasTerrainTrees(terrain))
            {
                return;
            }

            int terrainId = terrain.GetInstanceID();
            if (!terrainTreeSettingsCache.ContainsKey(terrainId))
            {
                terrainTreeSettingsCache[terrainId] = CaptureTerrainTreeSettings(terrain);
            }

            TerrainTreeSettings originalSettings = terrainTreeSettingsCache[terrainId];
            if (lodEnabled)
            {
                RestoreTerrainTreeSettings(terrain, originalSettings);
                return;
            }

            terrain.drawTreesAndFoliage = originalSettings.drawTreesAndFoliage;
            terrain.treeDistance = Mathf.Max(0f, originalSettings.treeDistance);
            terrain.treeBillboardDistance = Mathf.Max(terrain.treeDistance, originalSettings.treeBillboardDistance);
            terrain.treeCrossFadeLength = 0f;
            terrain.treeMaximumFullLODCount = Mathf.Max(originalSettings.treeMaximumFullLODCount, 1000000);
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

        private static bool HasTerrainTrees(Terrain terrain)
        {
            return terrain != null
                && terrain.terrainData != null
                && terrain.terrainData.treeInstances != null
                && terrain.terrainData.treeInstances.Length > 0;
        }

        private static TerrainTreeSettings CaptureTerrainTreeSettings(Terrain terrain)
        {
            return new TerrainTreeSettings
            {
                drawTreesAndFoliage = terrain.drawTreesAndFoliage,
                treeDistance = terrain.treeDistance,
                treeBillboardDistance = terrain.treeBillboardDistance,
                treeCrossFadeLength = terrain.treeCrossFadeLength,
                treeMaximumFullLODCount = terrain.treeMaximumFullLODCount
            };
        }

        private static void RestoreTerrainTreeSettings(Terrain terrain, TerrainTreeSettings settings)
        {
            terrain.drawTreesAndFoliage = settings.drawTreesAndFoliage;
            terrain.treeDistance = settings.treeDistance;
            terrain.treeBillboardDistance = settings.treeBillboardDistance;
            terrain.treeCrossFadeLength = settings.treeCrossFadeLength;
            terrain.treeMaximumFullLODCount = settings.treeMaximumFullLODCount;
        }

        private static bool IsLodGroupInExpectedMode(LODGroup lodGroup, bool expectedEnabled)
        {
            if (lodGroup == null)
            {
                return false;
            }

            LOD[] lods = lodGroup.GetLODs();
            if (lods == null || lods.Length == 0)
            {
                return lodGroup.enabled == expectedEnabled;
            }

            if (expectedEnabled)
            {
                return lodGroup.enabled;
            }

            if (lodGroup.enabled)
            {
                return false;
            }

            for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
            {
                bool shouldBeEnabled = lodIndex == 0;
                Renderer[] renderers = lods[lodIndex].renderers;
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer == null)
                    {
                        continue;
                    }

                    if (renderer.enabled != shouldBeEnabled)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsTerrainInExpectedMode(Terrain terrain, bool expectedEnabled)
        {
            if (!HasTerrainTrees(terrain))
            {
                return true;
            }

            int terrainId = terrain.GetInstanceID();
            if (!terrainTreeSettingsCache.ContainsKey(terrainId))
            {
                terrainTreeSettingsCache[terrainId] = CaptureTerrainTreeSettings(terrain);
            }

            TerrainTreeSettings originalSettings = terrainTreeSettingsCache[terrainId];
            if (expectedEnabled)
            {
                return terrain.drawTreesAndFoliage == originalSettings.drawTreesAndFoliage
                    && Mathf.Approximately(terrain.treeDistance, originalSettings.treeDistance)
                    && Mathf.Approximately(terrain.treeBillboardDistance, originalSettings.treeBillboardDistance)
                    && Mathf.Approximately(terrain.treeCrossFadeLength, originalSettings.treeCrossFadeLength)
                    && terrain.treeMaximumFullLODCount == originalSettings.treeMaximumFullLODCount;
            }

            return terrain.drawTreesAndFoliage == originalSettings.drawTreesAndFoliage
                && terrain.treeMaximumFullLODCount >= Mathf.Max(originalSettings.treeMaximumFullLODCount, 1000000)
                && Mathf.Approximately(terrain.treeDistance, Mathf.Max(0f, originalSettings.treeDistance))
                && Mathf.Approximately(terrain.treeBillboardDistance, Mathf.Max(terrain.treeDistance, originalSettings.treeBillboardDistance))
                && Mathf.Approximately(terrain.treeCrossFadeLength, 0f);
        }
    }
}
