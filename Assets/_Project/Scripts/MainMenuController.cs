using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AiMiniGames.UI
{
    [Serializable]
    public sealed class MainMenuEntry
    {
        public string title = "2048";
        [TextArea(2, 3)] public string description = "滑动合并数字，冲击更高分数。";
        public string sceneName = "Game2048";
        public Color accentColor = new(0.93f, 0.57f, 0.24f, 1f);
    }

    // 主菜单控制器：运行时自动生成菜单 UI，减少手工摆控件的工作量。
    public sealed class MainMenuController : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private string menuTitle = "AI Mini Games";
        [SerializeField] [TextArea(2, 4)] private string menuSubtitle = "三个经典小游戏原型，全部基于 Unity 搭建，并由 AI 协助完成代码与美术流程。";
        [SerializeField] private string footerText = "选择一个小游戏开始体验";

        [Header("Entries")]
        [SerializeField] private MainMenuEntry[] entries =
        {
            new()
            {
                title = "2048",
                description = "滑动数字方块，合并出更大的数字。",
                sceneName = "Game2048",
                accentColor = new Color(0.93f, 0.57f, 0.24f, 1f)
            },
            new()
            {
                title = "Match 3",
                description = "交换相邻方块，组成三连并触发连锁消除。",
                sceneName = "GameMatch3",
                accentColor = new Color(0.92f, 0.35f, 0.44f, 1f)
            },
            new()
            {
                title = "合成大西瓜",
                description = "投放水果并不断合成，挑战更高体积与分数。",
                sceneName = "GameWatermelon",
                accentColor = new Color(0.35f, 0.66f, 0.34f, 1f)
            }
        };

        [Header("Style")]
        [SerializeField] private Color backgroundColor = new(0.95f, 0.90f, 0.84f, 1f);
        [SerializeField] private Color panelColor = new(0.98f, 0.96f, 0.93f, 0.94f);
        [SerializeField] private Color inkColor = new(0.18f, 0.15f, 0.13f, 1f);
        [SerializeField] private Color softInkColor = new(0.35f, 0.30f, 0.27f, 1f);

        private const string CanvasName = "MainMenuCanvas";
        private const string GeneratedRootName = "GeneratedMainMenuUI";

        private Font builtinFont;

        private void Reset()
        {
            EnsureEntryDefaults();
        }

        private void Awake()
        {
            EnsureEntryDefaults();
            builtinFont = LoadBuiltinFont();

            if (builtinFont == null)
            {
                Debug.LogError("MainMenuController could not load a built-in font.", this);
                return;
            }

            BuildMenu();
        }

        // 如果你调整了文案或颜色，运行场景会自动按最新配置重建菜单。
        private void BuildMenu()
        {
            EnsureEventSystem();

            var canvas = GetOrCreateCanvas();
            RemovePreviousGeneratedRoot(canvas.transform);

            var root = CreateUiObject(GeneratedRootName, canvas.transform);
            StretchToParent(root.GetComponent<RectTransform>());

            BuildBackground(root.transform);
            BuildHeader(root.transform);
            BuildEntryList(root.transform);
            BuildFooter(root.transform);
        }

        public void OpenScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("MainMenuController received an empty scene name.", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"Scene '{sceneName}' is not available. Check Build Settings and scene name.", this);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        private void BuildBackground(Transform parent)
        {
            var background = CreatePanel("Background", parent, backgroundColor);
            StretchToParent(background);

            var glowLeft = CreatePanel("GlowLeft", parent, new Color(0.96f, 0.76f, 0.44f, 0.38f));
            ConfigureRect(glowLeft, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(220f, -140f), new Vector2(360f, 360f));
            glowLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);

            var glowRight = CreatePanel("GlowRight", parent, new Color(0.40f, 0.74f, 0.56f, 0.32f));
            ConfigureRect(glowRight, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-180f, 180f), new Vector2(320f, 320f));
            glowRight.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);

            var frame = CreatePanel("Frame", parent, panelColor);
            ConfigureRect(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 610f));
            AddShadow(frame.gameObject, new Color(0.22f, 0.16f, 0.11f, 0.22f), new Vector2(0f, -12f));

            var stripe = CreatePanel("Stripe", frame.transform, new Color(0.25f, 0.19f, 0.16f, 0.08f));
            ConfigureRect(stripe, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -86f), new Vector2(0f, 2f));
        }

        private void BuildHeader(Transform parent)
        {
            var title = CreateText("Title", parent, menuTitle, 50, FontStyle.Bold, inkColor);
            ConfigureRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(760f, 72f));
            title.alignment = TextAnchor.MiddleCenter;

            var subtitle = CreateText("Subtitle", parent, menuSubtitle, 20, FontStyle.Normal, softInkColor);
            ConfigureRect(subtitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(720f, 86f));
            subtitle.alignment = TextAnchor.UpperCenter;
            subtitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitle.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void BuildEntryList(Transform parent)
        {
            var listRoot = CreateUiObject("EntryList", parent);
            ConfigureRect(listRoot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(820f, 310f));

            var layout = listRoot.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 22f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (var index = 0; index < entries.Length; index++)
            {
                BuildEntryCard(listRoot.transform, entries[index]);
            }
        }

        private void BuildEntryCard(Transform parent, MainMenuEntry entry)
        {
            var card = CreateUiObject($"{entry.title}_Card", parent);
            var layoutElement = card.AddComponent<LayoutElement>();
            layoutElement.minWidth = 240f;
            layoutElement.preferredWidth = 250f;
            layoutElement.minHeight = 330f;

            var cardImage = card.AddComponent<Image>();
            cardImage.color = new Color(1f, 1f, 1f, 0.70f);
            AddShadow(card, new Color(0.15f, 0.11f, 0.08f, 0.18f), new Vector2(0f, -8f));

            var button = card.AddComponent<Button>();
            button.targetGraphic = cardImage;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = CreateButtonColors(cardImage.color, entry.accentColor);
            button.onClick.AddListener(() => OpenScene(entry.sceneName));

            var accent = CreatePanel("Accent", card.transform, entry.accentColor);
            ConfigureRect(accent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(168f, 10f));

            var badge = CreatePanel("Badge", card.transform, entry.accentColor);
            ConfigureRect(badge, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(88f, 88f));
            badge.transform.localRotation = Quaternion.Euler(0f, 0f, -10f);

            var badgeText = CreateText("BadgeText", badge.transform, GetBadgeText(entry.title), 34, FontStyle.Bold, Color.white);
            StretchToParent(badgeText.rectTransform);
            badgeText.alignment = TextAnchor.MiddleCenter;

            var title = CreateText("EntryTitle", card.transform, entry.title, 22, FontStyle.Bold, inkColor);
            ConfigureRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(204f, 34f));
            title.alignment = TextAnchor.MiddleCenter;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 16;
            title.resizeTextMaxSize = 22;

            var description = CreateText("EntryDescription", card.transform, entry.description, 16, FontStyle.Normal, softInkColor);
            ConfigureRect(description.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -210f), new Vector2(194f, 74f));
            description.alignment = TextAnchor.UpperCenter;
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Truncate;
            description.lineSpacing = 1.08f;

            var action = CreateText("ActionText", card.transform, "点击进入", 16, FontStyle.Bold, entry.accentColor);
            ConfigureRect(action.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(180f, 28f));
            action.alignment = TextAnchor.MiddleCenter;
        }

        private void BuildFooter(Transform parent)
        {
            var footer = CreateText("Footer", parent, footerText, 18, FontStyle.Italic, softInkColor);
            ConfigureRect(footer.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(620f, 36f));
            footer.alignment = TextAnchor.MiddleCenter;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private Canvas GetOrCreateCanvas()
        {
            var existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                existingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                if (existingCanvas.GetComponent<CanvasScaler>() == null)
                {
                    existingCanvas.gameObject.AddComponent<CanvasScaler>();
                }

                if (existingCanvas.GetComponent<GraphicRaycaster>() == null)
                {
                    existingCanvas.gameObject.AddComponent<GraphicRaycaster>();
                }

                var existingScaler = existingCanvas.GetComponent<CanvasScaler>();
                existingScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                existingScaler.referenceResolution = new Vector2(1920f, 1080f);
                existingScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                existingScaler.matchWidthOrHeight = 0.5f;
                return existingCanvas;
            }

            var canvasObject = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private void RemovePreviousGeneratedRoot(Transform parent)
        {
            var previousRoot = parent.Find(GeneratedRootName);
            if (previousRoot == null)
            {
                return;
            }

            Destroy(previousRoot.gameObject);
        }

        private void EnsureEntryDefaults()
        {
            if (entries != null && entries.Length > 0)
            {
                return;
            }

            entries = new[]
            {
                new MainMenuEntry(),
                new MainMenuEntry
                {
                    title = "Match 3",
                    description = "交换相邻方块，组成三连并触发连锁消除。",
                    sceneName = "GameMatch3",
                    accentColor = new Color(0.92f, 0.35f, 0.44f, 1f)
                },
                new MainMenuEntry
                {
                    title = "合成大西瓜",
                    description = "投放水果并不断合成，挑战更高体积与分数。",
                    sceneName = "GameWatermelon",
                    accentColor = new Color(0.35f, 0.66f, 0.34f, 1f)
                }
            };
        }

        private static Font LoadBuiltinFont()
        {
            var runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (runtimeFont != null)
            {
                return runtimeFont;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private Text CreateText(string objectName, Transform parent, string content, int fontSize, FontStyle fontStyle, Color color)
        {
            var textObject = CreateUiObject(objectName, parent);
            var text = textObject.AddComponent<Text>();
            text.font = builtinFont;
            text.text = content;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.supportRichText = false;
            return text;
        }

        private RectTransform CreatePanel(string objectName, Transform parent, Color color)
        {
            var panelObject = CreateUiObject(objectName, parent);
            var image = panelObject.AddComponent<Image>();
            image.color = color;
            return image.rectTransform;
        }

        private static GameObject CreateUiObject(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void ConfigureRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static ColorBlock CreateButtonColors(Color normal, Color accent)
        {
            return new ColorBlock
            {
                normalColor = normal,
                highlightedColor = Color.Lerp(normal, accent, 0.22f),
                pressedColor = Color.Lerp(normal, accent, 0.40f),
                selectedColor = Color.Lerp(normal, accent, 0.24f),
                disabledColor = new Color(0.8f, 0.8f, 0.8f, 0.55f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
        }

        private static string GetBadgeText(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return "?";
            }

            return content.Trim().Substring(0, 1);
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            var shadow = target.AddComponent<Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }
    }
}
