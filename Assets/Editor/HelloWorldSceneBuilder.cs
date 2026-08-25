using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using Cloud2026.Core;
using Cloud2026.Services;
using Cloud2026.UI;

namespace Cloud2026.EditorTools
{
    /// <summary>
    /// Genera la escena del "hola mundo" de Cloud Code por código.
    ///
    /// Montarla a mano en el Editor sería igual de válido, pero así queda versionada
    /// como código: cualquiera puede regenerarla desde cero y ver exactamente qué
    /// objetos la componen y cómo se conectan entre sí.
    /// </summary>
    public static class HelloWorldSceneBuilder
    {
        private const string ScenesFolder = "Assets/Scenes";
        private const string ScenePath = ScenesFolder + "/HelloWorld.unity";

        [MenuItem("Cloud2026/Crear escena Hola Mundo")]
        public static void CreateHelloWorldScene()
        {
            if (System.IO.File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog(
                    "Sobrescribir escena",
                    "Ya existe " + ScenePath + ". ¿Quieres reemplazarla?",
                    "Reemplazar", "Cancelar"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            BuildBootstrap();
            var canvas = BuildCanvas();
            BuildEventSystem();
            BuildPanel(canvas.transform);

            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            Debug.Log("[HelloWorldSceneBuilder] Escena creada en " + ScenePath +
                      ". Recuerda desplegar el módulo HelloWorld desde la ventana de Deployment antes de darle a Play.");
        }

        /// <summary>
        /// Objeto de arranque: inicializa UGS, entra como invitado y expone los
        /// servicios al resto de la escena.
        /// </summary>
        private static void BuildBootstrap()
        {
            var go = new GameObject("GameBootstrap",
                typeof(UGSAuthService),
                typeof(UGSCloudCodeService),
                typeof(GameBootstrap));

            var bootstrap = go.GetComponent<GameBootstrap>();
            var serialized = new SerializedObject(bootstrap);

            serialized.FindProperty("authService").objectReferenceValue = go.GetComponent<UGSAuthService>();
            serialized.FindProperty("cloudCodeService").objectReferenceValue = go.GetComponent<UGSCloudCodeService>();

            // Para el hola mundo entramos como invitados automáticamente: sin sesión,
            // Cloud Code no sabe quién llama y rechaza la petición.
            serialized.FindProperty("autoLoginAnonymous").boolValue = true;

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Canvas BuildCanvas()
        {
            var go = new GameObject("Canvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        /// <summary>
        /// El proyecto usa el Input System nuevo, así que el EventSystem necesita
        /// InputSystemUIInputModule; con el módulo antiguo los botones no responden.
        /// </summary>
        private static void BuildEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        private static void BuildPanel(Transform canvasTransform)
        {
            var panelGO = new GameObject("HelloWorldPanel",
                typeof(Image), typeof(VerticalLayoutGroup), typeof(HelloWorldPanel));
            panelGO.transform.SetParent(canvasTransform, false);

            var rect = panelGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(900f, 620f);

            panelGO.GetComponent<Image>().color = new Color(0.09f, 0.11f, 0.16f, 0.95f);

            var layout = panelGO.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var resources = BuildControlResources();
            var parent = panelGO.transform;

            CreateText(parent, resources, "Title", "Hola Mundo · Cloud Code",
                44f, TextAlignmentOptions.Center, 64f, Color.white);

            var nameInput = CreateInputField(parent, resources);
            var button = CreateButton(parent, resources);

            var messageText = CreateText(parent, resources, "MessageText",
                "Aquí aparecerá el saludo que componga el servidor.",
                32f, TextAlignmentOptions.Center, 110f, new Color(0.85f, 0.9f, 1f));

            var detailsText = CreateText(parent, resources, "DetailsText", string.Empty,
                22f, TextAlignmentOptions.Center, 90f, new Color(0.65f, 0.7f, 0.8f));

            var statusText = CreateText(parent, resources, "StatusText",
                "Esperando a que se inicie la sesión en UGS...",
                22f, TextAlignmentOptions.Center, 60f, Color.yellow);

            var panel = panelGO.GetComponent<HelloWorldPanel>();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("nameInput").objectReferenceValue = nameInput;
            serialized.FindProperty("sayHelloButton").objectReferenceValue = button;
            serialized.FindProperty("messageText").objectReferenceValue = messageText;
            serialized.FindProperty("detailsText").objectReferenceValue = detailsText;
            serialized.FindProperty("statusText").objectReferenceValue = statusText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_DefaultControls.Resources BuildControlResources()
        {
            return new TMP_DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd")
            };
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, TMP_DefaultControls.Resources resources, string name,
            string content, float fontSize, TextAlignmentOptions alignment, float height, Color color)
        {
            var go = TMP_DefaultControls.CreateText(resources);
            go.name = name;
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;

            SetPreferredHeight(go, height);
            return text;
        }

        private static TMP_InputField CreateInputField(Transform parent, TMP_DefaultControls.Resources resources)
        {
            var go = TMP_DefaultControls.CreateInputField(resources);
            go.name = "NameInput";
            go.transform.SetParent(parent, false);

            var input = go.GetComponent<TMP_InputField>();
            input.text = string.Empty;
            input.pointSize = 28f;

            if (input.placeholder is TextMeshProUGUI placeholder)
            {
                placeholder.text = "Escribe un nombre (opcional)";
                placeholder.fontSize = 28f;
            }

            if (input.textComponent != null)
            {
                input.textComponent.fontSize = 28f;
            }

            SetPreferredHeight(go, 64f);
            return input;
        }

        private static Button CreateButton(Transform parent, TMP_DefaultControls.Resources resources)
        {
            var go = TMP_DefaultControls.CreateButton(resources);
            go.name = "SayHelloButton";
            go.transform.SetParent(parent, false);

            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = "Saludar al servidor";
                label.fontSize = 30f;
            }

            SetPreferredHeight(go, 72f);
            return go.GetComponent<Button>();
        }

        /// <summary>
        /// El VerticalLayoutGroup no controla la altura de los hijos, así que cada uno
        /// declara la suya con un LayoutElement.
        /// </summary>
        private static void SetPreferredHeight(GameObject go, float height)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = go.AddComponent<LayoutElement>();
            }

            element.preferredHeight = height;
            element.minHeight = height;
        }

        private static void AddSceneToBuildSettings()
        {
            var current = EditorBuildSettings.scenes;

            foreach (var entry in current)
            {
                if (entry.path == ScenePath) return;
            }

            var updated = new EditorBuildSettingsScene[current.Length + 1];
            current.CopyTo(updated, 0);
            updated[current.Length] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = updated;
        }
    }
}
