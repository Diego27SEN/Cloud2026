using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Cloud2026.EditorTools
{
    /// <summary>
    /// Piezas de interfaz para los generadores de escena.
    ///
    /// Nada de esto es imprescindible: se podría montar todo a mano en el Editor.
    /// Está en código para que las escenas del curso se puedan regenerar y para
    /// que se vea, leyéndolo, de qué se compone cada pantalla.
    /// </summary>
    public static class UiFactory
    {
        public static readonly Color PanelBackground = new Color(0.09f, 0.11f, 0.16f, 0.95f);
        public static readonly Color SubtleText = new Color(0.65f, 0.7f, 0.8f);

        public static TMP_DefaultControls.Resources Resources()
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

        public static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        /// <summary>
        /// Contenedor con fondo que apila a sus hijos en vertical y se ajusta a su
        /// altura. Al ocultar un hijo, el resto se recoloca solo.
        /// </summary>
        public static GameObject CreatePanel(Transform parent, string name, float width, bool withBackground)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);

            if (withBackground)
            {
                go.AddComponent<Image>().color = PanelBackground;
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, 0f);

            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 28, 28);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return go;
        }

        public static TextMeshProUGUI CreateText(
            Transform parent, TMP_DefaultControls.Resources resources, string name,
            string content, float fontSize, float height, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = TMP_DefaultControls.CreateText(resources);
            go.name = name;
            go.transform.SetParent(parent, false);

            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.richText = true;

            SetPreferredHeight(go, height);
            return text;
        }

        public static Button CreateButton(
            Transform parent, TMP_DefaultControls.Resources resources,
            string name, string label, float height = 64f, float fontSize = 26f)
        {
            var go = TMP_DefaultControls.CreateButton(resources);
            go.name = name;
            go.transform.SetParent(parent, false);

            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = label;
                text.fontSize = fontSize;
            }

            SetPreferredHeight(go, height);
            return go.GetComponent<Button>();
        }

        public static TMP_InputField CreateInputField(
            Transform parent, TMP_DefaultControls.Resources resources,
            string name, string placeholder, float fontSize = 26f)
        {
            var go = TMP_DefaultControls.CreateInputField(resources);
            go.name = name;
            go.transform.SetParent(parent, false);

            var input = go.GetComponent<TMP_InputField>();
            input.text = string.Empty;
            input.pointSize = fontSize;

            if (input.placeholder is TextMeshProUGUI hint)
            {
                hint.text = placeholder;
                hint.fontSize = fontSize;
            }

            if (input.textComponent != null)
            {
                input.textComponent.fontSize = fontSize;
            }

            SetPreferredHeight(go, 60f);
            return input;
        }

        /// <summary>
        /// El layout no adivina la altura de los hijos, así que cada uno declara
        /// la suya con un LayoutElement.
        /// </summary>
        public static void SetPreferredHeight(GameObject go, float height)
        {
            var element = go.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = go.AddComponent<LayoutElement>();
            }

            element.preferredHeight = height;
            element.minHeight = height;
        }

        /// <summary>Asigna un campo privado marcado con [SerializeField].</summary>
        public static void Wire(SerializedObject serialized, string fieldName, Object value)
        {
            var property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogWarning($"[UiFactory] No existe el campo serializado '{fieldName}'.");
                return;
            }

            property.objectReferenceValue = value;
        }
    }
}
