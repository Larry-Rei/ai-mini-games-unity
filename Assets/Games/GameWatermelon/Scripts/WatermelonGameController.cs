using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AiMiniGames.GameWatermelon
{
    // 主控制器：管理预览水果、投放、碰撞合成和基础分数。
    public sealed class WatermelonGameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private WatermelonFruit fruitPrefab;
        [SerializeField] private Transform fruitRoot;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text stateText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Image nextFruitPreviewImage;
        [SerializeField] private Text nextFruitPreviewText;

        [Header("Spawn")]
        [SerializeField] private float spawnY = 4.5f;
        [SerializeField] private float spawnMinX = -2.5f;
        [SerializeField] private float spawnMaxX = 2.5f;
        [SerializeField] private float dropCooldown = 0.2f;
        [SerializeField] private int randomTierRange = 5;
        [SerializeField] private float nextPreviewUiSizeMultiplier = 56f;

        [Header("Progression")]
        [SerializeField] private WatermelonTierDefinition[] tierDefinitions =
        {
            new(),
            new(),
            new(),
            new(),
            new(),
            new(),
            new(),
            new()
        };

        [Header("Lose Rule")]
        [SerializeField] private float loseLineHoldDuration = 1.2f;
        [SerializeField] private float dropLoseLineGraceDuration = 0.35f;

        [Header("Feedback")]
        [SerializeField] private float mergeShrinkDuration = 0.08f;
        [SerializeField] private float mergeSpawnPopScale = 0.18f;
        [SerializeField] private float mergeSpawnPopDuration = 0.16f;

        private WatermelonFruit previewFruit;
        private System.Random random;
        private bool canDrop = true;
        private int score;
        private bool isGameOver;
        private float loseLineTimer = -1f;
        private readonly HashSet<WatermelonFruit> fruitsInsideLoseLine = new();
        private int currentPreviewTier = -1;
        private int nextPreviewTier = -1;

        public int Score => score;

        private void Awake()
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            if (fruitRoot == null)
            {
                fruitRoot = transform;
            }

            random = new System.Random();
        }

        private void Start()
        {
            UpdateUi();

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(ResetGame);
            }

            InitializePreviewQueue();
            SpawnPreviewFruit();
        }

        private void Update()
        {
            UpdateLoseLineTimer();

            if (isGameOver)
            {
                return;
            }

            if (previewFruit == null || gameplayCamera == null)
            {
                return;
            }

            UpdatePreviewPosition();

            if (canDrop && Input.GetMouseButtonDown(0))
            {
                DropPreviewFruit();
            }
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(ResetGame);
            }
        }

        // 给水果脚本读取对应层级的显示配置。
        public WatermelonTierDefinition GetTierDefinition(int tierIndex)
        {
            if (tierIndex < 0 || tierIndex >= tierDefinitions.Length)
            {
                return null;
            }

            return tierDefinitions[tierIndex];
        }

        // 两个同级水果接触后，尝试把它们合成成更高一级。
        public void TryMerge(WatermelonFruit first, WatermelonFruit second)
        {
            if (first == null || second == null || first == second)
            {
                return;
            }

            if (first.IsPreview || second.IsPreview || first.MergeLocked || second.MergeLocked)
            {
                return;
            }

            if (first.TierIndex != second.TierIndex)
            {
                return;
            }

            var nextTier = first.TierIndex + 1;
            if (nextTier >= tierDefinitions.Length)
            {
                return;
            }

            first.SetMergeLocked(true);
            second.SetMergeLocked(true);
            StartCoroutine(PlayMergeSequence(first, second, nextTier));
        }

        // 重新开始时可以直接清场并重建一个预览水果。
        public void ResetGame()
        {
            score = 0;
            isGameOver = false;
            canDrop = true;
            loseLineTimer = -1f;
            fruitsInsideLoseLine.Clear();
            currentPreviewTier = -1;
            nextPreviewTier = -1;

            var existingFruits = fruitRoot.GetComponentsInChildren<WatermelonFruit>();
            for (var index = 0; index < existingFruits.Length; index++)
            {
                Destroy(existingFruits[index].gameObject);
            }

            previewFruit = null;
            UpdateUi();
            InitializePreviewQueue();
            SpawnPreviewFruit();
        }

        // 失败线通知：有水果进入危险区域。
        public void NotifyFruitEnteredLoseLine(WatermelonFruit fruit)
        {
            if (fruit == null || fruit.IsPreview || fruit.CountedInLoseLine || !fruit.CanTriggerLoseLine)
            {
                return;
            }

            var wasEmpty = fruitsInsideLoseLine.Count == 0;
            fruit.SetCountedInLoseLine(true);
            fruitsInsideLoseLine.Add(fruit);

            if (!isGameOver && wasEmpty)
            {
                loseLineTimer = loseLineHoldDuration;
                UpdateStateText("危险：水果碰到失败线");
            }
        }

        // 失败线通知：水果离开危险区域后，危险倒计时可以取消。
        public void NotifyFruitExitedLoseLine(WatermelonFruit fruit)
        {
            if (fruit == null)
            {
                return;
            }

            fruit.SetCountedInLoseLine(false);
            fruitsInsideLoseLine.Remove(fruit);

            if (fruitsInsideLoseLine.Count == 0 && !isGameOver)
            {
                loseLineTimer = -1f;
                UpdateStateText("继续堆叠");
            }
        }

        private void UpdatePreviewPosition()
        {
            var mouseScreenPosition = Input.mousePosition;
            var worldPosition = gameplayCamera.ScreenToWorldPoint(mouseScreenPosition);
            var clampedX = Mathf.Clamp(worldPosition.x, spawnMinX, spawnMaxX);

            previewFruit.transform.position = new Vector3(clampedX, spawnY, 0f);
        }

        private void DropPreviewFruit()
        {
            if (previewFruit == null)
            {
                return;
            }

            previewFruit.ReleaseFromPreview(dropLoseLineGraceDuration);
            previewFruit = null;
            AdvancePreviewQueue();
            canDrop = false;
            StartCoroutine(SpawnNextPreviewAfterDelay());
        }

        private void SpawnPreviewFruit()
        {
            if (fruitPrefab == null)
            {
                Debug.LogWarning("WatermelonGameController is missing fruitPrefab.", this);
                return;
            }

            var spawnPosition = new Vector3(0f, spawnY, 0f);
            if (currentPreviewTier < 0)
            {
                InitializePreviewQueue();
            }

            previewFruit = SpawnFruit(currentPreviewTier, spawnPosition, true);
            UpdatePreviewPosition();
            UpdateNextPreviewUi();
            UpdateStateText("点击投放水果");
        }

        private WatermelonFruit SpawnFruit(int tierIndex, Vector3 position, bool previewMode)
        {
            var fruit = Instantiate(fruitPrefab, position, Quaternion.identity, fruitRoot);
            fruit.Initialize(this, tierIndex, previewMode);
            return fruit;
        }

        private int GetRandomSpawnTier()
        {
            var clampedRange = Mathf.Clamp(randomTierRange, 1, tierDefinitions.Length);
            return random.Next(clampedRange);
        }

        // 初始化当前水果和下一颗水果，保证预览栏始终有内容。
        private void InitializePreviewQueue()
        {
            if (currentPreviewTier < 0)
            {
                currentPreviewTier = GetRandomSpawnTier();
            }

            if (nextPreviewTier < 0)
            {
                nextPreviewTier = GetRandomSpawnTier();
            }

            UpdateNextPreviewUi();
        }

        // 每次投放后把“下一颗”顶上来，再随机生成一个新的候补。
        private void AdvancePreviewQueue()
        {
            if (currentPreviewTier < 0 || nextPreviewTier < 0)
            {
                InitializePreviewQueue();
                return;
            }

            currentPreviewTier = nextPreviewTier;
            nextPreviewTier = GetRandomSpawnTier();
            UpdateNextPreviewUi();
        }

        private IEnumerator SpawnNextPreviewAfterDelay()
        {
            yield return new WaitForSecondsRealtime(dropCooldown);

            if (isGameOver)
            {
                yield break;
            }

            canDrop = true;
            SpawnPreviewFruit();
        }

        private IEnumerator PlayMergeSequence(WatermelonFruit first, WatermelonFruit second, int nextTier)
        {
            if (first == null || second == null)
            {
                yield break;
            }

            first.PrepareForMerge();
            second.PrepareForMerge();

            var firstTransform = first.transform;
            var secondTransform = second.transform;
            var firstStartScale = firstTransform.localScale;
            var secondStartScale = secondTransform.localScale;
            var mergePosition = (firstTransform.position + secondTransform.position) * 0.5f;
            var duration = Mathf.Max(0.01f, mergeShrinkDuration);
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

                if (first != null)
                {
                    firstTransform.localScale = Vector3.LerpUnclamped(firstStartScale, Vector3.zero, easedProgress);
                }

                if (second != null)
                {
                    secondTransform.localScale = Vector3.LerpUnclamped(secondStartScale, Vector3.zero, easedProgress);
                }

                yield return null;
            }

            var mergedFruit = SpawnFruit(nextTier, mergePosition, false);
            mergedFruit.PlaySpawnPop(mergeSpawnPopScale, mergeSpawnPopDuration);

            var scoreDefinition = GetTierDefinition(nextTier);
            if (scoreDefinition != null)
            {
                score += scoreDefinition.Score;
                UpdateUi();
            }

            if (first != null)
            {
                Destroy(first.gameObject);
            }

            if (second != null)
            {
                Destroy(second.gameObject);
            }
        }

        private void UpdateLoseLineTimer()
        {
            if (isGameOver || fruitsInsideLoseLine.Count == 0 || loseLineTimer < 0f)
            {
                return;
            }

            loseLineTimer -= Time.deltaTime;
            if (loseLineTimer <= 0f)
            {
                TriggerGameOver();
            }
        }

        private void TriggerGameOver()
        {
            isGameOver = true;
            canDrop = false;
            loseLineTimer = -1f;

            if (previewFruit != null)
            {
                Destroy(previewFruit.gameObject);
                previewFruit = null;
            }

            UpdateStateText("游戏结束");
        }

        private void UpdateUi()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }

            if (stateText != null && string.IsNullOrWhiteSpace(stateText.text))
            {
                stateText.text = "点击投放水果";
            }
        }

        private void UpdateNextPreviewUi()
        {
            var definition = GetTierDefinition(nextPreviewTier);

            if (nextFruitPreviewText != null)
            {
                nextFruitPreviewText.text = definition != null
                    ? $"下一个：{definition.DisplayName}"
                    : "下一个";
            }

            if (nextFruitPreviewImage == null)
            {
                return;
            }

            if (definition == null)
            {
                nextFruitPreviewImage.enabled = false;
                return;
            }

            nextFruitPreviewImage.enabled = true;
            nextFruitPreviewImage.preserveAspect = true;

            var previewColor = definition.Color;
            previewColor.a = 1f;
            nextFruitPreviewImage.color = previewColor;

            var previewRenderer = fruitPrefab != null ? fruitPrefab.GetComponent<SpriteRenderer>() : null;
            if (previewRenderer != null && previewRenderer.sprite != null)
            {
                nextFruitPreviewImage.sprite = previewRenderer.sprite;
            }

            var size = Mathf.Max(36f, definition.Diameter * nextPreviewUiSizeMultiplier);
            nextFruitPreviewImage.rectTransform.sizeDelta = new Vector2(size, size);
        }

        private void UpdateStateText(string content)
        {
            if (stateText != null)
            {
                stateText.text = content;
            }
        }
    }
}
