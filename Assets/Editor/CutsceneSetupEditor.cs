using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GALATAMA.Cutscene;

namespace GALATAMA.Editor
{
    /// <summary>
    /// One-click tool that builds the complete CutScene UI hierarchy from scratch.
    /// Run via the menu: GALATAMA > Setup Cutscene Scene.
    /// </summary>
    public static class CutsceneSetupEditor
    {
        private const string ScenePath = "Assets/Scenes/CutScene.unity";
        private const string ImagePath = "Assets/Scenes/CUTSCENE.png";

        private static readonly string[] PanelTexts =
        {
            "Namaku Jafar. Setelah bertahun-tahun merantau dan menyelesaikan studi di luar negeri, aku akhirnya melangkahkan kaki kembali ke kampung halamanku.",
            "Malam ini udaranya terasa begitu tenang. Rasanya rindu sekali melihat kembali rumah pesisir tempatku dibesarkan dulu.",
            "Namun, kepulanganku kali ini bukan sekadar untuk bernostalgia. Ada sebuah mimpi dan tekad besar yang harus kuwujudkan di sini.",
            "Berbekal ilmu yang kupelajari, aku memutuskan untuk merintis usaha budidaya ikan hias. Gudang tua ini akan menjadi titik awal dari segalanya.",
            "Wah, aku sudah tidak sabar! Membayangkan fasilitas ini beroperasi membuatku sangat bersemangat!",
            "Dengan kerja keras dan teknologi Recirculating Aquaculture System (RAS) yang ramah lingkungan, aku pasti bisa menciptakan akuarium budidaya air laut terbaik!"
        };

        [MenuItem("GALATAMA/Setup Cutscene Scene")]
        public static void SetupCutsceneScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Clean up any existing UI objects from previous runs.
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                Object.DestroyImmediate(c.gameObject);
            foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                Object.DestroyImmediate(es.gameObject);
            foreach (var ctrl in Object.FindObjectsByType<CutsceneController>(FindObjectsSortMode.None))
                Object.DestroyImmediate(ctrl.gameObject);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
            var bgSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            var font = GetDefaultFont();

            // ── Canvas ──────────────────────────────────────────────────────────────
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Background (full-screen comic strip image) ──────────────────────────
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            StretchFull(bgGO.AddComponent<RectTransform>());
            var rawImg = bgGO.AddComponent<RawImage>();
            rawImg.texture = texture;
            rawImg.color = Color.white;
            rawImg.raycastTarget = false;

            // ── FadeOverlay (full-screen black, fades to transparent at start) ──────
            var fadeGO = new GameObject("FadeOverlay");
            fadeGO.transform.SetParent(canvasGO.transform, false);
            StretchFull(fadeGO.AddComponent<RectTransform>());
            var fadeImg = fadeGO.AddComponent<Image>();
            fadeImg.sprite = bgSprite;
            fadeImg.color = Color.black;
            fadeImg.raycastTarget = false;

            // ── DialogPanel (anchored to bottom, 240 px tall) ────────────────────────
            var panelGO = new GameObject("DialogPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(0f, 240f);
            var panelImg = panelGO.AddComponent<Image>();
            panelImg.sprite = bgSprite;
            panelImg.color = new Color(0f, 0f, 0f, 0.85f);
            panelImg.raycastTarget = false;

            // ── PanelIndicator (top-right of dialog panel) ───────────────────────────
            var indGO = new GameObject("PanelIndicator");
            indGO.transform.SetParent(panelGO.transform, false);
            var indRect = indGO.AddComponent<RectTransform>();
            indRect.anchorMin = new Vector2(1f, 1f);
            indRect.anchorMax = new Vector2(1f, 1f);
            indRect.pivot = new Vector2(1f, 1f);
            indRect.anchoredPosition = new Vector2(-15f, -10f);
            indRect.sizeDelta = new Vector2(110f, 30f);
            var indText = indGO.AddComponent<Text>();
            indText.font = font;
            indText.text = "1 / 6";
            indText.fontSize = 16;
            indText.color = new Color(1f, 1f, 1f, 0.7f);
            indText.alignment = TextAnchor.UpperRight;

            // ── BodyText (main narration area, stretches with padding) ───────────────
            var bodyGO = new GameObject("BodyText");
            bodyGO.transform.SetParent(panelGO.transform, false);
            var bodyRect = bodyGO.AddComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.pivot = new Vector2(0.5f, 0.5f);
            bodyRect.offsetMin = new Vector2(30f, 60f);   // left, bottom padding
            bodyRect.offsetMax = new Vector2(-30f, -45f); // right, top padding
            var bodyTxt = bodyGO.AddComponent<Text>();
            bodyTxt.font = font;
            bodyTxt.text = string.Empty;
            bodyTxt.fontSize = 22;
            bodyTxt.color = Color.white;
            bodyTxt.alignment = TextAnchor.MiddleLeft;
            bodyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyTxt.verticalOverflow = VerticalWrapMode.Overflow;
            bodyTxt.lineSpacing = 1.2f;

            // ── SkipButton (bottom-left) ─────────────────────────────────────────────
            var skipBtnGO = CreateButton(panelGO.transform, "SkipButton", "Lewati", font,
                anchorMin: new Vector2(0f, 0f),
                anchorMax: new Vector2(0f, 0f),
                pivot: new Vector2(0f, 0f),
                anchoredPos: new Vector2(20f, 10f),
                size: new Vector2(120f, 45f),
                color: new Color(0.25f, 0.25f, 0.25f, 0.9f),
                bgSprite: bgSprite);

            // ── NextButton (bottom-right) ────────────────────────────────────────────
            var nextBtnGO = CreateButton(panelGO.transform, "NextButton", "Lanjut \u2192", font,
                anchorMin: new Vector2(1f, 0f),
                anchorMax: new Vector2(1f, 0f),
                pivot: new Vector2(1f, 0f),
                anchoredPos: new Vector2(-20f, 10f),
                size: new Vector2(150f, 45f),
                color: new Color(0.08f, 0.5f, 0.8f, 1f),
                bgSprite: bgSprite);

            // ── EventSystem ──────────────────────────────────────────────────────────
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            var inputModuleType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModuleType != null)
                esGO.AddComponent(inputModuleType);
            else
                esGO.AddComponent<StandaloneInputModule>();

            // ── CutsceneManager ──────────────────────────────────────────────────────
            var mgrGO = new GameObject("CutsceneManager");
            var controller = mgrGO.AddComponent<CutsceneController>();

            // Wire up serialized fields
            var so = new SerializedObject(controller);
            so.FindProperty("backgroundImage").objectReferenceValue = rawImg;
            so.FindProperty("bodyText").objectReferenceValue = bodyTxt;
            so.FindProperty("panelIndicatorText").objectReferenceValue = indText;
            so.FindProperty("nextButton").objectReferenceValue = nextBtnGO.GetComponent<Button>();
            so.FindProperty("skipButton").objectReferenceValue = skipBtnGO.GetComponent<Button>();
            so.FindProperty("fadeOverlay").objectReferenceValue = fadeImg;

            var panelsProp = so.FindProperty("panels");
            panelsProp.arraySize = PanelTexts.Length;
            for (int i = 0; i < PanelTexts.Length; i++)
                panelsProp.GetArrayElementAtIndex(i)
                          .FindPropertyRelative("body")
                          .stringValue = PanelTexts[i];

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CutsceneSetup] Done. " + PanelTexts.Length + " panels wired up in " + ScenePath);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Creates a Button GameObject with a Text child and returns it.</summary>
        private static GameObject CreateButton(Transform parent, string name, string label, Font font,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 size, Color color, Sprite bgSprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.sprite = bgSprite;
            img.color = color;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var txtRect = txtGO.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            var txt = txtGO.AddComponent<Text>();
            txt.text = label;
            txt.font = font;
            txt.fontSize = 18;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;

            return go;
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }
    }
}
