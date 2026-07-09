using System.Collections.Generic;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GALATAMA.Debugging
{
    public class LodSceneDebugReporter : MonoBehaviour
    {
        [SerializeField] private bool logOnStart;
        [SerializeField] private float logDelaySeconds = 15f;

        private void Start()
        {
            if (logOnStart)
            {
                StartCoroutine(LogSceneLodSummaryAfterDelay());
            }
        }

        [ContextMenu("Log Scene LOD Summary")]
        public void LogSceneLodSummary()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            int totalSceneGameObjects = 0;
            int totalSceneComponents = 0;
            for (int i = 0; i < rootObjects.Length; i++)
            {
                CountHierarchy(rootObjects[i].transform, ref totalSceneGameObjects, ref totalSceneComponents);
            }

            LODGroup[] lodGroups = Object.FindObjectsOfType<LODGroup>(true);
            HashSet<GameObject> lodManagedGameObjects = new HashSet<GameObject>();
            int totalLodRenderers = 0;
            for (int i = 0; i < lodGroups.Length; i++)
            {
                if (lodGroups[i] == null)
                {
                    continue;
                }

                LOD[] lods = lodGroups[i].GetLODs();
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

                        totalLodRenderers++;
                        lodManagedGameObjects.Add(renderer.gameObject);
                    }
                }
            }

            Terrain[] terrains = Object.FindObjectsOfType<Terrain>(true);
            int totalTerrainCount = terrains.Length;
            int totalTerrainTreeInstances = 0;
            int totalTerrainTreePrototypes = 0;
            int terrainsWithTrees = 0;

            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null || terrain.terrainData == null)
                {
                    continue;
                }

                TreeInstance[] treeInstances = terrain.terrainData.treeInstances;
                TreePrototype[] treePrototypes = terrain.terrainData.treePrototypes;

                int treeInstanceCount = treeInstances != null ? treeInstances.Length : 0;
                int treePrototypeCount = treePrototypes != null ? treePrototypes.Length : 0;

                totalTerrainTreeInstances += treeInstanceCount;
                totalTerrainTreePrototypes += treePrototypeCount;

                if (treeInstanceCount > 0)
                {
                    terrainsWithTrees++;
                }
            }

            int totalObjectsManagedByLodEstimate = lodManagedGameObjects.Count + totalTerrainTreeInstances;

            StringBuilder builder = new StringBuilder(512);
            builder.AppendLine("[LOD DEBUG] Scene Summary");
            builder.Append("Scene: ").Append(activeScene.name).AppendLine();
            builder.Append("Total Scene GameObjects: ").Append(totalSceneGameObjects).AppendLine();
            builder.Append("Total Scene Components: ").Append(totalSceneComponents).AppendLine();
            builder.Append("LODGroup Count: ").Append(lodGroups.Length).AppendLine();
            builder.Append("LODGroup Managed GameObjects (unique renderer owners): ").Append(lodManagedGameObjects.Count).AppendLine();
            builder.Append("LODGroup Renderer Slots (all LOD levels): ").Append(totalLodRenderers).AppendLine();
            builder.Append("Terrain Count: ").Append(totalTerrainCount).AppendLine();
            builder.Append("Terrains With Trees: ").Append(terrainsWithTrees).AppendLine();
            builder.Append("Terrain Tree Instances: ").Append(totalTerrainTreeInstances).AppendLine();
            builder.Append("Terrain Tree Prototypes: ").Append(totalTerrainTreePrototypes).AppendLine();
            builder.Append("Estimated Total Objects Managed By LOD (LODGroup GameObjects + Terrain Tree Instances): ")
                .Append(totalObjectsManagedByLodEstimate).AppendLine();
            builder.AppendLine("Catatan: tree pada Terrain bukan GameObject biasa, jadi dihitung dari TreeInstance.");

            Debug.Log(builder.ToString());
        }

        [ContextMenu("Log Scene LOD Summary After Delay")]
        public void LogSceneLodSummaryAfterDelayNow()
        {
            StartCoroutine(LogSceneLodSummaryAfterDelay());
        }

        private IEnumerator LogSceneLodSummaryAfterDelay()
        {
            if (logDelaySeconds > 0f)
            {
                yield return new WaitForSeconds(logDelaySeconds);
            }

            LogSceneLodSummary();
        }

        private static void CountHierarchy(Transform root, ref int gameObjectCount, ref int componentCount)
        {
            if (root == null)
            {
                return;
            }

            gameObjectCount++;
            componentCount += root.GetComponents<Component>().Length;

            for (int i = 0; i < root.childCount; i++)
            {
                CountHierarchy(root.GetChild(i), ref gameObjectCount, ref componentCount);
            }
        }
    }
}
