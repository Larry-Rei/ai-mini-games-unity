using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AiMiniGames.UI
{
    // 全局场景导航：在非主菜单场景自动生成一个返回按钮，方便来回切换小游戏。
    public sealed class SceneNavigationOverlay : MonoBehaviour
    {
        private const string BootstrapName = "SceneNavigationOverlay";
        private const string OverlayCanvasName = "NavigationOverlayCanvas";
        private const string OverlayRootName = "NavigationOverlayRoot";
        private const string MainMenuSceneName = "MainMenu";

        private static SceneNavigationOverlay instance;

        private Font builtinFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            var overlayObject = new GameObject(BootstrapName);
            instance = overlayObject.AddComponent<SceneNavigationOverlay>();
            DontDestroyOnLoad(overlayObject);
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            builtinFont = LoadBuiltinFont();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void Start()
        {
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid())
            {
                return;
            }

            if (scene.name == MainMenuSceneName)
            {
                RemoveOverlayCanvas();
                return;
            }

            EnsureEventSystem();
            BuildOverlayForCurrentScene();
        }

        private void BuildOverlayForCurrentScene()
        {
            if (builtinFont == null)
            {
                builtinFont = LoadBuiltinFont();
            }

            var canvas = GetOrCreateOverlayCanvas();
            RemovePreviousRoot(canvas.transform);

            var root = CreateUiObject(OverlayRootName, canvas.transform);
            StretchToParent(root.GetComponent<RectTransform>());

            var buttonObject = CreateUiObject("BackButton", root.transform);
            var buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(22f, -22f);
            buttonRect.sizeDelta = new Vector2(154f, 44f);

            var buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = new Color(0.17f, 0.15f, 0.14f, 0.86f);

            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = CreateButtonColors();
            button.onClick.AddListener(GoToMainMenu);

            var shadow = buttonObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
            shadow.effectDistance = new Vector2(0f, -4f);
            shadow.useGraphicAlpha = true;

            var label = CreateText("BackLabel", buttonObject.transform, "返回主菜单", 18, FontStyle.Bold, Color.white);
            StretchToParent(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
        }

        private void GoToMainMenu()
        {
            SceneManager.LoadScene(MainMenuSceneName);
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

        private Canvas GetOrCreateOverlayCanvas()
        {
            var existingCanvas = GameObject.Find(OverlayCanvasName);
            if (existingCanvas != null && existingCanvas.TryGetComponent<Canvas>(out var existing))
            {
                existing.renderMode = RenderMode.ScreenSpaceOverlay;
                existing.sortingOrder = 5000;
                return existing;
            }

            var canvasObject = new GameObject(OverlayCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private void RemoveOverlayCanvas()
        {
            var overlayCanvas = GameObject.Find(OverlayCanvasName);
            if (overlayCanvas != null)
            {
                Destroy(overlayCanvas);
            }
        }

        private static void RemovePreviousRoot(Transform parent)
        {
            var previousRoot = parent.Find(OverlayRootName);
            if (previousRoot != null)
            {
                Destroy(previousRoot.gameObject);
            }
        }

        private static GameObject CreateUiObject(string objectName, Transform parent)
        {
            var gameObject = new GameObject(objectName, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
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

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static ColorBlock CreateButtonColors()
        {
            return new ColorBlock
            {
                normalColor = new Color(0.17f, 0.15f, 0.14f, 0.86f),
                highlightedColor = new Color(0.27f, 0.23f, 0.21f, 0.94f),
                pressedColor = new Color(0.11f, 0.10f, 0.09f, 0.98f),
                selectedColor = new Color(0.24f, 0.21f, 0.18f, 0.92f),
                disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
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
    }
}
