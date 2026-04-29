using UnityEngine;
using System.Collections;

namespace AiMiniGames.GameWatermelon
{
    // 挂在每个水果物体上，负责接收控制器配置并在碰撞时申请合成。
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class WatermelonFruit : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D cachedRigidbody;
        [SerializeField] private CircleCollider2D cachedCollider;
        [SerializeField] private SpriteRenderer cachedRenderer;
        [SerializeField] private bool autoDecorateVisual = true;

        private WatermelonGameController controller;
        private int tierIndex;
        private bool isPreview;
        private bool mergeLocked;
        private bool countedInLoseLine;
        private float loseLineIgnoreUntil;
        private Coroutine popAnimationRoutine;
        private SpriteRenderer outlineRenderer;
        private SpriteRenderer highlightRenderer;
        private SpriteRenderer badgeRenderer;
        private TextMesh labelTextMesh;
        private Color bodyBaseColor = Color.white;
        private Color outlineBaseColor = Color.white;
        private Color highlightBaseColor = Color.white;
        private Color badgeBaseColor = Color.white;
        private Color labelBaseColor = Color.white;

        public int TierIndex => tierIndex;

        public bool IsPreview => isPreview;

        public bool MergeLocked => mergeLocked;

        public bool CountedInLoseLine => countedInLoseLine;

        public bool CanTriggerLoseLine => !isPreview && Time.time >= loseLineIgnoreUntil;

        private void Reset()
        {
            CacheComponents();
        }

        private void Awake()
        {
            CacheComponents();
        }

        private void OnValidate()
        {
            CacheComponents();
        }

        private void LateUpdate()
        {
            // 让文字始终保持正向，避免水果滚动时名字跟着旋转得太乱。
            if (labelTextMesh != null)
            {
                labelTextMesh.transform.localRotation = Quaternion.Euler(0f, 0f, -transform.eulerAngles.z);
            }
        }

        // 由控制器在生成水果时调用，写入层级和外观配置。
        public void Initialize(WatermelonGameController owner, int targetTierIndex, bool previewMode)
        {
            controller = owner;
            tierIndex = targetTierIndex;
            mergeLocked = false;
            countedInLoseLine = false;
            loseLineIgnoreUntil = 0f;

            ApplyTierVisual();
            SetPreviewMode(previewMode);
        }

        // 预览水果不参与物理，真正放手后再激活碰撞和重力。
        public void SetPreviewMode(bool previewMode)
        {
            isPreview = previewMode;

            if (cachedRigidbody != null)
            {
                cachedRigidbody.simulated = !previewMode;
                cachedRigidbody.bodyType = previewMode ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
                cachedRigidbody.velocity = Vector2.zero;
                cachedRigidbody.angularVelocity = 0f;
            }

            if (cachedCollider != null)
            {
                cachedCollider.enabled = !previewMode;
            }

            ApplyVisualAlpha(previewMode ? 0.7f : 1f);
        }

        // 投放时给水果一个很短的失败线宽限，避免刚生成就立刻开始判负。
        public void ReleaseFromPreview(float loseLineGraceDuration)
        {
            SetPreviewMode(false);
            loseLineIgnoreUntil = Time.time + Mathf.Max(0f, loseLineGraceDuration);
        }

        // 合并处理中会先锁住两个水果，避免一次碰撞触发多次合成。
        public void SetMergeLocked(bool locked)
        {
            mergeLocked = locked;
        }

        // 失败线统计时只希望每个水果算一次，避免同一水果重复进入重复计数。
        public void SetCountedInLoseLine(bool counted)
        {
            countedInLoseLine = counted;
        }

        // 合成前先关闭物理和碰撞，这样缩放消失时不会继续参与计算。
        public void PrepareForMerge()
        {
            mergeLocked = true;

            if (cachedCollider != null)
            {
                cachedCollider.enabled = false;
            }

            if (cachedRigidbody != null)
            {
                cachedRigidbody.simulated = false;
                cachedRigidbody.velocity = Vector2.zero;
                cachedRigidbody.angularVelocity = 0f;
            }
        }

        // 新水果出现时轻微弹一下，合成反馈会更明显。
        public void PlaySpawnPop(float extraScale = 0.18f, float duration = 0.16f)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (popAnimationRoutine != null)
            {
                StopCoroutine(popAnimationRoutine);
            }

            popAnimationRoutine = StartCoroutine(PlaySpawnPopRoutine(extraScale, duration));
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isPreview || mergeLocked || controller == null)
            {
                return;
            }

            if (!collision.collider.TryGetComponent<WatermelonFruit>(out var otherFruit))
            {
                return;
            }

            controller.TryMerge(this, otherFruit);
        }

        private void OnDisable()
        {
            if (controller != null && countedInLoseLine)
            {
                controller.NotifyFruitExitedLoseLine(this);
            }
        }

        private void ApplyTierVisual()
        {
            if (controller == null)
            {
                return;
            }

            var definition = controller.GetTierDefinition(tierIndex);
            if (definition == null)
            {
                return;
            }

            transform.localScale = Vector3.one * definition.Diameter;
            gameObject.name = $"Fruit_{tierIndex}_{definition.DisplayName}";

            if (cachedRenderer != null)
            {
                bodyBaseColor = definition.Color;
            }

            if (autoDecorateVisual)
            {
                EnsureDecorationObjects();
                UpdateDecorationVisuals(definition);
            }

            ApplyVisualAlpha(isPreview ? 0.7f : 1f);
        }

        private void CacheComponents()
        {
            if (cachedRigidbody == null)
            {
                cachedRigidbody = GetComponent<Rigidbody2D>();
            }

            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<CircleCollider2D>();
            }

            if (cachedRenderer == null)
            {
                cachedRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void EnsureDecorationObjects()
        {
            if (cachedRenderer == null || cachedRenderer.sprite == null)
            {
                return;
            }

            if (outlineRenderer == null)
            {
                outlineRenderer = CreateDecorationSpriteRenderer("Outline", -1);
            }

            if (highlightRenderer == null)
            {
                highlightRenderer = CreateDecorationSpriteRenderer("Highlight", 1);
            }

            if (badgeRenderer == null)
            {
                badgeRenderer = CreateDecorationSpriteRenderer("Badge", 2);
            }

            if (labelTextMesh == null)
            {
                var labelObject = FindOrCreateChild("Label");
                labelObject.transform.localPosition = new Vector3(0f, -0.02f, 0f);
                labelObject.transform.localScale = Vector3.one;
                labelObject.transform.localRotation = Quaternion.identity;

                labelTextMesh = labelObject.GetComponent<TextMesh>();
                if (labelTextMesh == null)
                {
                    labelTextMesh = labelObject.AddComponent<TextMesh>();
                }

                labelTextMesh.anchor = TextAnchor.MiddleCenter;
                labelTextMesh.alignment = TextAlignment.Center;
                labelTextMesh.fontSize = 64;
                labelTextMesh.characterSize = 0.08f;
                labelTextMesh.GetComponent<MeshRenderer>().sortingLayerID = cachedRenderer.sortingLayerID;
                labelTextMesh.GetComponent<MeshRenderer>().sortingOrder = cachedRenderer.sortingOrder + 3;

                var builtinFont = LoadBuiltinFont();
                if (builtinFont != null)
                {
                    labelTextMesh.font = builtinFont;
                    labelTextMesh.GetComponent<MeshRenderer>().sharedMaterial = builtinFont.material;
                }
            }
        }

        private void UpdateDecorationVisuals(WatermelonTierDefinition definition)
        {
            if (cachedRenderer == null || cachedRenderer.sprite == null)
            {
                return;
            }

            var baseColor = definition.Color;
            outlineBaseColor = Color.Lerp(baseColor, Color.black, 0.35f);
            highlightBaseColor = new Color(1f, 1f, 1f, 0.28f);
            badgeBaseColor = new Color(1f, 1f, 1f, 0.30f);
            labelBaseColor = new Color(0.24f, 0.16f, 0.12f, 1f);

            SyncDecorationRenderer(outlineRenderer, cachedRenderer.sprite);
            SyncDecorationRenderer(highlightRenderer, cachedRenderer.sprite);
            SyncDecorationRenderer(badgeRenderer, cachedRenderer.sprite);

            if (outlineRenderer != null)
            {
                outlineRenderer.transform.localPosition = Vector3.zero;
                outlineRenderer.transform.localScale = Vector3.one * 1.08f;
            }

            if (highlightRenderer != null)
            {
                highlightRenderer.transform.localPosition = new Vector3(-0.18f, 0.18f, 0f);
                highlightRenderer.transform.localScale = Vector3.one * 0.42f;
            }

            if (badgeRenderer != null)
            {
                badgeRenderer.transform.localPosition = new Vector3(0f, -0.02f, 0f);
                badgeRenderer.transform.localScale = Vector3.one * 0.48f;
            }

            if (labelTextMesh != null)
            {
                labelTextMesh.text = GetLabelContent(definition);
            }
        }

        private void SyncDecorationRenderer(SpriteRenderer targetRenderer, Sprite sourceSprite)
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.sprite = sourceSprite;
            targetRenderer.sortingLayerID = cachedRenderer.sortingLayerID;
            targetRenderer.maskInteraction = SpriteMaskInteraction.None;
        }

        private SpriteRenderer CreateDecorationSpriteRenderer(string childName, int sortingOffset)
        {
            var child = FindOrCreateChild(childName);
            child.transform.localScale = Vector3.one;
            child.transform.localRotation = Quaternion.identity;

            var renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = child.AddComponent<SpriteRenderer>();
            }

            renderer.sortingLayerID = cachedRenderer.sortingLayerID;
            renderer.sortingOrder = cachedRenderer.sortingOrder + sortingOffset;
            renderer.drawMode = SpriteDrawMode.Simple;
            return renderer;
        }

        private GameObject FindOrCreateChild(string childName)
        {
            var child = transform.Find(childName);
            if (child != null)
            {
                return child.gameObject;
            }

            var childObject = new GameObject(childName);
            childObject.transform.SetParent(transform, false);
            return childObject;
        }

        private void ApplyVisualAlpha(float alpha)
        {
            if (cachedRenderer != null)
            {
                cachedRenderer.color = WithAlpha(bodyBaseColor, alpha);
            }

            if (outlineRenderer != null)
            {
                outlineRenderer.color = WithAlpha(outlineBaseColor, alpha);
            }

            if (highlightRenderer != null)
            {
                highlightRenderer.color = WithAlpha(highlightBaseColor, highlightBaseColor.a * alpha);
            }

            if (badgeRenderer != null)
            {
                badgeRenderer.color = WithAlpha(badgeBaseColor, badgeBaseColor.a * alpha);
            }

            if (labelTextMesh != null)
            {
                labelTextMesh.color = WithAlpha(labelBaseColor, alpha);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static string GetLabelContent(WatermelonTierDefinition definition)
        {
            if (definition == null)
            {
                return "?";
            }

            if (!string.IsNullOrWhiteSpace(definition.ShortLabel))
            {
                return definition.ShortLabel.Trim();
            }

            if (!string.IsNullOrWhiteSpace(definition.DisplayName))
            {
                return definition.DisplayName.Trim().Substring(0, 1);
            }

            return "?";
        }

        private static Font LoadBuiltinFont()
        {
            var builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (builtinFont != null)
            {
                return builtinFont;
            }

            // 兜底兼容旧版本 Unity。
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private IEnumerator PlaySpawnPopRoutine(float extraScale, float duration)
        {
            var baseScale = transform.localScale;
            var peakScale = baseScale * (1f + Mathf.Max(0f, extraScale));
            var halfDuration = Mathf.Max(0.01f, duration * 0.5f);
            var elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / halfDuration);
                transform.localScale = Vector3.LerpUnclamped(baseScale, peakScale, progress);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / halfDuration);
                transform.localScale = Vector3.LerpUnclamped(peakScale, baseScale, progress);
                yield return null;
            }

            transform.localScale = baseScale;
            popAnimationRoutine = null;
        }
    }
}
