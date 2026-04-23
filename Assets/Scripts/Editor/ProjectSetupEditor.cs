#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Tilemaps;
using System.IO;
using System.Collections.Generic;

namespace LochyIGorzala.Editor
{
    /// <summary>
    /// Editor utility that automatically builds the MainMenu and Dungeon scenes.
    /// Run from: Tools > Lochy i Gorzala > Setup Project
    /// </summary>
    public class ProjectSetupEditor
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string MainMenuScenePath  = "Assets/Scenes/MainMenu.unity";
        private const string CharSelectScenePath = "Assets/Scenes/CharSelect.unity";
        private const string DungeonScenePath   = "Assets/Scenes/Dungeon.unity";
        private const string CombatScenePath    = "Assets/Scenes/Combat.unity";

        // Art paths
        private const string LogoV2Path = "Assets/logo_v2.png";
        private const string LogoPath = "Assets/Art/logo.png";
        private const string TileSheetPath = "Assets/Art/32rogues-0.5.0/32rogues/tiles.png";
        private const string RogueSheetPath = "Assets/Art/32rogues-0.5.0/32rogues/rogues.png";
        private const string MonstersSheetPath = "Assets/Art/32rogues-0.5.0/32rogues/monsters.png";
        private const string ItemsSheetPath    = "Assets/Art/32rogues-0.5.0/32rogues/items.png";

        // logo_v2.png is 1264x842, buttons detected at bottom strip
        // Button Y range: roughly y=758 to y=825 (from top of image)
        // Unity anchors: Y inverted (0 = bottom, 1 = top)
        private const float BtnAnchorYMin = 0.0202f;  // 1 - 825/842
        private const float BtnAnchorYMax = 0.0998f;   // 1 - 758/842

        [MenuItem("Tools/Lochy i Gorzala/Setup Project (Stworz Sceny)", false, 0)]
        public static void SetupProject()
        {
            if (!EditorUtility.DisplayDialog(
                "Setup Project - Lochy i Gorzala",
                "Ta operacja stworzy sceny MainMenu i Dungeon.\nIstniejace sceny o tych nazwach zostana nadpisane.\n\nKontynuowac?",
                "Tak, stworz sceny",
                "Anuluj"))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            ConfigureSpriteImports();
            BuildMainMenuScene();
            BuildCharSelectScene();
            BuildDungeonScene();
            BuildCombatScene();
            UpdateBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(MainMenuScenePath);

            EditorUtility.DisplayDialog(
                "Setup Complete!",
                "Projekt zostal skonfigurowany!\n\n" +
                "Sceny:\n- MainMenu\n- Dungeon\n\n" +
                "Nacisnij Play aby uruchomic gre z menu glownego.",
                "OK");
        }

        [MenuItem("Tools/Lochy i Gorzala/Fix Sprite Imports", false, 1)]
        public static void FixSpriteImports()
        {
            ConfigureSpriteImports();
            AssetDatabase.Refresh();
            Debug.Log("Sprite import settings updated.");
        }

        // =====================================================
        // SPRITE IMPORT CONFIGURATION
        // =====================================================

        private static void ConfigureSpriteImports()
        {
            string[] spriteSheetPaths = new string[]
            {
                TileSheetPath,
                RogueSheetPath,
                "Assets/Art/32rogues-0.5.0/32rogues/monsters.png",
                "Assets/Art/32rogues-0.5.0/32rogues/items.png",
                "Assets/Art/32rogues-0.5.0/32rogues/animals.png",
            };

            foreach (string path in spriteSheetPaths)
            {
                ConfigureSpriteSheet(path);
            }

            // Configure menu background (logo_v2)
            ConfigureMenuBackground(LogoV2Path);
            ConfigureMenuBackground(LogoPath);
        }

        private static void ConfigureSpriteSheet(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 32;
            importer.isReadable = true;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = false;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        private static void ConfigureMenuBackground(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; // Pixel art style
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100;
            importer.isReadable = false;
            importer.maxTextureSize = 2048;
            importer.mipmapEnabled = false;

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }

        // =====================================================
        // MAIN MENU SCENE (with logo_v2 fullscreen background)
        // =====================================================

        private static void BuildMainMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Camera ---
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.05f, 0.03f, 0.02f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cameraObj.tag = "MainCamera";
            cameraObj.AddComponent<AudioListener>();

            // --- GameManager ---
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<Managers.GameManager>();

            // --- Event System ---
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // --- Canvas ---
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1264, 842);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // ============================
            // MAIN PANEL (with background)
            // ============================
            GameObject mainPanel = new GameObject("MainPanel");
            mainPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.sizeDelta = Vector2.zero;

            // --- Fullscreen Background Image (logo_v2.png) ---
            GameObject bgObj = new GameObject("BackgroundImage");
            bgObj.transform.SetParent(mainPanel.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImage = bgObj.AddComponent<Image>();
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoV2Path);
            if (bgSprite != null)
            {
                bgImage.sprite = bgSprite;
                bgImage.preserveAspect = false;
                bgImage.type = Image.Type.Simple;
            }
            else
            {
                Debug.LogError("logo_v2.png not found at: " + LogoV2Path);
                bgImage.color = new Color(0.1f, 0.08f, 0.06f);
            }
            // BUG FIX: Background image should NOT intercept raycasts.
            // Without this, the background absorbs all clicks in non-button areas,
            // making the overlay buttons feel unresponsive.
            bgImage.raycastTarget = false;

            // --- Overlay buttons: generously sized hit areas over image buttons ---
            // Expanded Y range (was 0.020–0.100, now 0.000–0.130) for easier clicking.
            // X ranges kept accurate to image button positions.
            const float yMin = 0.000f;
            const float yMax = 0.130f;

            GameObject startBtn = CreateOverlayButton(mainPanel.transform, "StartButton",
                new Vector2(0.035f, yMin), new Vector2(0.220f, yMax));

            GameObject loadBtn = CreateOverlayButton(mainPanel.transform, "LoadButton",
                new Vector2(0.225f, yMin), new Vector2(0.420f, yMax));

            GameObject optionsBtn = CreateOverlayButton(mainPanel.transform, "OptionsButton",
                new Vector2(0.415f, yMin), new Vector2(0.582f, yMax));

            GameObject authorsBtn = CreateOverlayButton(mainPanel.transform, "AuthorsButton",
                new Vector2(0.588f, yMin), new Vector2(0.768f, yMax));

            GameObject quitBtn = CreateOverlayButton(mainPanel.transform, "QuitButton",
                new Vector2(0.790f, yMin), new Vector2(0.953f, yMax));

            // ============================
            // SAVE SLOT PANEL (for load game)
            // ============================
            GameObject saveSlotPanelObj = BuildSaveSlotPanel(canvasObj.transform);

            // ============================
            // AUTHORS PANEL (pixel art style)
            // ============================
            GameObject authorsPanel = BuildAuthorsPanel(canvasObj.transform);
            authorsPanel.SetActive(false);

            // ============================
            // OPTIONS PANEL (DLC joke)
            // ============================
            GameObject optionsPanel = BuildOptionsPanel(canvasObj.transform);
            optionsPanel.SetActive(false);

            // ============================
            // MENU CONTROLLER
            // ============================
            GameObject menuController = new GameObject("MainMenuController");
            menuController.transform.SetParent(canvasObj.transform, false);
            UI.MainMenuController mmc = menuController.AddComponent<UI.MainMenuController>();

            SerializedObject so = new SerializedObject(mmc);
            so.FindProperty("mainPanel").objectReferenceValue = mainPanel;
            so.FindProperty("authorsPanel").objectReferenceValue = authorsPanel;
            so.FindProperty("optionsPanel").objectReferenceValue = optionsPanel;
            so.FindProperty("startButton").objectReferenceValue = startBtn.GetComponent<Button>();
            so.FindProperty("loadButton").objectReferenceValue = loadBtn.GetComponent<Button>();
            so.FindProperty("optionsButton").objectReferenceValue = optionsBtn.GetComponent<Button>();
            so.FindProperty("authorsButton").objectReferenceValue = authorsBtn.GetComponent<Button>();
            so.FindProperty("quitButton").objectReferenceValue = quitBtn.GetComponent<Button>();
            so.FindProperty("authorsBackButton").objectReferenceValue =
                authorsPanel.transform.Find("BackButton").GetComponent<Button>();
            so.FindProperty("optionsBackButton").objectReferenceValue =
                optionsPanel.transform.Find("BackButton").GetComponent<Button>();
            so.FindProperty("saveSlotPanel").objectReferenceValue =
                saveSlotPanelObj.GetComponent<UI.SaveSlotPanel>();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            Debug.Log("MainMenu scene created at: " + MainMenuScenePath);
        }

        /// <summary>
        /// Creates an overlay button for the main menu background image.
        /// Uses a near-invisible Image (alpha=0.01) which guarantees raycasts register,
        /// and sets targetGraphic explicitly so Button transitions work correctly.
        /// On hover shows a gold tint so players get visual feedback.
        /// </summary>
        private static GameObject CreateOverlayButton(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // alpha=0.01: essentially invisible to the eye but guarantees raycast detection.
            // alpha=0 can silently fail in some Unity versions / graphics backends.
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(1f, 1f, 1f, 0.01f);
            btnImage.raycastTarget = true; // explicit — never rely on default

            Button btn = btnObj.AddComponent<Button>();
            // MUST set targetGraphic explicitly — without it Button may not find the Image
            btn.targetGraphic = btnImage;

            ColorBlock colors = btn.colors;
            colors.normalColor      = new Color(1f, 1f, 1f, 0.01f);       // nearly invisible
            colors.highlightedColor = new Color(1f, 0.85f, 0.3f, 0.18f);  // gold glow on hover
            colors.pressedColor     = new Color(1f, 0.7f, 0.1f, 0.30f);   // brighter on click
            colors.selectedColor    = new Color(1f, 1f, 1f, 0.01f);
            colors.disabledColor    = new Color(0.5f, 0.5f, 0.5f, 0.01f);
            colors.fadeDuration     = 0.05f; // snappy response
            colors.colorMultiplier  = 1f;
            btn.colors = colors;

            return btnObj;
        }

        // =====================================================
        // AUTHORS PANEL (pixel art styled)
        // =====================================================

        private static GameObject BuildAuthorsPanel(Transform canvasTransform)
        {
            GameObject authorsPanel = new GameObject("AuthorsPanel");
            authorsPanel.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = authorsPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // --- Dark background with slight transparency ---
            Image panelBg = authorsPanel.AddComponent<Image>();
            panelBg.color = new Color(0.04f, 0.03f, 0.02f, 0.97f);

            // --- Decorative border frame ---
            GameObject borderFrame = new GameObject("BorderFrame");
            borderFrame.transform.SetParent(authorsPanel.transform, false);
            RectTransform borderRect = borderFrame.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.15f, 0.08f);
            borderRect.anchorMax = new Vector2(0.85f, 0.92f);
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;

            Image borderImg = borderFrame.AddComponent<Image>();
            borderImg.color = new Color(0.35f, 0.22f, 0.1f, 1f); // Dark wood frame

            // --- Inner panel (darker) ---
            GameObject innerPanel = new GameObject("InnerPanel");
            innerPanel.transform.SetParent(borderFrame.transform, false);
            RectTransform innerRect = innerPanel.AddComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.02f, 0.02f);
            innerRect.anchorMax = new Vector2(0.98f, 0.98f);
            innerRect.offsetMin = Vector2.zero;
            innerRect.offsetMax = Vector2.zero;

            Image innerBg = innerPanel.AddComponent<Image>();
            innerBg.color = new Color(0.08f, 0.06f, 0.04f, 1f);

            // --- Gold accent line at top ---
            GameObject goldLine = new GameObject("GoldLine");
            goldLine.transform.SetParent(innerPanel.transform, false);
            RectTransform goldRect = goldLine.AddComponent<RectTransform>();
            goldRect.anchorMin = new Vector2(0.1f, 0.88f);
            goldRect.anchorMax = new Vector2(0.9f, 0.89f);
            goldRect.offsetMin = Vector2.zero;
            goldRect.offsetMax = Vector2.zero;

            Image goldImg = goldLine.AddComponent<Image>();
            goldImg.color = new Color(0.85f, 0.65f, 0.25f, 1f);

            // --- Title: "GRE STWORZYLI" ---
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(innerPanel.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.78f);
            titleRect.anchorMax = new Vector2(0.9f, 0.88f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI titleTmp = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
            titleTmp.text = "GR\u0118 STWORZYLI";
            titleTmp.fontSize = 42;
            titleTmp.color = new Color(0.9f, 0.7f, 0.3f, 1f); // Gold
            titleTmp.alignment = TMPro.TextAlignmentOptions.Center;
            titleTmp.fontStyle = TMPro.FontStyles.Bold;

            // --- Gold accent line below title ---
            GameObject goldLine2 = new GameObject("GoldLine2");
            goldLine2.transform.SetParent(innerPanel.transform, false);
            RectTransform gold2Rect = goldLine2.AddComponent<RectTransform>();
            gold2Rect.anchorMin = new Vector2(0.1f, 0.77f);
            gold2Rect.anchorMax = new Vector2(0.9f, 0.78f);
            gold2Rect.offsetMin = Vector2.zero;
            gold2Rect.offsetMax = Vector2.zero;

            Image gold2Img = goldLine2.AddComponent<Image>();
            gold2Img.color = new Color(0.85f, 0.65f, 0.25f, 1f);

            // --- Author Names ---
            GameObject namesObj = new GameObject("AuthorNames");
            namesObj.transform.SetParent(innerPanel.transform, false);
            RectTransform namesRect = namesObj.AddComponent<RectTransform>();
            namesRect.anchorMin = new Vector2(0.1f, 0.25f);
            namesRect.anchorMax = new Vector2(0.9f, 0.75f);
            namesRect.offsetMin = Vector2.zero;
            namesRect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI namesTmp = namesObj.AddComponent<TMPro.TextMeshProUGUI>();
            namesTmp.text =
                "Daniel Borowski\n\n" +
                "Micha\u0142 Niek\u0142a\u0144\n\n" +
                "Jarek M\u0142odziejewski\n\n" +
                "Kuba Ciecierski\n\n" +
                "Radek M\u0142ynarczyk";
            namesTmp.fontSize = 32;
            namesTmp.color = new Color(0.95f, 0.9f, 0.8f, 1f); // Warm white
            namesTmp.alignment = TMPro.TextAlignmentOptions.Center;
            namesTmp.lineSpacing = 5f;

            // --- Project subtitle ---
            GameObject subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(innerPanel.transform, false);
            RectTransform subRect = subtitleObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.1f, 0.15f);
            subRect.anchorMax = new Vector2(0.9f, 0.22f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI subTmp = subtitleObj.AddComponent<TMPro.TextMeshProUGUI>();
            subTmp.text = "Lochy i Gorza\u0142a \u2022 Projekt Zaliczeniowy \u2022 2026";
            subTmp.fontSize = 23;
            subTmp.color = new Color(0.6f, 0.5f, 0.35f, 1f); // Muted gold
            subTmp.alignment = TMPro.TextAlignmentOptions.Center;
            subTmp.fontStyle = TMPro.FontStyles.Italic;

            // --- Back Button ---
            GameObject backBtn = new GameObject("BackButton");
            backBtn.transform.SetParent(authorsPanel.transform, false);
            RectTransform backRect = backBtn.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.4f, 0.02f);
            backRect.anchorMax = new Vector2(0.6f, 0.07f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            Image backBg = backBtn.AddComponent<Image>();
            backBg.color = new Color(0.35f, 0.22f, 0.1f, 1f);

            Button backButton = backBtn.AddComponent<Button>();
            ColorBlock backColors = backButton.colors;
            backColors.normalColor = new Color(0.35f, 0.22f, 0.1f, 1f);
            backColors.highlightedColor = new Color(0.5f, 0.32f, 0.15f, 1f);
            backColors.pressedColor = new Color(0.25f, 0.15f, 0.08f, 1f);
            backButton.colors = backColors;

            GameObject backTextObj = new GameObject("Text");
            backTextObj.transform.SetParent(backBtn.transform, false);
            RectTransform backTextRect = backTextObj.AddComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.sizeDelta = Vector2.zero;

            TMPro.TextMeshProUGUI backTmp = backTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            backTmp.text = "POWR\u00d3T";
            backTmp.fontSize = 26;
            backTmp.color = new Color(0.9f, 0.75f, 0.4f, 1f);
            backTmp.alignment = TMPro.TextAlignmentOptions.Center;
            backTmp.fontStyle = TMPro.FontStyles.Bold;

            return authorsPanel;
        }

        // =====================================================
        // OPTIONS PANEL (black screen with DLC joke)
        // =====================================================

        private static GameObject BuildOptionsPanel(Transform canvasTransform)
        {
            GameObject optionsPanel = new GameObject("OptionsPanel");
            optionsPanel.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = optionsPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // --- Solid black background ---
            Image panelBg = optionsPanel.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 1f);

            // --- DLC message centered ---
            GameObject msgObj = new GameObject("DlcMessage");
            msgObj.transform.SetParent(optionsPanel.transform, false);
            RectTransform msgRect = msgObj.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.1f, 0.35f);
            msgRect.anchorMax = new Vector2(0.9f, 0.65f);
            msgRect.offsetMin = Vector2.zero;
            msgRect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI msgTmp = msgObj.AddComponent<TMPro.TextMeshProUGUI>();
            msgTmp.text = "Opcje pojawi\u0105 si\u0119 w pierwszym p\u0142atnym DLC :)";
            msgTmp.fontSize = 38;
            msgTmp.color = new Color(0.9f, 0.7f, 0.3f, 1f); // Gold
            msgTmp.alignment = TMPro.TextAlignmentOptions.Center;
            msgTmp.fontStyle = TMPro.FontStyles.Bold;

            // --- Back Button ---
            GameObject backBtn = new GameObject("BackButton");
            backBtn.transform.SetParent(optionsPanel.transform, false);
            RectTransform backRect = backBtn.AddComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.4f, 0.12f);
            backRect.anchorMax = new Vector2(0.6f, 0.18f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            Image backBg = backBtn.AddComponent<Image>();
            backBg.color = new Color(0.35f, 0.22f, 0.1f, 1f);

            Button backButton = backBtn.AddComponent<Button>();
            ColorBlock backColors = backButton.colors;
            backColors.normalColor = new Color(0.35f, 0.22f, 0.1f, 1f);
            backColors.highlightedColor = new Color(0.5f, 0.32f, 0.15f, 1f);
            backColors.pressedColor = new Color(0.25f, 0.15f, 0.08f, 1f);
            backButton.colors = backColors;

            GameObject backTextObj = new GameObject("Text");
            backTextObj.transform.SetParent(backBtn.transform, false);
            RectTransform backTextRect = backTextObj.AddComponent<RectTransform>();
            backTextRect.anchorMin = Vector2.zero;
            backTextRect.anchorMax = Vector2.one;
            backTextRect.sizeDelta = Vector2.zero;

            TMPro.TextMeshProUGUI backTmp = backTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            backTmp.text = "POWR\u00d3T";
            backTmp.fontSize = 26;
            backTmp.color = new Color(0.9f, 0.75f, 0.4f, 1f);
            backTmp.alignment = TMPro.TextAlignmentOptions.Center;
            backTmp.fontStyle = TMPro.FontStyles.Bold;

            return optionsPanel;
        }

        // =====================================================
        // DUNGEON SCENE (with EnemySpawner)
        // =====================================================

        private static void BuildDungeonScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.orthographic = true;
            // orthoSize 6.5 ⇒ visible area ~23×13 tiles (16:9). Fits nicely inside the 36×26 dungeon
            // so the camera actually moves to follow the player instead of sitting static.
            cam.orthographicSize = 6.5f;
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cameraObj.tag = "MainCamera";
            cameraObj.transform.position = new Vector3(18f, 13f, -10f);
            cameraObj.AddComponent<AudioListener>();
            Player.CameraFollow camFollow = cameraObj.AddComponent<Player.CameraFollow>();

            GameObject gridObj = new GameObject("Grid");
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0f);

            GameObject floorTmObj = new GameObject("FloorTilemap");
            floorTmObj.transform.SetParent(gridObj.transform, false);
            Tilemap floorTm = floorTmObj.AddComponent<Tilemap>();
            floorTmObj.AddComponent<TilemapRenderer>().sortingOrder = 0;

            GameObject wallTmObj = new GameObject("WallTilemap");
            wallTmObj.transform.SetParent(gridObj.transform, false);
            Tilemap wallTm = wallTmObj.AddComponent<Tilemap>();
            wallTmObj.AddComponent<TilemapRenderer>().sortingOrder = 1;
            wallTmObj.AddComponent<TilemapCollider2D>();

            GameObject decorTmObj = new GameObject("DecorTilemap");
            decorTmObj.transform.SetParent(gridObj.transform, false);
            Tilemap decorTm = decorTmObj.AddComponent<Tilemap>();
            decorTmObj.AddComponent<TilemapRenderer>().sortingOrder = 2;

            // DungeonGenerator
            GameObject dungeonGenObj = new GameObject("DungeonGenerator");
            Dungeon.DungeonGenerator dungeonGen = dungeonGenObj.AddComponent<Dungeon.DungeonGenerator>();
            Texture2D tileSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(TileSheetPath);

            SerializedObject dgSo = new SerializedObject(dungeonGen);
            dgSo.FindProperty("floorTilemap").objectReferenceValue = floorTm;
            dgSo.FindProperty("wallTilemap").objectReferenceValue = wallTm;
            dgSo.FindProperty("decorTilemap").objectReferenceValue = decorTm;
            dgSo.FindProperty("tileSheet").objectReferenceValue = tileSheet;
            dgSo.ApplyModifiedPropertiesWithoutUndo();

            // EnemySpawner
            GameObject spawnerObj = new GameObject("EnemySpawner");
            Combat.EnemySpawner spawner = spawnerObj.AddComponent<Combat.EnemySpawner>();
            Texture2D monstersSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(MonstersSheetPath);

            SerializedObject spSo = new SerializedObject(spawner);
            spSo.FindProperty("monstersSheet").objectReferenceValue = monstersSheet;
            spSo.ApplyModifiedPropertiesWithoutUndo();

            // Player
            GameObject playerObj = new GameObject("Player");
            playerObj.tag = "Player";
            playerObj.transform.position = new Vector3(5f, 5f, 0f);
            SpriteRenderer playerSr = playerObj.AddComponent<SpriteRenderer>();
            playerSr.sortingOrder = 10;

            Player.PlayerController pc = playerObj.AddComponent<Player.PlayerController>();
            Texture2D rogueSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(RogueSheetPath);

            SerializedObject pcSo = new SerializedObject(pc);
            pcSo.FindProperty("rogueSheet").objectReferenceValue = rogueSheet;
            pcSo.FindProperty("dungeonGenerator").objectReferenceValue = dungeonGen;
            pcSo.FindProperty("enemySpawner").objectReferenceValue = spawner;
            pcSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject cfSo = new SerializedObject(camFollow);
            cfSo.FindProperty("target").objectReferenceValue = playerObj.transform;
            cfSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<Managers.GameManager>();

            // ============================
            // HUD: EventSystem + Canvas with Gear button + In-Game Menu + Save Slots
            // ============================
            GameObject evtSys = new GameObject("EventSystem");
            evtSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evtSys.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            GameObject hudCanvas = new GameObject("HUDCanvas");
            Canvas hud = hudCanvas.AddComponent<Canvas>();
            hud.renderMode = RenderMode.ScreenSpaceOverlay;
            hud.sortingOrder = 100;
            CanvasScaler hudScaler = hudCanvas.AddComponent<CanvasScaler>();
            hudScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            hudScaler.referenceResolution = new Vector2(1280, 720);
            hudScaler.matchWidthOrHeight = 0.5f;
            hudCanvas.AddComponent<GraphicRaycaster>();

            // Gear icon button (top-left corner)
            GameObject gearBtn = new GameObject("GearButton");
            gearBtn.transform.SetParent(hudCanvas.transform, false);
            RectTransform gearRect = gearBtn.AddComponent<RectTransform>();
            gearRect.anchorMin = new Vector2(0f, 1f);
            gearRect.anchorMax = new Vector2(0f, 1f);
            gearRect.pivot     = new Vector2(0f, 1f);
            gearRect.anchoredPosition = new Vector2(10f, -10f);
            gearRect.sizeDelta = new Vector2(44f, 44f);
            Image gearImg = gearBtn.AddComponent<Image>();
            gearImg.color = new Color(0.15f, 0.12f, 0.08f, 0.85f);
            Button gearBtnComp = gearBtn.AddComponent<Button>();
            gearBtnComp.targetGraphic = gearImg;
            ColorBlock gearCb = gearBtnComp.colors;
            gearCb.highlightedColor = new Color(0.9f, 0.7f, 0.2f, 0.9f);
            gearBtnComp.colors = gearCb;
            // Gear icon label (⚙)
            GameObject gearLabelObj = new GameObject("GearLabel");
            gearLabelObj.transform.SetParent(gearBtn.transform, false);
            RectTransform glRect = gearLabelObj.AddComponent<RectTransform>();
            glRect.anchorMin = Vector2.zero; glRect.anchorMax = Vector2.one;
            glRect.offsetMin = Vector2.zero; glRect.offsetMax = Vector2.zero;
            var gearTxt = gearLabelObj.AddComponent<TMPro.TextMeshProUGUI>();
            gearTxt.text = "≡";
            gearTxt.fontSize = 26;
            gearTxt.alignment = TMPro.TextAlignmentOptions.Center;
            gearTxt.color = new Color(0.9f, 0.75f, 0.3f);

            // ── Floor label (top-left, right of gear button) ──────────────
            GameObject floorLabelObj = new GameObject("FloorLabel");
            floorLabelObj.transform.SetParent(hudCanvas.transform, false);
            RectTransform floorLabelRect = floorLabelObj.AddComponent<RectTransform>();
            floorLabelRect.anchorMin = new Vector2(0f, 1f);
            floorLabelRect.anchorMax = new Vector2(0f, 1f);
            floorLabelRect.pivot     = new Vector2(0f, 1f);
            floorLabelRect.anchoredPosition = new Vector2(62f, -10f);
            floorLabelRect.sizeDelta = new Vector2(200f, 40f);
            var floorTmp = floorLabelObj.AddComponent<TMPro.TextMeshProUGUI>();
            floorTmp.text = "Przedsionek";
            floorTmp.fontSize = 23;
            floorTmp.alignment = TMPro.TextAlignmentOptions.Left;
            floorTmp.color = new Color(0.9f, 0.8f, 0.5f, 0.9f);

            // In-game menu panel (hidden by default)
            GameObject menuPanel = new GameObject("InGameMenuPanel");
            menuPanel.transform.SetParent(hudCanvas.transform, false);
            RectTransform menuRect = menuPanel.AddComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0f, 0.5f);
            menuRect.anchorMax = new Vector2(0f, 0.5f);
            menuRect.pivot = new Vector2(0f, 0.5f);
            menuRect.anchoredPosition = new Vector2(10f, 0f);
            menuRect.sizeDelta = new Vector2(200f, 150f);
            Image menuBg = menuPanel.AddComponent<Image>();
            menuBg.color = new Color(0.08f, 0.06f, 0.04f, 0.97f);

            Button saveMenuBtn   = CreateSimpleButton(menuPanel.transform, "SaveBtn",   "Zapisz grę",    new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0f,-10f), new Vector2(0f,-50f));
            Button returnMenuBtn = CreateSimpleButton(menuPanel.transform, "ReturnBtn", "Menu główne",   new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0f,-60f), new Vector2(0f,-100f));
            Button resumeMenuBtn = CreateSimpleButton(menuPanel.transform, "ResumeBtn", "Wróć do gry",   new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0f,-110f), new Vector2(0f,-150f));

            // Save slot panel (for in-game saves)
            GameObject dungeonSaveSlotObj = BuildSaveSlotPanel(hudCanvas.transform);

            // Wire InGameMenuPanel controller
            GameObject inGameCtrlObj = new GameObject("InGameMenuController");
            inGameCtrlObj.transform.SetParent(hudCanvas.transform, false);
            UI.InGameMenuPanel inGameCtrl = inGameCtrlObj.AddComponent<UI.InGameMenuPanel>();
            SerializedObject igSo = new SerializedObject(inGameCtrl);
            igSo.FindProperty("gearButton").objectReferenceValue = gearBtnComp;
            igSo.FindProperty("menuPanel").objectReferenceValue = menuPanel;
            igSo.FindProperty("saveButton").objectReferenceValue = saveMenuBtn;
            igSo.FindProperty("returnToMenuButton").objectReferenceValue = returnMenuBtn;
            igSo.FindProperty("resumeButton").objectReferenceValue = resumeMenuBtn;
            igSo.FindProperty("saveSlotPanel").objectReferenceValue =
                dungeonSaveSlotObj.GetComponent<UI.SaveSlotPanel>();
            igSo.FindProperty("floorLabel").objectReferenceValue = floorTmp;
            // bagButton + inventoryUI wired AFTER we create them below
            // (igSo.ApplyModifiedPropertiesWithoutUndo called after bag/panel creation)

            // ── EKWIPUNEK BUTTON (TOP-RIGHT corner — clear of gear/menu) ─────
            GameObject bagBtn = new GameObject("EkwipunekButton");
            bagBtn.transform.SetParent(hudCanvas.transform, false);
            RectTransform bagRect = bagBtn.AddComponent<RectTransform>();
            bagRect.anchorMin        = new Vector2(1f, 1f);
            bagRect.anchorMax        = new Vector2(1f, 1f);
            bagRect.pivot            = new Vector2(1f, 1f);
            bagRect.anchoredPosition = new Vector2(-10f, -10f);
            bagRect.sizeDelta        = new Vector2(140f, 38f);
            Image bagImg = bagBtn.AddComponent<Image>();
            bagImg.color = new Color(0.12f, 0.08f, 0.05f, 0.92f);
            Button bagBtnComp = bagBtn.AddComponent<Button>();
            bagBtnComp.targetGraphic = bagImg;
            ColorBlock bagCb = bagBtnComp.colors;
            bagCb.normalColor      = new Color(0.12f, 0.08f, 0.05f, 0.92f);
            bagCb.highlightedColor = new Color(0.6f, 0.45f, 0.1f, 1f);
            bagCb.pressedColor     = new Color(0.4f, 0.3f, 0.05f, 1f);
            bagBtnComp.colors = bagCb;
            GameObject bagLabelObj = new GameObject("EkwipunekLabel");
            bagLabelObj.transform.SetParent(bagBtn.transform, false);
            RectTransform blRect = bagLabelObj.AddComponent<RectTransform>();
            blRect.anchorMin = Vector2.zero; blRect.anchorMax = Vector2.one;
            blRect.offsetMin = new Vector2(4f, 2f); blRect.offsetMax = new Vector2(-4f, -2f);
            var bagTxt = bagLabelObj.AddComponent<TMPro.TextMeshProUGUI>();
            bagTxt.text      = "EKWIPUNEK";
            bagTxt.fontSize  = 16;
            bagTxt.fontStyle = TMPro.FontStyles.Bold;
            bagTxt.alignment = TMPro.TextAlignmentOptions.Center;
            bagTxt.color     = new Color(0.95f, 0.82f, 0.3f);
            bagTxt.enableAutoSizing = true;
            bagTxt.fontSizeMin = 12f;
            bagTxt.fontSizeMax = 16f;

            // ── INVENTORY PANEL (full-screen overlay on HUDCanvas) ────────────
            Texture2D itemsTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ItemsSheetPath);
            (GameObject invPanel, UI.InventoryUIController invCtrl) =
                BuildInventoryPanel(hudCanvas.transform, itemsTex, inCombat: false);

            // ── SHOP PANEL (full-screen overlay on HUDCanvas) ────────────────
            (GameObject shopPanel, UI.ShopUIController shopCtrl) =
                BuildShopPanel(hudCanvas.transform, itemsTex);

            // ── NPC SPAWNER ───────────────────────────────────────────────────
            Texture2D merchantTex = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Art/32rogues-0.5.0/32rogues/rogues.png");
            GameObject npcSpawnerObj = new GameObject("NpcSpawner");
            NPC.NpcSpawner npcSpawner = npcSpawnerObj.AddComponent<NPC.NpcSpawner>();
            SerializedObject npcSo = new SerializedObject(npcSpawner);
            npcSo.FindProperty("merchantSheet").objectReferenceValue = merchantTex;
            // Use row=3, col=4 from rogues.png as Mirek (a hooded figure)
            npcSo.FindProperty("merchantSpriteCol").intValue = 4;
            npcSo.FindProperty("merchantSpriteRow").intValue = 3;
            npcSo.FindProperty("shopUI").objectReferenceValue = shopCtrl;
            npcSo.ApplyModifiedPropertiesWithoutUndo();

            // ── Wire bagButton + inventoryUI into InGameMenuPanel via SerializedObject
            igSo.FindProperty("bagButton").objectReferenceValue   = bagBtnComp;
            igSo.FindProperty("inventoryUI").objectReferenceValue = invCtrl;
            igSo.ApplyModifiedPropertiesWithoutUndo();

            // ── QUEST JOURNAL (toggle button + panel on HUDCanvas) ──────────
            BuildQuestJournal(hudCanvas.transform);

            // ── ACHIEVEMENTS PANEL (toggle button + panel on HUDCanvas) ─────
            BuildAchievementsPanel(hudCanvas.transform);

            // ── NOTIFICATION OVERLAY (timed messages on HUDCanvas) ───────────
            BuildNotificationOverlay(hudCanvas.transform);

            EditorSceneManager.SaveScene(scene, DungeonScenePath);
            Debug.Log("Dungeon scene created at: " + DungeonScenePath);
        }

        /// <summary>
        /// Builds the quest journal: a toggle button (top-left) + a panel with quest list.
        /// </summary>
        private static void BuildQuestJournal(Transform canvasParent)
        {
            // ── Controller object ────────────────────────────────────
            GameObject ctrlObj = new GameObject("QuestJournalController");
            ctrlObj.transform.SetParent(canvasParent, false);
            var ctrl = ctrlObj.AddComponent<UI.QuestJournalUIController>();

            // ── Toggle button (top-left corner) ──────────────────────
            GameObject btnObj = new GameObject("QuestJournalButton");
            btnObj.transform.SetParent(canvasParent, false);
            RectTransform btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 1f);
            btnRt.anchorMax = new Vector2(0f, 1f);
            btnRt.pivot     = new Vector2(0f, 1f);
            btnRt.anchoredPosition = new Vector2(10f, -55f);
            btnRt.sizeDelta = new Vector2(120f, 36f);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.15f, 0.12f, 0.08f, 0.85f);
            Button btn = btnObj.AddComponent<Button>();
            var btnColors = btn.colors;
            btnColors.normalColor      = new Color(0.15f, 0.12f, 0.08f, 0.85f);
            btnColors.highlightedColor = new Color(0.25f, 0.22f, 0.15f, 0.95f);
            btnColors.pressedColor     = new Color(0.35f, 0.30f, 0.20f, 1f);
            btn.colors = btnColors;

            // Button label
            GameObject btnLabel = new GameObject("Label");
            btnLabel.transform.SetParent(btnObj.transform, false);
            RectTransform lblRt = btnLabel.AddComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
            var lblTmp = btnLabel.AddComponent<TMPro.TextMeshProUGUI>();
            lblTmp.text      = "Dziennik";
            lblTmp.fontSize  = 16f;
            lblTmp.alignment = TMPro.TextAlignmentOptions.Center;
            lblTmp.color     = new Color(1f, 0.84f, 0f); // gold

            // ── Journal panel (left side, semi-transparent) ──────────
            GameObject panel = new GameObject("QuestJournalPanel");
            panel.transform.SetParent(canvasParent, false);
            RectTransform prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0f, 0.15f);
            prt.anchorMax = new Vector2(0.35f, 0.85f);
            prt.offsetMin = new Vector2(10f, 0f);
            prt.offsetMax = new Vector2(0f, -52f); // leave room below button row
            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.06f, 0.04f, 0.90f);

            // Journal text
            GameObject textObj = new GameObject("JournalText");
            textObj.transform.SetParent(panel.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 12f);
            trt.offsetMax = new Vector2(-12f, -12f);
            var journalTmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            journalTmp.fontSize  = 16f;
            journalTmp.alignment = TMPro.TextAlignmentOptions.TopLeft;
            journalTmp.color     = Color.white;
            journalTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            journalTmp.overflowMode = TMPro.TextOverflowModes.Ellipsis;

            panel.SetActive(false); // starts hidden

            // ── Wire references via SerializedObject ─────────────────
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("journalPanel").objectReferenceValue = panel;
            so.FindProperty("journalText").objectReferenceValue  = journalTmp;
            so.FindProperty("toggleButton").objectReferenceValue = btn;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[ProjectSetupEditor] Quest journal UI built.");
        }

        /// <summary>
        /// Builds the achievements panel: a toggle button (top-left, below quest journal)
        /// + a panel on the right side showing all achievements.
        /// </summary>
        private static void BuildAchievementsPanel(Transform canvasParent)
        {
            // ── Controller object ────────────────────────────────────
            GameObject ctrlObj = new GameObject("AchievementsController");
            ctrlObj.transform.SetParent(canvasParent, false);
            var ctrl = ctrlObj.AddComponent<UI.AchievementsUIController>();

            // ── Toggle button (top-left, below Dziennik button) ─────
            GameObject btnObj = new GameObject("AchievementsButton");
            btnObj.transform.SetParent(canvasParent, false);
            RectTransform btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0f, 1f);
            btnRt.anchorMax = new Vector2(0f, 1f);
            btnRt.pivot     = new Vector2(0f, 1f);
            btnRt.anchoredPosition = new Vector2(138f, -55f); // right of Dziennik button
            btnRt.sizeDelta = new Vector2(120f, 36f);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.12f, 0.08f, 0.15f, 0.85f);
            Button btn = btnObj.AddComponent<Button>();
            var btnColors = btn.colors;
            btnColors.normalColor      = new Color(0.12f, 0.08f, 0.15f, 0.85f);
            btnColors.highlightedColor = new Color(0.22f, 0.15f, 0.25f, 0.95f);
            btnColors.pressedColor     = new Color(0.30f, 0.20f, 0.35f, 1f);
            btn.colors = btnColors;

            // Button label
            GameObject btnLabel = new GameObject("Label");
            btnLabel.transform.SetParent(btnObj.transform, false);
            RectTransform lblRt = btnLabel.AddComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
            var lblTmp = btnLabel.AddComponent<TMPro.TextMeshProUGUI>();
            lblTmp.text      = "Osiągnięcia";
            lblTmp.fontSize  = 14f;
            lblTmp.enableAutoSizing = true;
            lblTmp.fontSizeMin = 10f;
            lblTmp.fontSizeMax = 14f;
            lblTmp.alignment = TMPro.TextAlignmentOptions.Center;
            lblTmp.color     = new Color(0.8f, 0.6f, 1f); // light purple

            // ── Achievements panel (right side, semi-transparent) ───
            GameObject panel = new GameObject("AchievementsPanel");
            panel.transform.SetParent(canvasParent, false);
            RectTransform prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.60f, 0.10f);
            prt.anchorMax = new Vector2(0.98f, 0.90f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.06f, 0.04f, 0.08f, 0.92f);

            // Achievements text (scrollable content)
            GameObject textObj = new GameObject("AchievementsText");
            textObj.transform.SetParent(panel.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(12f, 12f);
            trt.offsetMax = new Vector2(-12f, -12f);
            var achTmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            achTmp.fontSize  = 15f;
            achTmp.alignment = TMPro.TextAlignmentOptions.TopLeft;
            achTmp.color     = Color.white;
            achTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            achTmp.overflowMode = TMPro.TextOverflowModes.Ellipsis;

            panel.SetActive(false); // starts hidden

            // ── Wire references via SerializedObject ─────────────────
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("achievementsPanel").objectReferenceValue = panel;
            so.FindProperty("achievementsText").objectReferenceValue  = achTmp;
            so.FindProperty("toggleButton").objectReferenceValue      = btn;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[ProjectSetupEditor] Achievements UI built.");
        }

        /// <summary>
        /// Builds the notification overlay UI: a centred panel with a text label
        /// managed by NotificationUIController. Starts hidden; shown via GameEvents.OnNotification.
        /// </summary>
        private static void BuildNotificationOverlay(Transform canvasParent)
        {
            // Controller object (invisible — just hosts the MonoBehaviour)
            GameObject ctrlObj = new GameObject("NotificationController");
            ctrlObj.transform.SetParent(canvasParent, false);
            var ctrl = ctrlObj.AddComponent<UI.NotificationUIController>();

            // Panel background — centred, semi-transparent dark box
            GameObject panel = new GameObject("NotificationPanel");
            panel.transform.SetParent(canvasParent, false);
            RectTransform prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.15f, 0.35f);
            prt.anchorMax = new Vector2(0.85f, 0.65f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.03f, 0.02f, 0.97f);
            panel.AddComponent<CanvasGroup>();

            // Message text
            GameObject txtObj = new GameObject("NotificationText");
            txtObj.transform.SetParent(panel.transform, false);
            RectTransform trt = txtObj.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.05f, 0.05f);
            trt.anchorMax = new Vector2(0.95f, 0.95f);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var tmp = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 26;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.9f, 0.5f);
            tmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Wire references via SerializedObject
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.FindProperty("messageText").objectReferenceValue = tmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            panel.SetActive(false);
        }

        /// <summary>Creates a simple text button anchored with offsetMin/offsetMax.</summary>
        private static Button CreateSimpleButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.1f, 0.06f, 0.9f);
            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.9f, 0.7f, 0.2f, 1f);
            btn.colors = cb;

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(obj.transform, false);
            RectTransform txtRt = txtObj.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
            var txt = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
            txt.text = label; txt.fontSize = 23;
            txt.alignment = TMPro.TextAlignmentOptions.Center;
            txt.color = new Color(0.9f, 0.8f, 0.6f);
            return btn;
        }

        // =====================================================
        // COMBAT SCENE (Pokemon Fire Red style)
        // =====================================================

        private static void BuildCombatScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject cameraObj = new GameObject("Main Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cameraObj.tag = "MainCamera";
            cameraObj.AddComponent<AudioListener>();

            // GameManager (backup if not carried from previous scene)
            GameObject gmObj = new GameObject("GameManager");
            gmObj.AddComponent<Managers.GameManager>();

            // Event System
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Canvas
            GameObject canvasObj = new GameObject("CombatCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // ============================
            // BACKGROUND (dark dungeon gradient)
            // ============================
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);
            SetFullStretch(bgObj);
            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.06f, 0.1f);

            // ============================
            // ENEMY AREA (top half)
            // ============================
            // Enemy sprite (large, centered)
            GameObject enemyImgObj = new GameObject("EnemyImage");
            enemyImgObj.transform.SetParent(canvasObj.transform, false);
            RectTransform enemyImgRect = enemyImgObj.AddComponent<RectTransform>();
            enemyImgRect.anchorMin = new Vector2(0.35f, 0.50f);
            enemyImgRect.anchorMax = new Vector2(0.65f, 0.85f);
            enemyImgRect.offsetMin = Vector2.zero;
            enemyImgRect.offsetMax = Vector2.zero;
            Image enemyImg = enemyImgObj.AddComponent<Image>();
            enemyImg.preserveAspect = true;
            enemyImg.color = Color.white;

            // Enemy info box (top right, Pokemon style)
            GameObject enemyInfoBox = CreateInfoBox(canvasObj.transform, "EnemyInfoBox",
                new Vector2(0.55f, 0.82f), new Vector2(0.95f, 0.97f),
                new Color(0.15f, 0.12f, 0.08f, 0.95f));

            TMPro.TextMeshProUGUI enemyNameTxt = CreateTextInParent(enemyInfoBox, "EnemyName",
                new Vector2(0.05f, 0.55f), new Vector2(0.6f, 0.95f), "Utopiec", 22,
                new Color(0.95f, 0.9f, 0.8f));

            TMPro.TextMeshProUGUI enemyLevelTxt = CreateTextInParent(enemyInfoBox, "EnemyLevel",
                new Vector2(0.65f, 0.55f), new Vector2(0.95f, 0.95f), "Poz. 2", 18,
                new Color(0.7f, 0.65f, 0.5f));

            // Enemy HP bar background
            GameObject enemyHPBg = CreateBar(enemyInfoBox, "EnemyHPBarBg",
                new Vector2(0.05f, 0.1f), new Vector2(0.75f, 0.45f),
                new Color(0.2f, 0.15f, 0.1f));

            // Enemy HP bar fill
            GameObject enemyHPFill = CreateBar(enemyHPBg, "EnemyHPBarFill",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Color(0.8f, 0.2f, 0.15f));
            // HP fill uses RectTransform anchor scaling (not fillAmount — that requires a sprite)
            // CombatUIController.UpdateUI() sets anchorMax.x to the HP ratio

            TMPro.TextMeshProUGUI enemyHPTxt = CreateTextInParent(enemyInfoBox, "EnemyHPText",
                new Vector2(0.78f, 0.1f), new Vector2(0.98f, 0.45f), "45/45", 14,
                new Color(0.85f, 0.8f, 0.7f));

            // ============================
            // PLAYER INFO BOX (bottom left, Pokemon style)
            // ============================
            GameObject playerInfoBox = CreateInfoBox(canvasObj.transform, "PlayerInfoBox",
                new Vector2(0.02f, 0.28f), new Vector2(0.45f, 0.48f),
                new Color(0.15f, 0.12f, 0.08f, 0.95f));

            TMPro.TextMeshProUGUI playerNameTxt = CreateTextInParent(playerInfoBox, "PlayerName",
                new Vector2(0.05f, 0.6f), new Vector2(0.55f, 0.95f), "Gniewko", 22,
                new Color(0.95f, 0.9f, 0.8f));

            TMPro.TextMeshProUGUI playerLevelTxt = CreateTextInParent(playerInfoBox, "PlayerLevel",
                new Vector2(0.6f, 0.6f), new Vector2(0.95f, 0.95f), "Poz. 1", 18,
                new Color(0.7f, 0.65f, 0.5f));

            // Player HP bar
            GameObject playerHPBg = CreateBar(playerInfoBox, "PlayerHPBarBg",
                new Vector2(0.05f, 0.25f), new Vector2(0.65f, 0.55f),
                new Color(0.2f, 0.15f, 0.1f));

            GameObject playerHPFill = CreateBar(playerHPBg, "PlayerHPBarFill",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Color(0.2f, 0.75f, 0.25f));
            // HP fill uses RectTransform anchor scaling (not fillAmount — that requires a sprite)

            TMPro.TextMeshProUGUI playerHPTxt = CreateTextInParent(playerInfoBox, "PlayerHPText",
                new Vector2(0.68f, 0.25f), new Vector2(0.98f, 0.55f), "100/100", 14,
                new Color(0.85f, 0.8f, 0.7f));

            // Player AP text
            TMPro.TextMeshProUGUI playerAPTxt = CreateTextInParent(playerInfoBox, "PlayerAPText",
                new Vector2(0.05f, 0.02f), new Vector2(0.55f, 0.22f), "Wigor: 3/3", 16,
                new Color(0.4f, 0.7f, 0.9f));

            // Toxicity bar
            GameObject toxBg = CreateBar(playerInfoBox, "ToxBarBg",
                new Vector2(0.58f, 0.02f), new Vector2(0.95f, 0.2f),
                new Color(0.15f, 0.1f, 0.15f));

            GameObject toxFill = CreateBar(toxBg, "ToxBarFill",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Color(0.5f, 0.1f, 0.6f));
            // Toxicity fill uses RectTransform anchor scaling (starts at 0)
            toxFill.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);

            // ============================
            // MESSAGE BOX (bottom, Pokemon style)
            // ============================
            GameObject msgBox = CreateInfoBox(canvasObj.transform, "MessageBox",
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.25f),
                new Color(0.12f, 0.1f, 0.07f, 0.95f));

            // Gold border for message box
            Outline msgOutline = msgBox.AddComponent<Outline>();
            msgOutline.effectColor = new Color(0.6f, 0.45f, 0.2f);
            msgOutline.effectDistance = new Vector2(2, 2);

            TMPro.TextMeshProUGUI msgTxt = CreateTextInParent(msgBox, "MessageText",
                new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.95f),
                "Walka rozpoczyna sie!", 20, new Color(0.95f, 0.92f, 0.85f));
            msgTxt.alignment = TMPro.TextAlignmentOptions.TopLeft;

            // ============================
            // ACTION PANEL (right side, anchor-based 2-col layout)
            // ============================
            GameObject actionPanel = new GameObject("ActionPanel");
            actionPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform actRect = actionPanel.AddComponent<RectTransform>();
            actRect.anchorMin = new Vector2(0.52f, 0.22f);
            actRect.anchorMax = new Vector2(0.98f, 0.50f);
            actRect.offsetMin = Vector2.zero;
            actRect.offsetMax = Vector2.zero;

            Image actBg = actionPanel.AddComponent<Image>();
            actBg.color = new Color(0.1f, 0.08f, 0.06f, 0.9f);

            // Anchor-based button layout — buttons stretch to fill the panel.
            // 4 rows, 2 columns (last row: UCIEKAJ spans full width).
            // Padding: 2% from each edge, 1% gap between buttons.
            Color btnColor = new Color(0.3f, 0.2f, 0.1f);
            Color btnText = new Color(0.95f, 0.85f, 0.6f);

            // Row anchors (bottom-up): row0=top, row3=bottom
            // 4 rows with ~1% vertical gap: each row ~23.5% tall
            float pad = 0.02f;           // edge padding
            float gap = 0.01f;           // gap between cells
            float colMid = 0.50f;        // column split
            float rowH = (1f - 2f * pad - 3f * gap) / 4f; // row height

            // Helper: row Y anchors (top-down: row 0 at top)
            float RowTop(int r) => 1f - pad - r * (rowH + gap);
            float RowBot(int r) => RowTop(r) - rowH;
            float colL = pad;
            float colR = 1f - pad;

            // Row 0: LEKKI ATAK | CIĘŻKI ATAK
            GameObject lightAtkBtn = CreateCombatButtonAnchored(actionPanel.transform, "LightAttackBtn", "LEKKI ATAK",
                btnColor, btnText, new Vector2(colL, RowBot(0)), new Vector2(colMid - gap / 2f, RowTop(0)));
            GameObject heavyAtkBtn = CreateCombatButtonAnchored(actionPanel.transform, "HeavyAttackBtn", "CIĘŻKI ATAK",
                btnColor, btnText, new Vector2(colMid + gap / 2f, RowBot(0)), new Vector2(colR, RowTop(0)));

            // Row 1: ULECZ SIĘ | WYPIJ BIMBER
            GameObject healBtn = CreateCombatButtonAnchored(actionPanel.transform, "HealBtn", "ULECZ SIĘ",
                new Color(0.1f, 0.3f, 0.15f), btnText, new Vector2(colL, RowBot(1)), new Vector2(colMid - gap / 2f, RowTop(1)));
            GameObject bimberBtn = CreateCombatButtonAnchored(actionPanel.transform, "BimberBtn", "WYPIJ BIMBER",
                new Color(0.35f, 0.15f, 0.3f), btnText, new Vector2(colMid + gap / 2f, RowBot(1)), new Vector2(colR, RowTop(1)));

            // Row 2: OBRONA | PLECAK
            GameObject defendBtn = CreateCombatButtonAnchored(actionPanel.transform, "DefendBtn", "OBRONA",
                new Color(0.15f, 0.2f, 0.35f), btnText, new Vector2(colL, RowBot(2)), new Vector2(colMid - gap / 2f, RowTop(2)));
            Color bagBtnCol = new Color(0.2f, 0.25f, 0.15f);
            GameObject bagCombatBtn = CreateCombatButtonAnchored(actionPanel.transform, "BagBtn", "PLECAK",
                bagBtnCol, btnText, new Vector2(colMid + gap / 2f, RowBot(2)), new Vector2(colR, RowTop(2)));

            // Row 3: UCIEKAJ (full width)
            GameObject fleeBtn = CreateCombatButtonAnchored(actionPanel.transform, "FleeBtn", "UCIEKAJ",
                new Color(0.4f, 0.15f, 0.1f), btnText, new Vector2(colL, RowBot(3)), new Vector2(colR, RowTop(3)));

            // ============================
            // COMBAT UI CONTROLLER
            // ============================
            GameObject controllerObj = new GameObject("CombatUIController");
            controllerObj.transform.SetParent(canvasObj.transform, false);
            Combat.CombatUIController combatUI = controllerObj.AddComponent<Combat.CombatUIController>();

            Texture2D monstersTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MonstersSheetPath);

            SerializedObject cuiSo = new SerializedObject(combatUI);
            cuiSo.FindProperty("enemyImage").objectReferenceValue = enemyImg;
            cuiSo.FindProperty("enemyNameText").objectReferenceValue = enemyNameTxt;
            cuiSo.FindProperty("enemyLevelText").objectReferenceValue = enemyLevelTxt;
            cuiSo.FindProperty("enemyHPBar").objectReferenceValue = enemyHPFill.GetComponent<Image>();
            cuiSo.FindProperty("enemyHPText").objectReferenceValue = enemyHPTxt;
            cuiSo.FindProperty("playerNameText").objectReferenceValue = playerNameTxt;
            cuiSo.FindProperty("playerLevelText").objectReferenceValue = playerLevelTxt;
            cuiSo.FindProperty("playerHPBar").objectReferenceValue = playerHPFill.GetComponent<Image>();
            cuiSo.FindProperty("playerHPText").objectReferenceValue = playerHPTxt;
            cuiSo.FindProperty("playerAPText").objectReferenceValue = playerAPTxt;
            cuiSo.FindProperty("toxicityBar").objectReferenceValue = toxFill.GetComponent<Image>();
            cuiSo.FindProperty("lightAttackButton").objectReferenceValue = lightAtkBtn.GetComponent<Button>();
            cuiSo.FindProperty("heavyAttackButton").objectReferenceValue = heavyAtkBtn.GetComponent<Button>();
            cuiSo.FindProperty("healButton").objectReferenceValue = healBtn.GetComponent<Button>();
            cuiSo.FindProperty("bimberButton").objectReferenceValue = bimberBtn.GetComponent<Button>();
            cuiSo.FindProperty("defendButton").objectReferenceValue = defendBtn.GetComponent<Button>();
            cuiSo.FindProperty("fleeButton").objectReferenceValue = fleeBtn.GetComponent<Button>();
            cuiSo.FindProperty("messageText").objectReferenceValue = msgTxt;
            cuiSo.FindProperty("actionPanel").objectReferenceValue = actionPanel;
            cuiSo.FindProperty("messagePanel").objectReferenceValue = msgBox;

            // ── INVENTORY PANEL (overlay on CombatCanvas) ─────────────────────
            Texture2D itemsTex2 = AssetDatabase.LoadAssetAtPath<Texture2D>(ItemsSheetPath);
            (GameObject invPanel2, UI.InventoryUIController invCtrl2) =
                BuildInventoryPanel(canvasObj.transform, itemsTex2, inCombat: true);

            // Wire bagButton into CombatUIController via SerializedObject
            // (lambda onClick.AddListener does NOT serialize)
            cuiSo.FindProperty("monstersSheet").objectReferenceValue = monstersTex;
            cuiSo.FindProperty("backgroundImage").objectReferenceValue = bgImg;
            cuiSo.FindProperty("bagButton").objectReferenceValue =
                bagCombatBtn.GetComponent<Button>();
            cuiSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, CombatScenePath);
            Debug.Log("Combat scene created at: " + CombatScenePath);
        }

        // --- Combat UI Helpers ---

        private static void SetFullStretch(GameObject obj)
        {
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static GameObject CreateInfoBox(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            GameObject box = new GameObject(name);
            box.transform.SetParent(parent, false);
            RectTransform rt = box.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = box.AddComponent<Image>();
            img.color = bgColor;
            return box;
        }

        private static TMPro.TextMeshProUGUI CreateTextInParent(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, string text, int fontSize, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            TMPro.TextMeshProUGUI tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            return tmp;
        }

        private static GameObject CreateBar(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(parent.transform, false);
            RectTransform rt = bar.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = bar.AddComponent<Image>();
            img.color = color;
            return bar;
        }

        private static GameObject CreateCombatButton(Transform parent, string name,
            string text, Color bgColor, Color textColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(
                Mathf.Min(bgColor.r + 0.15f, 1f),
                Mathf.Min(bgColor.g + 0.12f, 1f),
                Mathf.Min(bgColor.b + 0.08f, 1f), 1f);
            colors.pressedColor = new Color(bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f, 1f);
            colors.disabledColor = new Color(0.2f, 0.18f, 0.15f, 0.6f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 17;
            tmp.color = textColor;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 12;
            tmp.fontSizeMax = 17;

            return btnObj;
        }

        /// <summary>
        /// Creates an anchor-stretched combat button that fills its allocated area.
        /// Unlike the GridLayout version, buttons resize with the panel — no fixed pixel size.
        /// </summary>
        private static GameObject CreateCombatButtonAnchored(Transform parent, string name,
            string text, Color bgColor, Color textColor, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(
                Mathf.Min(bgColor.r + 0.15f, 1f),
                Mathf.Min(bgColor.g + 0.12f, 1f),
                Mathf.Min(bgColor.b + 0.08f, 1f), 1f);
            colors.pressedColor = new Color(bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f, 1f);
            colors.disabledColor = new Color(0.2f, 0.18f, 0.15f, 0.6f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4f, 2f);
            textRect.offsetMax = new Vector2(-4f, -2f);

            TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = textColor;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14;
            tmp.fontSizeMax = 18;

            return btnObj;
        }

        // =====================================================
        // SHARED: SETTINGS PANEL (reused in MainMenu + Dungeon)
        // =====================================================

        /// <summary>
        /// Builds the settings panel: Volume slider, Fullscreen toggle, Resolution picker, Back button.
        /// Returns (root GameObject, SettingsPanel controller).
        /// </summary>
        private static (GameObject, UI.SettingsPanel) BuildSettingsPanel(Transform canvasParent)
        {
            // ── Root panel (centered overlay) ────────────────────────
            GameObject root = new GameObject("SettingsPanel");
            root.transform.SetParent(canvasParent, false);
            RectTransform rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0.2f, 0.15f);
            rootRt.anchorMax = new Vector2(0.8f, 0.85f);
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;
            Image rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.06f, 0.04f, 0.03f, 0.97f);

            // ── Title ────────────────────────────────────────────────
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(root.transform, false);
            RectTransform titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.85f);
            titleRt.anchorMax = new Vector2(0.95f, 0.97f);
            titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
            var titleTmp = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
            titleTmp.text = "USTAWIENIA";
            titleTmp.fontSize = 28;
            titleTmp.fontStyle = TMPro.FontStyles.Bold;
            titleTmp.alignment = TMPro.TextAlignmentOptions.Center;
            titleTmp.color = new Color(0.95f, 0.82f, 0.3f);

            // ── Volume label ─────────────────────────────────────────
            GameObject volLabelObj = new GameObject("VolumeLabel");
            volLabelObj.transform.SetParent(root.transform, false);
            RectTransform vlRt = volLabelObj.AddComponent<RectTransform>();
            vlRt.anchorMin = new Vector2(0.05f, 0.68f);
            vlRt.anchorMax = new Vector2(0.95f, 0.80f);
            vlRt.offsetMin = Vector2.zero; vlRt.offsetMax = Vector2.zero;
            var volTmp = volLabelObj.AddComponent<TMPro.TextMeshProUGUI>();
            volTmp.text = "Głośność: 100%";
            volTmp.fontSize = 20;
            volTmp.alignment = TMPro.TextAlignmentOptions.Left;
            volTmp.color = new Color(0.9f, 0.85f, 0.7f);

            // ── Volume slider ────────────────────────────────────────
            GameObject sliderObj = new GameObject("VolumeSlider");
            sliderObj.transform.SetParent(root.transform, false);
            RectTransform slRt = sliderObj.AddComponent<RectTransform>();
            slRt.anchorMin = new Vector2(0.05f, 0.58f);
            slRt.anchorMax = new Vector2(0.95f, 0.67f);
            slRt.offsetMin = Vector2.zero; slRt.offsetMax = Vector2.zero;

            // Slider background
            GameObject slBg = new GameObject("Background");
            slBg.transform.SetParent(sliderObj.transform, false);
            RectTransform slBgRt = slBg.AddComponent<RectTransform>();
            slBgRt.anchorMin = new Vector2(0f, 0.25f); slBgRt.anchorMax = new Vector2(1f, 0.75f);
            slBgRt.offsetMin = Vector2.zero; slBgRt.offsetMax = Vector2.zero;
            Image slBgImg = slBg.AddComponent<Image>();
            slBgImg.color = new Color(0.2f, 0.15f, 0.1f);

            // Slider fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.25f); faRt.anchorMax = new Vector2(1f, 0.75f);
            faRt.offsetMin = Vector2.zero; faRt.offsetMax = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.7f, 0.55f, 0.15f);

            // Slider handle area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform haRt = handleArea.AddComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(10f, 0f); haRt.offsetMax = new Vector2(-10f, 0f);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform hRt = handle.AddComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(20f, 0f);
            hRt.anchorMin = new Vector2(0f, 0f); hRt.anchorMax = new Vector2(0f, 1f);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.color = new Color(0.95f, 0.82f, 0.3f);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            // ── Fullscreen toggle ────────────────────────────────────
            GameObject fsObj = new GameObject("FullscreenToggle");
            fsObj.transform.SetParent(root.transform, false);
            RectTransform fsRt = fsObj.AddComponent<RectTransform>();
            fsRt.anchorMin = new Vector2(0.05f, 0.43f);
            fsRt.anchorMax = new Vector2(0.95f, 0.55f);
            fsRt.offsetMin = Vector2.zero; fsRt.offsetMax = Vector2.zero;

            // Toggle checkbox background
            GameObject checkBg = new GameObject("Background");
            checkBg.transform.SetParent(fsObj.transform, false);
            RectTransform cbRt = checkBg.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0f, 0.1f); cbRt.anchorMax = new Vector2(0f, 0.9f);
            cbRt.pivot = new Vector2(0f, 0.5f);
            cbRt.sizeDelta = new Vector2(28f, 0f);
            cbRt.anchoredPosition = Vector2.zero;
            Image cbImg = checkBg.AddComponent<Image>();
            cbImg.color = new Color(0.2f, 0.15f, 0.1f);

            // Toggle checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(checkBg.transform, false);
            RectTransform cmRt = checkmark.AddComponent<RectTransform>();
            cmRt.anchorMin = new Vector2(0.15f, 0.15f); cmRt.anchorMax = new Vector2(0.85f, 0.85f);
            cmRt.offsetMin = Vector2.zero; cmRt.offsetMax = Vector2.zero;
            Image cmImg = checkmark.AddComponent<Image>();
            cmImg.color = new Color(0.95f, 0.82f, 0.3f);

            Toggle toggle = fsObj.AddComponent<Toggle>();
            toggle.targetGraphic = cbImg;
            toggle.graphic = cmImg;
            toggle.isOn = Screen.fullScreen;

            // Toggle label
            GameObject fsLabel = new GameObject("Label");
            fsLabel.transform.SetParent(fsObj.transform, false);
            RectTransform flRt = fsLabel.AddComponent<RectTransform>();
            flRt.anchorMin = new Vector2(0f, 0f); flRt.anchorMax = new Vector2(1f, 1f);
            flRt.offsetMin = new Vector2(36f, 0f); flRt.offsetMax = Vector2.zero;
            var fsTmp = fsLabel.AddComponent<TMPro.TextMeshProUGUI>();
            fsTmp.text = "Pełny ekran";
            fsTmp.fontSize = 20;
            fsTmp.alignment = TMPro.TextAlignmentOptions.Left;
            fsTmp.color = new Color(0.9f, 0.85f, 0.7f);

            // ── Resolution row ───────────────────────────────────────
            // Label
            GameObject resLabel = new GameObject("ResolutionLabel");
            resLabel.transform.SetParent(root.transform, false);
            RectTransform rlRt = resLabel.AddComponent<RectTransform>();
            rlRt.anchorMin = new Vector2(0.05f, 0.28f);
            rlRt.anchorMax = new Vector2(0.30f, 0.40f);
            rlRt.offsetMin = Vector2.zero; rlRt.offsetMax = Vector2.zero;
            var resLabelTmp = resLabel.AddComponent<TMPro.TextMeshProUGUI>();
            resLabelTmp.text = "Rozdzielczość:";
            resLabelTmp.fontSize = 19;
            resLabelTmp.alignment = TMPro.TextAlignmentOptions.Left;
            resLabelTmp.color = new Color(0.9f, 0.85f, 0.7f);

            // Left arrow
            Color arrowCol = new Color(0.25f, 0.18f, 0.1f);
            GameObject resLeftBtn = new GameObject("ResLeftBtn");
            resLeftBtn.transform.SetParent(root.transform, false);
            RectTransform rlbRt = resLeftBtn.AddComponent<RectTransform>();
            rlbRt.anchorMin = new Vector2(0.32f, 0.29f);
            rlbRt.anchorMax = new Vector2(0.40f, 0.39f);
            rlbRt.offsetMin = Vector2.zero; rlbRt.offsetMax = Vector2.zero;
            Image rlbImg = resLeftBtn.AddComponent<Image>();
            rlbImg.color = arrowCol;
            Button rlbBtn = resLeftBtn.AddComponent<Button>();
            rlbBtn.targetGraphic = rlbImg;
            GameObject rlbText = new GameObject("Text");
            rlbText.transform.SetParent(resLeftBtn.transform, false);
            RectTransform rlbtRt = rlbText.AddComponent<RectTransform>();
            rlbtRt.anchorMin = Vector2.zero; rlbtRt.anchorMax = Vector2.one;
            rlbtRt.offsetMin = Vector2.zero; rlbtRt.offsetMax = Vector2.zero;
            var rlbTmp = rlbText.AddComponent<TMPro.TextMeshProUGUI>();
            rlbTmp.text = "<";
            rlbTmp.fontSize = 22;
            rlbTmp.fontStyle = TMPro.FontStyles.Bold;
            rlbTmp.alignment = TMPro.TextAlignmentOptions.Center;
            rlbTmp.color = new Color(0.95f, 0.82f, 0.3f);

            // Resolution value text
            GameObject resValueObj = new GameObject("ResolutionValue");
            resValueObj.transform.SetParent(root.transform, false);
            RectTransform rvRt = resValueObj.AddComponent<RectTransform>();
            rvRt.anchorMin = new Vector2(0.41f, 0.28f);
            rvRt.anchorMax = new Vector2(0.69f, 0.40f);
            rvRt.offsetMin = Vector2.zero; rvRt.offsetMax = Vector2.zero;
            var resValueTmp = resValueObj.AddComponent<TMPro.TextMeshProUGUI>();
            resValueTmp.text = "1920 x 1080";
            resValueTmp.fontSize = 20;
            resValueTmp.alignment = TMPro.TextAlignmentOptions.Center;
            resValueTmp.color = new Color(0.95f, 0.9f, 0.8f);

            // Right arrow
            GameObject resRightBtn = new GameObject("ResRightBtn");
            resRightBtn.transform.SetParent(root.transform, false);
            RectTransform rrbRt = resRightBtn.AddComponent<RectTransform>();
            rrbRt.anchorMin = new Vector2(0.70f, 0.29f);
            rrbRt.anchorMax = new Vector2(0.78f, 0.39f);
            rrbRt.offsetMin = Vector2.zero; rrbRt.offsetMax = Vector2.zero;
            Image rrbImg = resRightBtn.AddComponent<Image>();
            rrbImg.color = arrowCol;
            Button rrbBtn = resRightBtn.AddComponent<Button>();
            rrbBtn.targetGraphic = rrbImg;
            GameObject rrbText = new GameObject("Text");
            rrbText.transform.SetParent(resRightBtn.transform, false);
            RectTransform rrbtRt = rrbText.AddComponent<RectTransform>();
            rrbtRt.anchorMin = Vector2.zero; rrbtRt.anchorMax = Vector2.one;
            rrbtRt.offsetMin = Vector2.zero; rrbtRt.offsetMax = Vector2.zero;
            var rrbTmp = rrbText.AddComponent<TMPro.TextMeshProUGUI>();
            rrbTmp.text = ">";
            rrbTmp.fontSize = 22;
            rrbTmp.fontStyle = TMPro.FontStyles.Bold;
            rrbTmp.alignment = TMPro.TextAlignmentOptions.Center;
            rrbTmp.color = new Color(0.95f, 0.82f, 0.3f);

            // ── Back button ──────────────────────────────────────────
            GameObject backObj = new GameObject("BackButton");
            backObj.transform.SetParent(root.transform, false);
            RectTransform backRt = backObj.AddComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0.30f, 0.05f);
            backRt.anchorMax = new Vector2(0.70f, 0.18f);
            backRt.offsetMin = Vector2.zero; backRt.offsetMax = Vector2.zero;
            Image backBgImg = backObj.AddComponent<Image>();
            backBgImg.color = new Color(0.3f, 0.2f, 0.1f);
            Button backBtn = backObj.AddComponent<Button>();
            backBtn.targetGraphic = backBgImg;
            ColorBlock backCb = backBtn.colors;
            backCb.highlightedColor = new Color(0.5f, 0.35f, 0.15f);
            backCb.pressedColor = new Color(0.2f, 0.15f, 0.08f);
            backBtn.colors = backCb;
            GameObject backText = new GameObject("Text");
            backText.transform.SetParent(backObj.transform, false);
            RectTransform btRt = backText.AddComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero; btRt.anchorMax = Vector2.one;
            btRt.offsetMin = Vector2.zero; btRt.offsetMax = Vector2.zero;
            var backTmp = backText.AddComponent<TMPro.TextMeshProUGUI>();
            backTmp.text = "Wróć";
            backTmp.fontSize = 22;
            backTmp.fontStyle = TMPro.FontStyles.Bold;
            backTmp.alignment = TMPro.TextAlignmentOptions.Center;
            backTmp.color = new Color(0.95f, 0.82f, 0.3f);

            // ── Wire SettingsPanel controller ────────────────────────
            var ctrl = root.AddComponent<UI.SettingsPanel>();
            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("volumeSlider").objectReferenceValue     = slider;
            so.FindProperty("volumeLabel").objectReferenceValue      = volTmp;
            so.FindProperty("fullscreenToggle").objectReferenceValue = toggle;
            so.FindProperty("resolutionLeftBtn").objectReferenceValue  = rlbBtn;
            so.FindProperty("resolutionRightBtn").objectReferenceValue = rrbBtn;
            so.FindProperty("resolutionLabel").objectReferenceValue   = resValueTmp;
            so.FindProperty("backButton").objectReferenceValue        = backBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false); // starts hidden
            return (root, ctrl);
        }

        // =====================================================
        // SHARED: SAVE SLOT PANEL (reused in MainMenu + Dungeon)
        // =====================================================

        /// <summary>
        /// Builds the 3-slot save/load panel. Returns the root GameObject.
        /// The panel is hidden by default (SetActive false) and activated by SaveSlotPanel component.
        /// </summary>
        private static GameObject BuildSaveSlotPanel(Transform canvasParent)
        {
            GameObject root = new GameObject("SaveSlotPanel");
            root.transform.SetParent(canvasParent, false);
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = new Vector2(0.15f, 0.2f);
            rootRT.anchorMax = new Vector2(0.85f, 0.8f);
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;
            Image rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.05f, 0.04f, 0.03f, 0.98f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(root.transform, false);
            RectTransform titleRT = titleObj.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.05f, 0.82f);
            titleRT.anchorMax = new Vector2(0.95f, 0.97f);
            titleRT.offsetMin = Vector2.zero; titleRT.offsetMax = Vector2.zero;
            var titleTxt = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
            titleTxt.text = "ZAPISZ GRĘ";
            titleTxt.fontSize = 22; titleTxt.fontStyle = TMPro.FontStyles.Bold;
            titleTxt.alignment = TMPro.TextAlignmentOptions.Center;
            titleTxt.color = new Color(0.9f, 0.75f, 0.2f);

            // Three slot buttons
            var (s1btn, s1lbl) = CreateSlotButton(root.transform, "Slot1Button", "SLOT 1\nPusty slot",
                new Vector2(0.03f, 0.42f), new Vector2(0.97f, 0.76f));
            var (s2btn, s2lbl) = CreateSlotButton(root.transform, "Slot2Button", "SLOT 2\nPusty slot",
                new Vector2(0.03f, 0.20f), new Vector2(0.97f, 0.40f));
            var (s3btn, s3lbl) = CreateSlotButton(root.transform, "Slot3Button", "SLOT 3\nPusty slot",
                new Vector2(0.03f, -0.02f), new Vector2(0.97f, 0.18f));

            // Cancel button
            GameObject cancelObj = new GameObject("CancelButton");
            cancelObj.transform.SetParent(root.transform, false);
            RectTransform cancelRT = cancelObj.AddComponent<RectTransform>();
            cancelRT.anchorMin = new Vector2(0.25f, 0.02f);
            cancelRT.anchorMax = new Vector2(0.75f, 0.14f);
            cancelRT.offsetMin = Vector2.zero; cancelRT.offsetMax = Vector2.zero;
            Image cancelBg = cancelObj.AddComponent<Image>();
            cancelBg.color = new Color(0.35f, 0.12f, 0.08f, 0.9f);
            Button cancelBtn = cancelObj.AddComponent<Button>();
            cancelBtn.targetGraphic = cancelBg;
            GameObject cancelTxtObj = new GameObject("Label");
            cancelTxtObj.transform.SetParent(cancelObj.transform, false);
            RectTransform cancelTxtRT = cancelTxtObj.AddComponent<RectTransform>();
            cancelTxtRT.anchorMin = Vector2.zero; cancelTxtRT.anchorMax = Vector2.one;
            cancelTxtRT.offsetMin = Vector2.zero; cancelTxtRT.offsetMax = Vector2.zero;
            var cancelTxt = cancelTxtObj.AddComponent<TMPro.TextMeshProUGUI>();
            cancelTxt.text = "ANULUJ"; cancelTxt.fontSize = 23;
            cancelTxt.alignment = TMPro.TextAlignmentOptions.Center;
            cancelTxt.color = Color.white;

            // Wire SaveSlotPanel component
            UI.SaveSlotPanel ssp = root.AddComponent<UI.SaveSlotPanel>();
            SerializedObject sspSo = new SerializedObject(ssp);
            sspSo.FindProperty("slot1Button").objectReferenceValue = s1btn;
            sspSo.FindProperty("slot2Button").objectReferenceValue = s2btn;
            sspSo.FindProperty("slot3Button").objectReferenceValue = s3btn;
            sspSo.FindProperty("slot1Label").objectReferenceValue  = s1lbl;
            sspSo.FindProperty("slot2Label").objectReferenceValue  = s2lbl;
            sspSo.FindProperty("slot3Label").objectReferenceValue  = s3lbl;
            sspSo.FindProperty("titleText").objectReferenceValue   = titleTxt;
            sspSo.FindProperty("cancelButton").objectReferenceValue = cancelBtn;
            sspSo.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return root;
        }

        private static (Button btn, TMPro.TextMeshProUGUI label) CreateSlotButton(
            Transform parent, string name, string labelText,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(4f, 4f); rt.offsetMax = new Vector2(-4f, -4f);
            Image bg = obj.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.08f, 0.05f, 0.95f);
            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.8f, 0.6f, 0.1f, 1f);
            cb.pressedColor     = new Color(0.6f, 0.45f, 0.1f, 1f);
            btn.colors = cb;

            GameObject lblObj = new GameObject("Label");
            lblObj.transform.SetParent(obj.transform, false);
            RectTransform lblRT = lblObj.AddComponent<RectTransform>();
            lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
            lblRT.offsetMin = new Vector2(8f, 4f); lblRT.offsetMax = new Vector2(-8f, -4f);
            var lbl = lblObj.AddComponent<TMPro.TextMeshProUGUI>();
            lbl.text = labelText; lbl.fontSize = 20;
            lbl.alignment = TMPro.TextAlignmentOptions.Left;
            lbl.color = new Color(0.85f, 0.8f, 0.7f);

            return (btn, lbl);
        }

        // =====================================================
        // BUILD SETTINGS
        // =====================================================

        private static void UpdateBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainMenuScenePath,   true),
                new EditorBuildSettingsScene(CharSelectScenePath, true),
                new EditorBuildSettingsScene(DungeonScenePath,    true),
                new EditorBuildSettingsScene(CombatScenePath,     true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Build settings updated with 4 scenes.");
        }

        // =====================================================
        // CHARACTER SELECT SCENE
        // =====================================================

        private static void BuildCharSelectScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Camera ---
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.02f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camObj.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();

            // --- Event System ---
            GameObject evtSys = new GameObject("EventSystem");
            evtSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
            evtSys.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // --- Canvas ---
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // --- Root panel (dark background) ---
            GameObject root = new GameObject("Root");
            root.transform.SetParent(canvasObj.transform, false);
            RectTransform rootRT = root.AddComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;
            Image rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.06f, 0.04f, 0.03f, 1f);

            // --- Title ---
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(root.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.1f, 0.85f);
            titleRect.anchorMax = new Vector2(0.9f, 0.97f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            var titleTxt = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
            titleTxt.text = "WYBIERZ KLASĘ POSTACI";
            titleTxt.fontSize = 36;
            titleTxt.fontStyle = TMPro.FontStyles.Bold;
            titleTxt.alignment = TMPro.TextAlignmentOptions.Center;
            titleTxt.color = new Color(0.9f, 0.75f, 0.2f);

            // Subtitle
            GameObject subtitleObj = new GameObject("Subtitle");
            subtitleObj.transform.SetParent(root.transform, false);
            RectTransform subRect = subtitleObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.1f, 0.79f);
            subRect.anchorMax = new Vector2(0.9f, 0.87f);
            subRect.offsetMin = Vector2.zero;
            subRect.offsetMax = Vector2.zero;
            var subTxt = subtitleObj.AddComponent<TMPro.TextMeshProUGUI>();
            subTxt.text = "Każda klasa to inny styl walki. Wybierz mądrze.";
            subTxt.fontSize = 23;
            subTxt.alignment = TMPro.TextAlignmentOptions.Center;
            subTxt.color = new Color(0.65f, 0.55f, 0.45f);

            // ============================
            // THREE CLASS CARDS (left side, ~45% width)
            // ============================
            GameObject cardsPanel = new GameObject("CardsPanel");
            cardsPanel.transform.SetParent(root.transform, false);
            RectTransform cardsRect = cardsPanel.AddComponent<RectTransform>();
            cardsRect.anchorMin = new Vector2(0.02f, 0.1f);
            cardsRect.anchorMax = new Vector2(0.45f, 0.78f);
            cardsRect.offsetMin = Vector2.zero;
            cardsRect.offsetMax = Vector2.zero;

            Button wojownikBtn = CreateClassCard(cardsPanel.transform, "WojownikCard",
                "WOJOWNIK",
                "HP: 120  Atak: 14  Obr: 8\nWigor: 3 PA",
                new Vector2(0f, 0.68f), new Vector2(1f, 1f));

            Button lucznikBtn = CreateClassCard(cardsPanel.transform, "LucznikCard",
                "ŁUCZNIK",
                "HP: 85   Atak: 11  Obr: 4\nWigor: 4 PA",
                new Vector2(0f, 0.34f), new Vector2(1f, 0.66f));

            Button magBtn = CreateClassCard(cardsPanel.transform, "MagCard",
                "MAG",
                "HP: 70   Atak: 7   Obr: 3\nWigor: 3 PA",
                new Vector2(0f, 0f), new Vector2(1f, 0.32f));

            // ============================
            // PREVIEW PANEL (right side, ~52% width)
            // ============================
            GameObject previewPanel = new GameObject("PreviewPanel");
            previewPanel.transform.SetParent(root.transform, false);
            RectTransform previewRect = previewPanel.AddComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.47f, 0.1f);
            previewRect.anchorMax = new Vector2(0.99f, 0.78f);
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;
            Image previewBg = previewPanel.AddComponent<Image>();
            previewBg.color = new Color(0.09f, 0.07f, 0.05f, 0.95f);

            // Sprite display (large, top portion)
            GameObject spriteContainer = new GameObject("SpriteContainer");
            spriteContainer.transform.SetParent(previewPanel.transform, false);
            RectTransform sprContRect = spriteContainer.AddComponent<RectTransform>();
            sprContRect.anchorMin = new Vector2(0.1f, 0.55f);
            sprContRect.anchorMax = new Vector2(0.9f, 0.97f);
            sprContRect.offsetMin = Vector2.zero;
            sprContRect.offsetMax = Vector2.zero;
            Image previewImg = spriteContainer.AddComponent<Image>();
            previewImg.color = Color.white;
            previewImg.preserveAspect = true;

            // Class name
            GameObject nameObj = new GameObject("ClassName");
            nameObj.transform.SetParent(previewPanel.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.05f, 0.42f);
            nameRect.anchorMax = new Vector2(0.95f, 0.56f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;
            var nameTxt = nameObj.AddComponent<TMPro.TextMeshProUGUI>();
            nameTxt.text = "GNIEWKO\nWOJOWNIK";
            nameTxt.fontSize = 23;
            nameTxt.fontStyle = TMPro.FontStyles.Bold;
            nameTxt.alignment = TMPro.TextAlignmentOptions.Center;
            nameTxt.color = new Color(0.9f, 0.75f, 0.2f);

            // Stats
            GameObject statsObj = new GameObject("StatsText");
            statsObj.transform.SetParent(previewPanel.transform, false);
            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.05f, 0.22f);
            statsRect.anchorMax = new Vector2(0.95f, 0.43f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
            var statsTxt = statsObj.AddComponent<TMPro.TextMeshProUGUI>();
            statsTxt.text = "Zdrowie   120\nAtak      14\nObrona    8\nWigor     3 PA";
            statsTxt.fontSize = 20;
            statsTxt.alignment = TMPro.TextAlignmentOptions.Left;
            statsTxt.color = new Color(0.8f, 0.75f, 0.65f);

            // Description
            GameObject descObj = new GameObject("Description");
            descObj.transform.SetParent(previewPanel.transform, false);
            RectTransform descRect = descObj.AddComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.05f, 0.02f);
            descRect.anchorMax = new Vector2(0.95f, 0.23f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;
            var descTxt = descObj.AddComponent<TMPro.TextMeshProUGUI>();
            descTxt.text = "Specjal: Potezne Uderzenie\n2x obrazenia fizyczne!";
            descTxt.fontSize = 23;
            descTxt.alignment = TMPro.TextAlignmentOptions.Left;
            descTxt.color = new Color(0.5f, 0.8f, 0.5f);

            // ============================
            // BOTTOM BUTTONS
            // ============================
            Button backBtn    = CreateNavButton(root.transform, "BackButton",    "← POWRÓT",
                new Vector2(0.02f, 0.01f), new Vector2(0.25f, 0.09f),
                new Color(0.35f, 0.15f, 0.08f, 0.9f));

            Button confirmBtn = CreateNavButton(root.transform, "ConfirmButton", "ROZPOCZNIJ PRZYGODĘ →",
                new Vector2(0.45f, 0.01f), new Vector2(0.99f, 0.09f),
                new Color(0.15f, 0.4f, 0.15f, 0.9f));

            // ============================
            // CONTROLLER
            // ============================
            GameObject ctrlObj = new GameObject("CharSelectController");
            ctrlObj.transform.SetParent(canvasObj.transform, false);
            UI.CharSelectController ctrl = ctrlObj.AddComponent<UI.CharSelectController>();

            Texture2D roguesTex = AssetDatabase.LoadAssetAtPath<Texture2D>(RogueSheetPath);

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("wojownikButton").objectReferenceValue  = wojownikBtn;
            so.FindProperty("lucznikButton").objectReferenceValue   = lucznikBtn;
            so.FindProperty("magButton").objectReferenceValue       = magBtn;

            // Card labels (TMP inside each card)
            var wojLabel = wojownikBtn.transform.Find("Label")?.GetComponent<TMPro.TextMeshProUGUI>();
            var lucLabel = lucznikBtn.transform.Find("Label")?.GetComponent<TMPro.TextMeshProUGUI>();
            var magLabel = magBtn.transform.Find("Label")?.GetComponent<TMPro.TextMeshProUGUI>();
            so.FindProperty("wojownikLabel").objectReferenceValue   = wojLabel;
            so.FindProperty("lucznikLabel").objectReferenceValue    = lucLabel;
            so.FindProperty("magLabel").objectReferenceValue        = magLabel;

            so.FindProperty("previewSprite").objectReferenceValue   = previewImg;
            so.FindProperty("previewName").objectReferenceValue     = nameTxt;
            so.FindProperty("previewStats").objectReferenceValue    = statsTxt;
            so.FindProperty("previewDescription").objectReferenceValue = descTxt;
            so.FindProperty("confirmButton").objectReferenceValue   = confirmBtn;
            so.FindProperty("backButton").objectReferenceValue      = backBtn;
            so.FindProperty("rogueSheet").objectReferenceValue      = roguesTex;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, CharSelectScenePath);
            Debug.Log("CharSelect scene created at: " + CharSelectScenePath);
        }

        /// <summary>Creates a class selection card button with name + stats text.</summary>
        private static Button CreateClassCard(Transform parent, string name,
            string className, string statsLine,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject cardObj = new GameObject(name);
            cardObj.transform.SetParent(parent, false);
            RectTransform rect = cardObj.AddComponent<RectTransform>();
            rect.anchorMin  = anchorMin;
            rect.anchorMax  = anchorMax;
            rect.offsetMin  = new Vector2(4f, 4f);
            rect.offsetMax  = new Vector2(-4f, -4f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = new Color(0.07f, 0.05f, 0.04f, 0.95f);

            Button btn = cardObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = new Color(0.07f, 0.05f, 0.04f, 0.95f);
            cb.highlightedColor = new Color(0.2f, 0.15f, 0.05f, 1f);
            cb.pressedColor     = new Color(0.8f, 0.6f, 0.1f, 1f);
            cb.selectedColor    = new Color(0.8f, 0.6f, 0.1f, 0.9f);
            btn.colors = cb;
            btn.targetGraphic = cardBg;

            // Class name label (large, top)
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(cardObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.05f, 0.5f);
            labelRect.anchorMax = new Vector2(0.95f, 0.95f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelTxt = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
            labelTxt.text       = className;
            labelTxt.fontSize   = 23;
            labelTxt.fontStyle  = TMPro.FontStyles.Bold;
            labelTxt.alignment  = TMPro.TextAlignmentOptions.Center;
            labelTxt.color      = new Color(0.9f, 0.75f, 0.2f);

            // Stats (small, bottom)
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(cardObj.transform, false);
            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.05f, 0.05f);
            statsRect.anchorMax = new Vector2(0.95f, 0.5f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;
            var statsTxt = statsObj.AddComponent<TMPro.TextMeshProUGUI>();
            statsTxt.text       = statsLine;
            statsTxt.fontSize   = 16;
            statsTxt.alignment  = TMPro.TextAlignmentOptions.Center;
            statsTxt.color      = new Color(0.7f, 0.65f, 0.6f);

            return btn;
        }

        // =====================================================
        // PATCH INVENTORY UI — standalone menu item
        // Adds inventory panel to EXISTING Dungeon + Combat scenes
        // without rebuilding everything from scratch.
        // =====================================================

        [MenuItem("Tools/Lochy i Gorzala/Patch Inventory UI (dodaj do istniejacych scen)", false, 10)]
        public static void PatchInventoryUI()
        {
            if (!EditorUtility.DisplayDialog(
                "Patch Inventory UI",
                "Doda panel Plecaka do istniejących scen Dungeon i Combat.\nSceny zostaną zapisane.\n\nKontynuować?",
                "Tak", "Anuluj")) return;

            Texture2D itemsTex = AssetDatabase.LoadAssetAtPath<Texture2D>(ItemsSheetPath);
            if (itemsTex == null)
            {
                EditorUtility.DisplayDialog("Błąd",
                    $"Nie znaleziono items.png w:\n{ItemsSheetPath}", "OK");
                return;
            }

            PatchScene(DungeonScenePath, itemsTex, inCombat: false);
            PatchScene(CombatScenePath,  itemsTex, inCombat: true);

            EditorUtility.DisplayDialog("Gotowe!",
                "Panel Plecaka dodany do scen Dungeon i Combat.", "OK");
        }

        private static void PatchScene(string scenePath, Texture2D itemsTex, bool inCombat)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning($"Scena nie istnieje: {scenePath}. Uruchom najpierw 'Setup Project'.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Remove any existing InventoryPanel to avoid duplicates
            foreach (var go in scene.GetRootGameObjects())
            {
                var existing = go.GetComponentInChildren<UI.InventoryUIController>(true);
                if (existing != null)
                {
                    Object.DestroyImmediate(existing.gameObject);
                    break;
                }
            }

            // Find the canvas
            Canvas canvas = null;
            string canvasName = inCombat ? "CombatCanvas" : "HUDCanvas";
            foreach (var go in scene.GetRootGameObjects())
            {
                canvas = go.GetComponentInChildren<Canvas>(true);
                if (canvas != null && go.name == canvasName) break;
                // Also search children
                var found = go.transform.Find(canvasName);
                if (found != null) { canvas = found.GetComponent<Canvas>(); break; }
                canvas = null;
            }
            // Fallback: first canvas
            if (canvas == null)
                foreach (var go in scene.GetRootGameObjects())
                {
                    canvas = go.GetComponent<Canvas>() ??
                             go.GetComponentInChildren<Canvas>(true);
                    if (canvas != null) break;
                }

            if (canvas == null)
            {
                Debug.LogError($"Nie znaleziono Canvas w scenie {scenePath}");
                return;
            }

            BuildInventoryPanel(canvas.transform, itemsTex, inCombat);
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"Inventory UI dodany do: {scenePath}");
        }

        // =====================================================
        // BUILD INVENTORY PANEL — shared builder
        // Returns (root GO, InventoryUIController component)
        // =====================================================

        // =====================================================
        // BUILD SHOP PANEL — Sklep Mirka Handlarza
        // =====================================================

        private static (GameObject panel, UI.ShopUIController ctrl)
            BuildShopPanel(Transform canvasTransform, Texture2D itemsTex)
        {
            // ── Root (full screen overlay) ────────────────────────
            GameObject root = new GameObject("ShopPanel");
            root.transform.SetParent(canvasTransform, false);
            RectTransform rootRT = root.AddComponent<RectTransform>();
            SetFullAnchor(rootRT);
            Image rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.04f, 0.03f, 0.02f, 0.97f);

            // ── Header ────────────────────────────────────────────
            GameObject hdrGO = new GameObject("Header");
            hdrGO.transform.SetParent(root.transform, false);
            RectTransform hdrRT = hdrGO.AddComponent<RectTransform>();
            hdrRT.anchorMin = new Vector2(0f, 0.92f); hdrRT.anchorMax = Vector2.one;
            hdrRT.offsetMin = Vector2.zero; hdrRT.offsetMax = Vector2.zero;
            hdrGO.AddComponent<Image>().color = new Color(0.1f, 0.07f, 0.03f);

            GameObject shopTitleGO = new GameObject("ShopTitle");
            shopTitleGO.transform.SetParent(hdrGO.transform, false);
            RectTransform stRT = shopTitleGO.AddComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0.03f, 0f); stRT.anchorMax = new Vector2(0.85f, 1f);
            stRT.offsetMin = Vector2.zero; stRT.offsetMax = Vector2.zero;
            var shopTitleTmp = shopTitleGO.AddComponent<TMPro.TextMeshProUGUI>();
            shopTitleTmp.text = "SKLEP MIRKA HANDLARZA";
            shopTitleTmp.fontSize = 22;
            shopTitleTmp.fontStyle = TMPro.FontStyles.Bold;
            shopTitleTmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            shopTitleTmp.color = new Color(0.95f, 0.75f, 0.2f);

            // Close button
            GameObject closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(hdrGO.transform, false);
            RectTransform cRT = closeGO.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0.88f, 0.1f); cRT.anchorMax = new Vector2(0.99f, 0.9f);
            cRT.offsetMin = Vector2.zero; cRT.offsetMax = Vector2.zero;
            Image closeBg = closeGO.AddComponent<Image>();
            closeBg.color = new Color(0.4f, 0.1f, 0.08f);
            Button closeBtn = closeGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            GameObject closeLblGO = new GameObject("Label");
            closeLblGO.transform.SetParent(closeGO.transform, false);
            var clRT = closeLblGO.AddComponent<RectTransform>(); SetFullAnchor(clRT);
            var clTmp = closeLblGO.AddComponent<TMPro.TextMeshProUGUI>();
            clTmp.text = "X"; clTmp.fontSize = 23;
            clTmp.alignment = TMPro.TextAlignmentOptions.Center;
            clTmp.color = Color.white;

            // ── Gold display (top bar right of title) ─────────────
            GameObject goldGO = new GameObject("GoldText");
            goldGO.transform.SetParent(root.transform, false);
            RectTransform goldRT = goldGO.AddComponent<RectTransform>();
            goldRT.anchorMin = new Vector2(0.01f, 0.915f); goldRT.anchorMax = new Vector2(0.55f, 0.945f);
            goldRT.offsetMin = Vector2.zero; goldRT.offsetMax = Vector2.zero;
            var goldTmp = goldGO.AddComponent<TMPro.TextMeshProUGUI>();
            goldTmp.text = "Twoje zloto: 0";
            goldTmp.fontSize = 23;
            goldTmp.color = new Color(0.95f, 0.8f, 0.2f);

            // ── Buy label ─────────────────────────────────────────
            var buyLblGO = CreateShopSectionLabel(root.transform, "BuyLabel",
                "KUPUJ od Mirka:", new Vector2(0.01f, 0.84f), new Vector2(0.55f, 0.90f));

            // ── Stock grid (buy side — left 57%) ──────────────────
            GameObject stockGrid = new GameObject("StockGrid");
            stockGrid.transform.SetParent(root.transform, false);
            RectTransform sgRT = stockGrid.AddComponent<RectTransform>();
            sgRT.anchorMin = new Vector2(0.01f, 0.40f); sgRT.anchorMax = new Vector2(0.57f, 0.84f);
            sgRT.offsetMin = new Vector2(4f, 4f); sgRT.offsetMax = new Vector2(-4f, -4f);
            stockGrid.AddComponent<Image>().color = new Color(0.07f, 0.06f, 0.04f, 0.9f);
            var sgGLG = stockGrid.AddComponent<GridLayoutGroup>();
            sgGLG.cellSize = new Vector2(60f, 60f);
            sgGLG.spacing  = new Vector2(6f, 6f);
            sgGLG.padding  = new RectOffset(6, 6, 6, 6);
            sgGLG.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            sgGLG.constraintCount = 5;
            sgGLG.childAlignment = TextAnchor.UpperLeft;

            // ── Sell label ────────────────────────────────────────
            var sellLblGO = CreateShopSectionLabel(root.transform, "SellLabel",
                "SPRZEDAJ swoje przedmioty:", new Vector2(0.01f, 0.34f), new Vector2(0.57f, 0.40f));

            // ── Sell grid (player inventory — left 57%) ───────────
            GameObject sellGrid = new GameObject("SellGrid");
            sellGrid.transform.SetParent(root.transform, false);
            RectTransform sellGRT = sellGrid.AddComponent<RectTransform>();
            sellGRT.anchorMin = new Vector2(0.01f, 0.02f); sellGRT.anchorMax = new Vector2(0.57f, 0.34f);
            sellGRT.offsetMin = new Vector2(4f, 4f); sellGRT.offsetMax = new Vector2(-4f, -4f);
            sellGrid.AddComponent<Image>().color = new Color(0.06f, 0.07f, 0.04f, 0.9f);
            var sgGLG2 = sellGrid.AddComponent<GridLayoutGroup>();
            sgGLG2.cellSize = new Vector2(60f, 60f);
            sgGLG2.spacing  = new Vector2(6f, 6f);
            sgGLG2.padding  = new RectOffset(6, 6, 6, 6);
            sgGLG2.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            sgGLG2.constraintCount = 5;
            sgGLG2.childAlignment = TextAnchor.UpperLeft;

            // ── Detail panel (right 40%) ──────────────────────────
            GameObject detailGO = new GameObject("ShopDetailPanel");
            detailGO.transform.SetParent(root.transform, false);
            RectTransform detRT = detailGO.AddComponent<RectTransform>();
            detRT.anchorMin = new Vector2(0.60f, 0.02f); detRT.anchorMax = new Vector2(0.99f, 0.91f);
            detRT.offsetMin = Vector2.zero; detRT.offsetMax = Vector2.zero;
            detailGO.AddComponent<Image>().color = new Color(0.08f, 0.07f, 0.05f, 0.92f);

            // Detail name
            GameObject dNameGO = new GameObject("DetailName");
            dNameGO.transform.SetParent(detailGO.transform, false);
            RectTransform dnRT = dNameGO.AddComponent<RectTransform>();
            dnRT.anchorMin = new Vector2(0.04f, 0.82f); dnRT.anchorMax = new Vector2(0.96f, 0.98f);
            dnRT.offsetMin = Vector2.zero; dnRT.offsetMax = Vector2.zero;
            var dNameTmp = dNameGO.AddComponent<TMPro.TextMeshProUGUI>();
            dNameTmp.text = "Wybierz przedmiot..."; dNameTmp.fontSize = 23;
            dNameTmp.fontStyle = TMPro.FontStyles.Bold;
            dNameTmp.alignment = TMPro.TextAlignmentOptions.TopLeft;
            dNameTmp.color = new Color(0.9f, 0.85f, 0.65f);
            dNameTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Detail desc
            GameObject dDescGO = new GameObject("DetailDesc");
            dDescGO.transform.SetParent(detailGO.transform, false);
            RectTransform ddRT = dDescGO.AddComponent<RectTransform>();
            ddRT.anchorMin = new Vector2(0.04f, 0.60f); ddRT.anchorMax = new Vector2(0.96f, 0.82f);
            ddRT.offsetMin = Vector2.zero; ddRT.offsetMax = Vector2.zero;
            var dDescTmp = dDescGO.AddComponent<TMPro.TextMeshProUGUI>();
            dDescTmp.text = ""; dDescTmp.fontSize = 23;
            dDescTmp.color = new Color(0.75f, 0.7f, 0.6f);
            dDescTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            dDescTmp.alignment = TMPro.TextAlignmentOptions.TopLeft;

            // Detail price
            GameObject dPriceGO = new GameObject("DetailPrice");
            dPriceGO.transform.SetParent(detailGO.transform, false);
            RectTransform dpRT = dPriceGO.AddComponent<RectTransform>();
            dpRT.anchorMin = new Vector2(0.04f, 0.44f); dpRT.anchorMax = new Vector2(0.96f, 0.60f);
            dpRT.offsetMin = Vector2.zero; dpRT.offsetMax = Vector2.zero;
            var dPriceTmp = dPriceGO.AddComponent<TMPro.TextMeshProUGUI>();
            dPriceTmp.text = ""; dPriceTmp.fontSize = 20;
            dPriceTmp.color = new Color(0.95f, 0.8f, 0.2f);
            dPriceTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Buy button
            Button buyBtn = CreateDetailButton(detailGO.transform, "BuyButton",
                "Kup", new Color(0.1f, 0.35f, 0.1f),
                new Vector2(0.04f, 0.26f), new Vector2(0.96f, 0.42f));

            // Sell button
            Button sellBtn = CreateDetailButton(detailGO.transform, "SellButton",
                "Sprzedaj", new Color(0.35f, 0.2f, 0.05f),
                new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.25f));

            // ── ShopUIController ──────────────────────────────────
            GameObject ctrlGO = new GameObject("ShopUIController");
            ctrlGO.transform.SetParent(root.transform, false);
            var shopCtrl = ctrlGO.AddComponent<UI.ShopUIController>();

            SerializedObject so = new SerializedObject(shopCtrl);
            so.FindProperty("shopPanel").objectReferenceValue       = root;
            so.FindProperty("closeButton").objectReferenceValue     = closeBtn;
            so.FindProperty("goldText").objectReferenceValue        = goldTmp;
            so.FindProperty("stockGridParent").objectReferenceValue = stockGrid.transform;
            so.FindProperty("sellGridParent").objectReferenceValue  = sellGrid.transform;
            so.FindProperty("detailPanel").objectReferenceValue     = detailGO;
            so.FindProperty("detailName").objectReferenceValue      = dNameTmp;
            so.FindProperty("detailDesc").objectReferenceValue      = dDescTmp;
            so.FindProperty("detailPrice").objectReferenceValue     = dPriceTmp;
            so.FindProperty("buyButton").objectReferenceValue       = buyBtn;
            so.FindProperty("sellButton").objectReferenceValue      = sellBtn;
            so.FindProperty("itemsSheet").objectReferenceValue      = itemsTex;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            return (root, shopCtrl);
        }

        private static GameObject CreateShopSectionLabel(Transform parent, string name,
            string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(4f, 0f); rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 23;
            tmp.alignment = TMPro.TextAlignmentOptions.Left;
            tmp.color = new Color(0.6f, 0.55f, 0.4f);
            return go;
        }

        // Returns (root GO, InventoryUIController component)
        // =====================================================

        private static (GameObject panel, UI.InventoryUIController ctrl)
            BuildInventoryPanel(Transform canvasTransform, Texture2D itemsTex, bool inCombat)
        {
            // ── Root overlay (full screen, draws on top of everything) ─────────
            GameObject root = new GameObject("InventoryPanel");
            root.transform.SetParent(canvasTransform, false);
            RectTransform rootRT = root.AddComponent<RectTransform>();
            SetFullAnchor(rootRT);
            Image rootBg = root.AddComponent<Image>();
            rootBg.color = new Color(0.05f, 0.04f, 0.03f, 0.95f);

            // ── HEADER ────────────────────────────────────────────────────────
            GameObject header = new GameObject("Header");
            header.transform.SetParent(root.transform, false);
            RectTransform headerRT = header.AddComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0f, 0.92f);
            headerRT.anchorMax = Vector2.one;
            headerRT.offsetMin = Vector2.zero; headerRT.offsetMax = Vector2.zero;
            Image headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.1f, 0.08f, 0.05f, 1f);

            // Title
            GameObject titleGO = new GameObject("Title");
            titleGO.transform.SetParent(header.transform, false);
            RectTransform titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.05f, 0f);
            titleRT.anchorMax = new Vector2(0.85f, 1f);
            titleRT.offsetMin = Vector2.zero; titleRT.offsetMax = Vector2.zero;
            var titleTmp = titleGO.AddComponent<TMPro.TextMeshProUGUI>();
            titleTmp.text      = "== PLECAK ==";
            titleTmp.fontSize  = 24;
            titleTmp.fontStyle = TMPro.FontStyles.Bold;
            titleTmp.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            titleTmp.color     = new Color(0.9f, 0.75f, 0.2f);

            // Close button (top-right X)
            GameObject closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(header.transform, false);
            RectTransform closeRT = closeGO.AddComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(0.88f, 0.1f);
            closeRT.anchorMax = new Vector2(0.99f, 0.9f);
            closeRT.offsetMin = Vector2.zero; closeRT.offsetMax = Vector2.zero;
            Image closeBg = closeGO.AddComponent<Image>();
            closeBg.color = new Color(0.4f, 0.1f, 0.08f, 0.9f);
            Button closeBtn = closeGO.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            GameObject closeLbl = new GameObject("Label");
            closeLbl.transform.SetParent(closeGO.transform, false);
            var closeLblRT = closeLbl.AddComponent<RectTransform>();
            SetFullAnchor(closeLblRT);
            var closeTmp = closeLbl.AddComponent<TMPro.TextMeshProUGUI>();
            closeTmp.text = "X"; closeTmp.fontSize = 22;
            closeTmp.alignment = TMPro.TextAlignmentOptions.Center;
            closeTmp.color = Color.white;

            // ── LEFT PANEL (60 %) — equip slots + bag grid ───────────────────
            GameObject leftPanel = new GameObject("LeftPanel");
            leftPanel.transform.SetParent(root.transform, false);
            RectTransform leftRT = leftPanel.AddComponent<RectTransform>();
            leftRT.anchorMin = new Vector2(0.01f, 0.02f);
            leftRT.anchorMax = new Vector2(0.59f, 0.91f);
            leftRT.offsetMin = Vector2.zero; leftRT.offsetMax = Vector2.zero;

            // ── Equip section (top 22 % of left panel) ──────────────────────
            GameObject equipSection = new GameObject("EquipSection");
            equipSection.transform.SetParent(leftPanel.transform, false);
            RectTransform equipRT = equipSection.AddComponent<RectTransform>();
            equipRT.anchorMin = new Vector2(0f, 0.78f);
            equipRT.anchorMax = Vector2.one;
            equipRT.offsetMin = Vector2.zero; equipRT.offsetMax = Vector2.zero;
            Image equipBg = equipSection.AddComponent<Image>();
            equipBg.color = new Color(0.08f, 0.07f, 0.04f, 0.9f);

            // Three equip slot containers (weapon / armor / accessory)
            (Image weaponIcon, TMPro.TextMeshProUGUI weaponLabel) =
                CreateEquipSlot(equipSection.transform, "WeaponSlot",  "Broń",
                    new Vector2(0.02f, 0.05f), new Vector2(0.32f, 0.95f));

            (Image armorIcon, TMPro.TextMeshProUGUI armorLabel) =
                CreateEquipSlot(equipSection.transform, "ArmorSlot",   "Zbroja",
                    new Vector2(0.35f, 0.05f), new Vector2(0.65f, 0.95f));

            (Image accessoryIcon, TMPro.TextMeshProUGUI accessoryLabel) =
                CreateEquipSlot(equipSection.transform, "AccessorySlot", "Akcesorium",
                    new Vector2(0.68f, 0.05f), new Vector2(0.98f, 0.95f));

            // ── Bag Grid (bottom 76 % of left panel) — GridLayoutGroup ───────
            GameObject bagGrid = new GameObject("BagGrid");
            bagGrid.transform.SetParent(leftPanel.transform, false);
            RectTransform bagGridRT = bagGrid.AddComponent<RectTransform>();
            bagGridRT.anchorMin = new Vector2(0f, 0f);
            bagGridRT.anchorMax = new Vector2(1f, 0.77f);
            bagGridRT.offsetMin = new Vector2(4f, 4f);
            bagGridRT.offsetMax = new Vector2(-4f, -4f);
            Image bagGridBg = bagGrid.AddComponent<Image>();
            bagGridBg.color = new Color(0.07f, 0.06f, 0.04f, 0.85f);

            var glg = bagGrid.AddComponent<GridLayoutGroup>();
            glg.cellSize        = new Vector2(56f, 56f);
            glg.spacing         = new Vector2(6f, 6f);
            glg.padding         = new RectOffset(8, 8, 8, 8);
            glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;
            glg.childAlignment  = TextAnchor.UpperLeft;

            // Bag label
            GameObject bagLbl = new GameObject("BagLabel");
            bagLbl.transform.SetParent(leftPanel.transform, false);
            RectTransform bagLblRT = bagLbl.AddComponent<RectTransform>();
            bagLblRT.anchorMin = new Vector2(0f, 0.73f);
            bagLblRT.anchorMax = new Vector2(1f, 0.78f);
            bagLblRT.offsetMin = Vector2.zero; bagLblRT.offsetMax = Vector2.zero;
            var bagLblTmp = bagLbl.AddComponent<TMPro.TextMeshProUGUI>();
            bagLblTmp.text = "PLECAK  (0/20)";
            bagLblTmp.fontSize = 23;
            bagLblTmp.color = new Color(0.6f, 0.55f, 0.4f);
            bagLblTmp.alignment = TMPro.TextAlignmentOptions.Left;

            // ── RIGHT PANEL (38 %) — detail + buttons ────────────────────────
            GameObject rightPanel = new GameObject("DetailPanel");
            rightPanel.transform.SetParent(root.transform, false);
            RectTransform rightRT = rightPanel.AddComponent<RectTransform>();
            rightRT.anchorMin = new Vector2(0.61f, 0.02f);
            rightRT.anchorMax = new Vector2(0.99f, 0.91f);
            rightRT.offsetMin = Vector2.zero; rightRT.offsetMax = Vector2.zero;
            Image rightBg = rightPanel.AddComponent<Image>();
            rightBg.color = new Color(0.08f, 0.07f, 0.05f, 0.92f);

            // Item name (large, top)
            GameObject nameGO = new GameObject("DetailName");
            nameGO.transform.SetParent(rightPanel.transform, false);
            RectTransform nameRT = nameGO.AddComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0.04f, 0.82f);
            nameRT.anchorMax = new Vector2(0.96f, 0.98f);
            nameRT.offsetMin = Vector2.zero; nameRT.offsetMax = Vector2.zero;
            var nameTmp = nameGO.AddComponent<TMPro.TextMeshProUGUI>();
            nameTmp.text = "Wybierz przedmiot…";
            nameTmp.fontSize = 23;
            nameTmp.fontStyle = TMPro.FontStyles.Bold;
            nameTmp.alignment = TMPro.TextAlignmentOptions.TopLeft;
            nameTmp.color = new Color(0.9f, 0.85f, 0.65f);
            nameTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // Description
            GameObject descGO = new GameObject("DetailDesc");
            descGO.transform.SetParent(rightPanel.transform, false);
            RectTransform descRT = descGO.AddComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.04f, 0.62f);
            descRT.anchorMax = new Vector2(0.96f, 0.82f);
            descRT.offsetMin = Vector2.zero; descRT.offsetMax = Vector2.zero;
            var descTmp = descGO.AddComponent<TMPro.TextMeshProUGUI>();
            descTmp.text = "";
            descTmp.fontSize = 23;
            descTmp.color = new Color(0.75f, 0.7f, 0.6f);
            descTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            descTmp.alignment = TMPro.TextAlignmentOptions.TopLeft;

            // Stats
            GameObject statsGO = new GameObject("DetailStats");
            statsGO.transform.SetParent(rightPanel.transform, false);
            RectTransform statsRT = statsGO.AddComponent<RectTransform>();
            statsRT.anchorMin = new Vector2(0.04f, 0.38f);
            statsRT.anchorMax = new Vector2(0.96f, 0.62f);
            statsRT.offsetMin = Vector2.zero; statsRT.offsetMax = Vector2.zero;
            var statsTmp = statsGO.AddComponent<TMPro.TextMeshProUGUI>();
            statsTmp.text = "";
            statsTmp.fontSize = 23;
            statsTmp.color = new Color(0.55f, 0.85f, 0.55f);
            statsTmp.alignment = TMPro.TextAlignmentOptions.TopLeft;
            statsTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // ── Action buttons (bottom 35 % of right panel) ───────────────────
            Button equipBtn = CreateDetailButton(rightPanel.transform, "EquipButton",
                "Załóż", new Color(0.15f, 0.35f, 0.15f),
                new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.36f));

            Button useBtn = CreateDetailButton(rightPanel.transform, "UseButton",
                "Użyj", new Color(0.15f, 0.2f, 0.35f),
                new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.24f));

            Button dropBtn = CreateDetailButton(rightPanel.transform, "DropButton",
                "Wyrzuć", new Color(0.35f, 0.12f, 0.1f),
                new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.12f));

            // ── FOOTER (gold display) ─────────────────────────────────────────
            GameObject footerGO = new GameObject("GoldText");
            footerGO.transform.SetParent(root.transform, false);
            RectTransform footerRT = footerGO.AddComponent<RectTransform>();
            footerRT.anchorMin = new Vector2(0.01f, 0.915f);
            footerRT.anchorMax = new Vector2(0.55f, 0.945f);
            footerRT.offsetMin = Vector2.zero; footerRT.offsetMax = Vector2.zero;
            var goldTmp = footerGO.AddComponent<TMPro.TextMeshProUGUI>();
            goldTmp.text = "Złoto: 0";
            goldTmp.fontSize = 20;
            goldTmp.color = new Color(0.95f, 0.8f, 0.2f);
            goldTmp.alignment = TMPro.TextAlignmentOptions.Left;

            // ── InventoryUIController ─────────────────────────────────────────
            GameObject ctrlGO = new GameObject("InventoryUIController");
            ctrlGO.transform.SetParent(root.transform, false);
            var ctrl = ctrlGO.AddComponent<UI.InventoryUIController>();

            SerializedObject so = new SerializedObject(ctrl);
            so.FindProperty("inventoryPanel").objectReferenceValue     = root;
            so.FindProperty("weaponSlotIcon").objectReferenceValue     = weaponIcon;
            so.FindProperty("armorSlotIcon").objectReferenceValue      = armorIcon;
            so.FindProperty("accessorySlotIcon").objectReferenceValue  = accessoryIcon;
            so.FindProperty("weaponSlotLabel").objectReferenceValue    = weaponLabel;
            so.FindProperty("armorSlotLabel").objectReferenceValue     = armorLabel;
            so.FindProperty("accessorySlotLabel").objectReferenceValue = accessoryLabel;
            so.FindProperty("bagGridParent").objectReferenceValue      = bagGrid.transform;
            so.FindProperty("detailPanel").objectReferenceValue        = rightPanel;
            so.FindProperty("detailName").objectReferenceValue         = nameTmp;
            so.FindProperty("detailDesc").objectReferenceValue         = descTmp;
            so.FindProperty("detailStats").objectReferenceValue        = statsTmp;
            so.FindProperty("equipButton").objectReferenceValue        = equipBtn;
            so.FindProperty("useButton").objectReferenceValue          = useBtn;
            so.FindProperty("dropButton").objectReferenceValue         = dropBtn;
            so.FindProperty("goldText").objectReferenceValue           = goldTmp;
            so.FindProperty("closeButton").objectReferenceValue        = closeBtn;
            so.FindProperty("itemsSheet").objectReferenceValue         = itemsTex;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Hidden by default
            root.SetActive(false);

            return (root, ctrl);
        }

        // ── Inventory UI helpers ──────────────────────────────────

        private static (Image icon, TMPro.TextMeshProUGUI label)
            CreateEquipSlot(Transform parent, string name, string emptyText,
                Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject slotGO = new GameObject(name);
            slotGO.transform.SetParent(parent, false);
            RectTransform rt = slotGO.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(4f, 4f); rt.offsetMax = new Vector2(-4f, -4f);
            Image bg = slotGO.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.1f, 0.07f, 0.9f);
            slotGO.AddComponent<Button>().targetGraphic = bg;

            // Icon (child, full size)
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(slotGO.transform, false);
            RectTransform iconRT = iconGO.AddComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.1f, 0.25f);
            iconRT.anchorMax = new Vector2(0.9f, 0.9f);
            iconRT.offsetMin = Vector2.zero; iconRT.offsetMax = Vector2.zero;
            Image icon = iconGO.AddComponent<Image>();
            icon.color = new Color(1f, 1f, 1f, 0.25f);
            icon.preserveAspect = true;

            // Label (bottom strip)
            GameObject labelGO = new GameObject("SlotLabel");
            labelGO.transform.SetParent(slotGO.transform, false);
            RectTransform labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0f, 0f);
            labelRT.anchorMax = new Vector2(1f, 0.28f);
            labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;
            var lbl = labelGO.AddComponent<TMPro.TextMeshProUGUI>();
            lbl.text = emptyText;
            lbl.fontSize = 20;
            lbl.alignment = TMPro.TextAlignmentOptions.Center;
            lbl.color = new Color(0.65f, 0.6f, 0.45f);

            return (icon, lbl);
        }

        private static Button CreateDetailButton(Transform parent, string name,
            string label, Color bgColor, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = new Vector2(0f, 2f); rt.offsetMax = new Vector2(0f, -2f);
            Image bg = go.AddComponent<Image>();
            bg.color = bgColor;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            ColorBlock cb = btn.colors;
            cb.highlightedColor = bgColor * 1.4f;
            cb.pressedColor     = bgColor * 0.7f;
            btn.colors = cb;

            GameObject lblGO = new GameObject("Label");
            lblGO.transform.SetParent(go.transform, false);
            var lrt = lblGO.AddComponent<RectTransform>();
            SetFullAnchor(lrt);
            var tmp = lblGO.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 20;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private static void SetFullAnchor(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Creates a navigation button (Back / Confirm).</summary>
        private static Button CreateNavButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Color bgColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin  = anchorMin;
            rect.anchorMax  = anchorMax;
            rect.offsetMin  = new Vector2(8f, 4f);
            rect.offsetMax  = new Vector2(-8f, -4f);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = bgColor;
            cb.highlightedColor = bgColor * 1.3f;
            cb.pressedColor     = bgColor * 0.8f;
            btn.colors = cb;
            btn.targetGraphic = bg;

            GameObject txtObj = new GameObject("Label");
            txtObj.transform.SetParent(btnObj.transform, false);
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            var txt = txtObj.AddComponent<TMPro.TextMeshProUGUI>();
            txt.text      = label;
            txt.fontSize  = 19;
            txt.fontStyle = TMPro.FontStyles.Bold;
            txt.alignment = TMPro.TextAlignmentOptions.Center;
            txt.color     = Color.white;

            return btn;
        }
    }
}
#endif
