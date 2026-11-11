using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampusQuest.IDGS
{
    // Small UI drawing helpers; gameplay and persistence remain in the existing systems.
    internal static class IDGSDesktopUI
    {
        internal static readonly Color Dark = new Color32(21, 28, 38, 255);
        internal static readonly Color Gold = new Color32(225, 195, 118, 255);
        internal static readonly Color Cyan = new Color32(97, 217, 220, 255);
        internal static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.sizeDelta = size; rect.anchoredPosition = position;
            return rect;
        }
        internal static Image Box(string name, Transform parent, Vector2 size, Vector2 position, Color color, bool raycast = false)
        {
            var image = Rect(name, parent, size, position).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = raycast; return image;
        }
        internal static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, Vector2 size, Vector2 position, float points, Color color)
        {
            var text = Rect(name, parent, size, position).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = points; text.color = color;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap; return text;
        }
        internal static Button Button(string name, Transform parent, TMP_FontAsset font, string label, Vector2 size, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            var image = Box(name, parent, size, position, Gold, true);
            Box("Interior", image.transform, size - Vector2.one * 4, Vector2.zero, Dark);
            Text("Texto", image.transform, font, label, size, Vector2.zero, 17, Gold);
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(action); return button;
        }
    }
}
