#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GALATAMA.Editor
{
    /// <summary>
    /// Editor utility for configuring LODGroup components on scene GameObjects
    /// using pre-made LOD meshes from an FBX file.
    ///
    /// Usage:
    ///  - Select a GameObject in the Hierarchy that has a LODGroup + a child named "LOD0".
    ///  - Go to GALATAMA > LOD > Setup LOD Group on Selected, or right-click > GALATAMA > Setup LOD Group.
    ///  - Choose the LOD FBX that contains meshes named with the pattern *_LOD0, *_LOD1, etc.
    /// </summary>
    public static class LODSetupTool
    {
        private const string MenuRoot = "GALATAMA/LOD/";
        private const string ContextMenu = "GameObject/GALATAMA/Setup LOD Group";

        // Screen-relative height thresholds for each LOD transition.
        // Index 0 = transition from LOD0 → LOD1, index N = cull threshold.
        private static readonly float[] LodTransitionThresholds = { 0.15f, 0.06f, 0.02f, 0.005f };

        // Maximum number of LOD levels to create (excluding LOD0).
        private const int MaxExtraLodLevels = 3;

        [MenuItem(MenuRoot + "Setup LOD Group on Selected", false, 1)]
        [MenuItem(ContextMenu, false, 10)]
        private static void SetupLodGroupOnSelected()
        {
            GameObject target = Selection.activeGameObject;
            if (target == null)
            {
                EditorUtility.DisplayDialog("LOD Setup", "Select a GameObject in the Hierarchy first.", "OK");
                return;
            }

            LODGroup lodGroup = target.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                EditorUtility.DisplayDialog("LOD Setup",
                    $"'{target.name}' does not have a LODGroup component.\nAdd one first.", "OK");
                return;
            }

            Transform lod0Transform = target.transform.Find("LOD0");
            if (lod0Transform == null)
            {
                EditorUtility.DisplayDialog("LOD Setup",
                    "A child named 'LOD0' was not found under the selected object.", "OK");
                return;
            }

            MeshRenderer lod0Renderer = lod0Transform.GetComponent<MeshRenderer>();
            MeshFilter lod0Filter = lod0Transform.GetComponent<MeshFilter>();
            if (lod0Renderer == null || lod0Filter == null)
            {
                EditorUtility.DisplayDialog("LOD Setup",
                    "The 'LOD0' child must have both MeshRenderer and MeshFilter components.", "OK");
                return;
            }

            // Prompt the user to choose the LOD FBX file.
            string absolutePath = EditorUtility.OpenFilePanel(
                "Select LOD FBX (containing *_LOD0, *_LOD1, ... meshes)",
                "Assets/Modular Object/LODTest",
                "fbx");

            if (string.IsNullOrEmpty(absolutePath))
            {
                return;
            }

            // Convert OS-absolute path to project-relative path.
            string relativePath = absolutePath;
            if (absolutePath.StartsWith(Application.dataPath))
            {
                relativePath = "Assets" + absolutePath.Substring(Application.dataPath.Length);
            }

            List<Mesh> sortedLodMeshes = LoadSortedLodMeshes(relativePath);
            if (sortedLodMeshes == null || sortedLodMeshes.Count == 0)
            {
                EditorUtility.DisplayDialog("LOD Setup",
                    $"No meshes found in:\n{relativePath}\n\nMake sure the FBX contains mesh sub-assets.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(target, "Setup LOD Group");

            Material[] sharedMaterials = lod0Renderer.sharedMaterials;

            // LOD0: keep the existing renderer with its current mesh.
            var lods = new List<LOD>
            {
                new LOD(LodTransitionThresholds[0], new Renderer[] { lod0Renderer })
            };

            // LOD1 .. LODn: create or update child GameObjects using meshes from the FBX.
            // sortedLodMeshes[0] = LOD0 mesh, [1] = LOD1 mesh, etc.
            // We skip index 0 because LOD0 already has its own mesh from the source FBX.
            int extraLevels = Mathf.Min(sortedLodMeshes.Count - 1, MaxExtraLodLevels);
            for (int i = 1; i <= extraLevels; i++)
            {
                Mesh lodMesh = sortedLodMeshes[i];
                string childName = $"LOD{i}";

                Transform existingChild = target.transform.Find(childName);
                GameObject lodObject = existingChild != null
                    ? existingChild.gameObject
                    : new GameObject(childName);

                if (existingChild == null)
                {
                    lodObject.transform.SetParent(target.transform, false);
                    Undo.RegisterCreatedObjectUndo(lodObject, "Create LOD Child");
                }

                MeshFilter meshFilter = GetOrAddComponent<MeshFilter>(lodObject);
                meshFilter.sharedMesh = lodMesh;

                MeshRenderer meshRenderer = GetOrAddComponent<MeshRenderer>(lodObject);
                meshRenderer.sharedMaterials = sharedMaterials;
                meshRenderer.shadowCastingMode = lod0Renderer.shadowCastingMode;
                meshRenderer.receiveShadows = lod0Renderer.receiveShadows;

                float threshold = i < LodTransitionThresholds.Length
                    ? LodTransitionThresholds[i]
                    : LodTransitionThresholds[LodTransitionThresholds.Length - 1];

                lods.Add(new LOD(threshold, new Renderer[] { meshRenderer }));
            }

            lodGroup.SetLODs(lods.ToArray());
            lodGroup.RecalculateBounds();
            lodGroup.fadeMode = LODFadeMode.CrossFade;
            lodGroup.animateCrossFading = true;

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();

            string summary = BuildSummary(lods, sortedLodMeshes, relativePath);
            Debug.Log($"[LODSetupTool] {summary}");
            EditorUtility.DisplayDialog("LOD Setup Complete", summary, "OK");
        }

        [MenuItem(MenuRoot + "Setup LOD Group on Selected", true)]
        [MenuItem(ContextMenu, true)]
        private static bool ValidateSetupLodGroup()
        {
            return Selection.activeGameObject != null;
        }

        [MenuItem(MenuRoot + "Reset to LOD0 Only", false, 2)]
        private static void ResetToLod0Only()
        {
            GameObject target = Selection.activeGameObject;
            if (target == null) return;

            Undo.RegisterFullObjectHierarchyUndo(target, "Reset LOD to LOD0");

            // Destroy all LOD children except LOD0.
            for (int i = target.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = target.transform.GetChild(i);
                if (child.name != "LOD0")
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            LODGroup lodGroup = target.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                Transform lod0Transform = target.transform.Find("LOD0");
                MeshRenderer lod0Renderer = lod0Transform != null
                    ? lod0Transform.GetComponent<MeshRenderer>()
                    : null;

                if (lod0Renderer != null)
                {
                    lodGroup.SetLODs(new LOD[]
                    {
                        new LOD(LodTransitionThresholds[LodTransitionThresholds.Length - 1],
                            new Renderer[] { lod0Renderer })
                    });
                    lodGroup.RecalculateBounds();
                    EditorUtility.SetDirty(target);
                }
            }

            Debug.Log($"[LODSetupTool] Reset '{target.name}' to LOD0 only.");
        }

        [MenuItem(MenuRoot + "Reset to LOD0 Only", true)]
        private static bool ValidateResetToLod0Only()
        {
            return Selection.activeGameObject != null;
        }

        // ─── Helpers ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads all Mesh sub-assets from the given FBX and sorts them by name so that
        /// LOD0 is first, LOD1 second, etc. Works with any naming convention that uses
        /// a numeric suffix (e.g., *_LOD0, *_LOD1 or *LOD0, *LOD1).
        /// </summary>
        private static List<Mesh> LoadSortedLodMeshes(string assetPath)
        {
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (allAssets == null) return null;

            return allAssets
                .OfType<Mesh>()
                .OrderBy(m => m.name)
                .ToList();
        }

        /// <summary>Returns the existing component or adds a new one.</summary>
        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static string BuildSummary(List<LOD> lods, List<Mesh> meshes, string fbxPath)
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"LOD Group configured with {lods.Count} level(s).");
            lines.AppendLine();

            for (int i = 0; i < lods.Count; i++)
            {
                string meshName = i < meshes.Count ? meshes[i].name : "existing";
                string trisInfo = i < meshes.Count
                    ? $"{meshes[i].triangles.Length / 3} tris"
                    : "";
                float nextThreshold = i + 1 < lods.Count ? lods[i + 1].screenRelativeTransitionHeight : 0f;
                lines.AppendLine(
                    $"  LOD{i}: {meshName} ({trisInfo})  " +
                    $"visible >{nextThreshold * 100f:0.0}% screen height");
            }

            lines.AppendLine($"\nSource FBX: {fbxPath}");
            return lines.ToString().TrimEnd();
        }
    }
}
#endif
