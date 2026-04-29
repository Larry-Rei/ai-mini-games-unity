using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AiMiniGames.Match3
{
    // 负责生成三消棋盘 UI，并播放交换、消除、下落和补新块动画。
    public sealed class Match3BoardView : MonoBehaviour
    {
        [SerializeField] private Match3GameController controller;
        [SerializeField] private RectTransform boardRoot;
        [SerializeField] private Match3TileView tilePrefab;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text stateText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Vector2 cellSize = new(100f, 100f);
        [SerializeField] private Vector2 spacing = new(8f, 8f);

        [Header("Animation")]
        [SerializeField] private float swapDuration = 0.12f;
        [SerializeField] private float clearDuration = 0.14f;
        [SerializeField] private float fallDuration = 0.18f;
        [SerializeField] private float spawnPopScale = 0.85f;

        private readonly List<Match3TileView> tileViews = new();
        private readonly List<Match3TileView> animationTiles = new();
        private GridPosition? selectedPosition;
        private RectTransform animationRoot;
        private Coroutine animationCoroutine;

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.BoardChanged += HandleBoardChanged;
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);
            }
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.BoardChanged -= HandleBoardChanged;
                controller.SetInputLocked(false);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            ClearAnimationTiles();
        }

        private void Start()
        {
            RefreshBoardImmediate();
        }

        private void OnValidate()
        {
            if (controller == null)
            {
                controller = GetComponent<Match3GameController>();
            }
        }

        // 接收单个格子的点击：
        // 第一次点击记录选中，第二次点击尝试与第一次交换。
        public void HandleTileClicked(GridPosition position)
        {
            if (controller == null || controller.Board == null || animationCoroutine != null)
            {
                return;
            }

            if (selectedPosition == null)
            {
                selectedPosition = position;
                RefreshSelection();
                UpdateStateText($"已选中 {position}");
                return;
            }

            if (selectedPosition.Value.Equals(position))
            {
                selectedPosition = null;
                RefreshSelection();
                UpdateStateText("已取消选择");
                return;
            }

            var first = selectedPosition.Value;
            selectedPosition = null;

            var success = controller.TrySwap(
                new Vector2Int(first.X, first.Y),
                new Vector2Int(position.X, position.Y));

            if (!success)
            {
                RefreshSelection();
                RefreshBoardImmediate();
            }
        }

        [ContextMenu("Refresh Board")]
        // 立即按当前棋盘状态刷新显示，不播放过程动画。
        public void RefreshBoardImmediate()
        {
            if (controller == null || controller.Board == null || boardRoot == null || tilePrefab == null)
            {
                UpdateStateText("缺少棋盘引用");
                return;
            }

            EnsureGridLayout(controller.Board.Width, controller.Board.Height);
            EnsureTileViews(controller.Board.Width, controller.Board.Height);
            EnsureAnimationRoot();

            for (var y = 0; y < controller.Board.Height; y++)
            {
                for (var x = 0; x < controller.Board.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    var tileView = GetCellView(position);
                    if (tileView != null)
                    {
                        tileView.Setup(position);
                        tileView.SetTile(controller.GetTile(x, y));
                        tileView.ResetVisualState();
                    }
                }
            }

            UpdateScoreText(controller.Board.Score);
            RefreshSelection();
            UpdateStateText(selectedPosition == null
                ? controller.LastStatusMessage
                : $"已选中 {selectedPosition.Value}");
        }

        private void HandleBoardChanged()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            ClearAnimationTiles();

            if (controller == null || controller.LastTurnResult == null || controller.LastTurnResult.CascadePhases.Count == 0)
            {
                RefreshBoardImmediate();
                return;
            }

            animationCoroutine = StartCoroutine(PlayTurnAnimation(controller.LastTurnResult));
        }

        private IEnumerator PlayTurnAnimation(Match3TurnResult turnResult)
        {
            controller.SetInputLocked(true);
            selectedPosition = null;
            RefreshSelection();
            EnsureAnimationRoot();

            yield return PlaySwapAnimation(turnResult.SwapInfo);

            for (var index = 0; index < turnResult.CascadePhases.Count; index++)
            {
                yield return PlayCascadePhase(turnResult.CascadePhases[index]);
            }

            ClearAnimationTiles();
            animationCoroutine = null;
            controller.CompleteTurnPresentation();
            controller.SetInputLocked(false);
            RefreshBoardImmediate();
        }

        // 播放最开始两块互换位置的动画。
        private IEnumerator PlaySwapAnimation(Match3SwapInfo swapInfo)
        {
            if (swapInfo == null)
            {
                yield break;
            }

            var hiddenPositions = new HashSet<GridPosition> { swapInfo.FirstPosition, swapInfo.SecondPosition };
            SetHiddenForPositions(hiddenPositions, true);

            var firstTile = CreateAnimationTile(swapInfo.FirstTileType, swapInfo.FirstPosition);
            var secondTile = CreateAnimationTile(swapInfo.SecondTileType, swapInfo.SecondPosition);

            var firstFrom = GetAnchoredPosition(swapInfo.FirstPosition);
            var firstTo = GetAnchoredPosition(swapInfo.SecondPosition);
            var secondFrom = GetAnchoredPosition(swapInfo.SecondPosition);
            var secondTo = GetAnchoredPosition(swapInfo.FirstPosition);

            yield return AnimateTiles(
                new List<AnimatedTileState>
                {
                    new(firstTile, firstFrom, firstTo, false),
                    new(secondTile, secondFrom, secondTo, false)
                },
                swapDuration,
                false);

            ClearAnimationTiles();
            SetHiddenForPositions(hiddenPositions, false);
        }

        private IEnumerator PlayCascadePhase(Match3CascadePhase cascadePhase)
        {
            ApplySnapshot(cascadePhase.BoardBeforeClear);
            yield return PlayClearAnimation(cascadePhase);
            yield return PlayFallAnimation(cascadePhase);
        }

        // 让本轮被消除的方块做一个缩小淡出的反馈。
        private IEnumerator PlayClearAnimation(Match3CascadePhase cascadePhase)
        {
            var clearedViews = new List<Match3TileView>();
            for (var index = 0; index < cascadePhase.ClearedPositions.Count; index++)
            {
                var tileView = GetCellView(cascadePhase.ClearedPositions[index]);
                if (tileView != null)
                {
                    clearedViews.Add(tileView);
                }
            }

            if (clearedViews.Count == 0)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < clearDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = clearDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / clearDuration);
                var scale = Mathf.LerpUnclamped(1f, 0.2f, progress);
                var alpha = Mathf.LerpUnclamped(1f, 0f, progress);

                for (var index = 0; index < clearedViews.Count; index++)
                {
                    clearedViews[index].SetVisualScale(scale);
                    clearedViews[index].SetVisualAlpha(alpha);
                }

                yield return null;
            }

            var clearedArray = new GridPosition[cascadePhase.ClearedPositions.Count];
            for (var index = 0; index < cascadePhase.ClearedPositions.Count; index++)
            {
                clearedArray[index] = cascadePhase.ClearedPositions[index];
            }

            ApplySnapshot(cascadePhase.BoardBeforeClear.CreateWithClearedPositions(clearedArray));
        }

        // 在清空后的棋盘上方播放下落和补新块动画。
        private IEnumerator PlayFallAnimation(Match3CascadePhase cascadePhase)
        {
            if (cascadePhase.TileMotions.Count == 0)
            {
                ApplySnapshot(cascadePhase.BoardAfterCascade);
                yield break;
            }

            ApplySnapshot(cascadePhase.BoardAfterCascade);

            var hiddenPositions = new HashSet<GridPosition>();
            var animatedTiles = new List<AnimatedTileState>();

            for (var index = 0; index < cascadePhase.TileMotions.Count; index++)
            {
                var motion = cascadePhase.TileMotions[index];
                hiddenPositions.Add(motion.To);

                if (motion.From.Y >= 0 && motion.From.Y < controller.Board.Height)
                {
                    hiddenPositions.Add(motion.From);
                }

                var tile = CreateAnimationTile(motion.TileType, motion.From);
                animatedTiles.Add(new AnimatedTileState(
                    tile,
                    GetAnchoredPosition(motion.From),
                    GetAnchoredPosition(motion.To),
                    motion.IsSpawnedFromAbove));
            }

            SetHiddenForPositions(hiddenPositions, true);
            yield return AnimateTiles(animatedTiles, fallDuration, true);

            ClearAnimationTiles();
            ApplySnapshot(cascadePhase.BoardAfterCascade);
        }

        private IEnumerator AnimateTiles(List<AnimatedTileState> animatedTiles, float duration, bool animateScaleForSpawns)
        {
            if (animatedTiles.Count == 0)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

                for (var index = 0; index < animatedTiles.Count; index++)
                {
                    var state = animatedTiles[index];
                    var rectTransform = state.Tile != null ? state.Tile.GetComponent<RectTransform>() : null;
                    if (rectTransform == null)
                    {
                        continue;
                    }

                    rectTransform.anchoredPosition = Vector2.LerpUnclamped(state.From, state.To, easedProgress);

                    if (animateScaleForSpawns && state.IsSpawnedFromAbove)
                    {
                        var scale = Mathf.LerpUnclamped(spawnPopScale, 1f, easedProgress);
                        state.Tile.SetVisualScale(scale);
                    }
                }

                yield return null;
            }
        }

        private void ApplySnapshot(Match3BoardSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            EnsureGridLayout(snapshot.Width, snapshot.Height);
            EnsureTileViews(snapshot.Width, snapshot.Height);

            for (var y = 0; y < snapshot.Height; y++)
            {
                for (var x = 0; x < snapshot.Width; x++)
                {
                    var position = new GridPosition(x, y);
                    var tileView = GetCellView(position);
                    if (tileView != null)
                    {
                        tileView.Setup(position);
                        tileView.SetTile(snapshot.GetTile(x, y));
                        tileView.ResetVisualState();
                    }
                }
            }

            UpdateScoreText(controller.Board.Score);
        }

        private void EnsureGridLayout(int width, int height)
        {
            var layout = boardRoot.GetComponent<GridLayoutGroup>();
            if (layout == null)
            {
                layout = boardRoot.gameObject.AddComponent<GridLayoutGroup>();
            }

            layout.cellSize = cellSize;
            layout.spacing = spacing;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = width;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperCenter;

            var totalWidth = width * cellSize.x + (width - 1) * spacing.x;
            var totalHeight = height * cellSize.y + (height - 1) * spacing.y;
            boardRoot.sizeDelta = new Vector2(totalWidth, totalHeight);
        }

        // 如果当前格子数量不够，就自动按预制体补齐整张棋盘。
        private void EnsureTileViews(int width, int height)
        {
            var expectedCount = width * height;

            while (tileViews.Count > expectedCount)
            {
                var lastIndex = tileViews.Count - 1;
                var tileView = tileViews[lastIndex];
                tileViews.RemoveAt(lastIndex);

                if (tileView != null)
                {
                    Destroy(tileView.gameObject);
                }
            }

            while (tileViews.Count < expectedCount)
            {
                var tileView = Instantiate(tilePrefab, boardRoot);
                tileView.name = $"Tile_{tileViews.Count}";

                var tileButton = tileView.GetComponent<Match3TileButton>();
                if (tileButton == null)
                {
                    tileButton = tileView.gameObject.AddComponent<Match3TileButton>();
                }

                tileButton.Setup(this, tileView);
                tileViews.Add(tileView);
            }
        }

        private Match3TileView GetCellView(GridPosition logicalPosition)
        {
            if (controller == null || controller.Board == null)
            {
                return null;
            }

            var displayRow = controller.Board.Height - 1 - logicalPosition.Y;
            var flatIndex = displayRow * controller.Board.Width + logicalPosition.X;
            return flatIndex >= 0 && flatIndex < tileViews.Count ? tileViews[flatIndex] : null;
        }

        private void RefreshSelection()
        {
            for (var index = 0; index < tileViews.Count; index++)
            {
                var tileView = tileViews[index];
                tileView.SetSelected(selectedPosition.HasValue && tileView.Position.Equals(selectedPosition.Value));
            }
        }

        private void EnsureAnimationRoot()
        {
            if (boardRoot == null || animationRoot != null)
            {
                return;
            }

            var existing = boardRoot.parent != null ? boardRoot.parent.Find("AnimatedTiles") : null;
            if (existing != null)
            {
                animationRoot = existing as RectTransform;
                return;
            }

            var animationRootObject = new GameObject("AnimatedTiles", typeof(RectTransform));
            animationRoot = animationRootObject.GetComponent<RectTransform>();
            animationRoot.SetParent(boardRoot.parent, false);
            animationRoot.SetAsLastSibling();
            animationRoot.anchorMin = boardRoot.anchorMin;
            animationRoot.anchorMax = boardRoot.anchorMax;
            animationRoot.pivot = boardRoot.pivot;
            animationRoot.sizeDelta = boardRoot.sizeDelta;
            animationRoot.anchoredPosition = boardRoot.anchoredPosition;
            animationRoot.localScale = Vector3.one;
        }

        private Match3TileView CreateAnimationTile(Match3TileType tileType, GridPosition startPosition)
        {
            var tile = Instantiate(tilePrefab, animationRoot);
            tile.Setup(startPosition);
            tile.SetTile(tileType);
            tile.ResetVisualState();
            tile.SetSelected(false);

            var tileButton = tile.GetComponent<Match3TileButton>();
            if (tileButton != null)
            {
                tileButton.enabled = false;
            }

            var button = tile.GetComponent<Button>();
            if (button != null)
            {
                button.enabled = false;
            }

            var rectTransform = tile.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = cellSize;
                rectTransform.localScale = Vector3.one;
                rectTransform.anchoredPosition = GetAnchoredPosition(startPosition);
            }

            animationTiles.Add(tile);
            return tile;
        }

        private Vector2 GetAnchoredPosition(GridPosition logicalPosition)
        {
            if (controller == null || controller.Board == null)
            {
                return Vector2.zero;
            }

            var width = controller.Board.Width;
            var height = controller.Board.Height;
            var totalWidth = width * cellSize.x + (width - 1) * spacing.x;
            var totalHeight = height * cellSize.y + (height - 1) * spacing.y;

            var x = -totalWidth * 0.5f + cellSize.x * 0.5f + logicalPosition.X * (cellSize.x + spacing.x);
            var displayRow = height - 1 - logicalPosition.Y;
            var y = totalHeight * 0.5f - cellSize.y * 0.5f - displayRow * (cellSize.y + spacing.y);

            return new Vector2(x, y);
        }

        private void SetHiddenForPositions(HashSet<GridPosition> positions, bool hidden)
        {
            foreach (var position in positions)
            {
                if (position.X < 0 || position.Y < 0 || controller == null || controller.Board == null)
                {
                    continue;
                }

                if (position.X >= controller.Board.Width || position.Y >= controller.Board.Height)
                {
                    continue;
                }

                var tileView = GetCellView(position);
                if (tileView == null)
                {
                    continue;
                }

                if (hidden)
                {
                    tileView.SetVisualAlpha(0f);
                }
                else
                {
                    tileView.ResetVisualState();
                }
            }
        }

        private void ClearAnimationTiles()
        {
            for (var index = 0; index < animationTiles.Count; index++)
            {
                var tile = animationTiles[index];
                if (tile != null)
                {
                    Destroy(tile.gameObject);
                }
            }

            animationTiles.Clear();
        }

        private void UpdateScoreText(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }

        private void UpdateStateText(string content)
        {
            if (stateText != null)
            {
                stateText.text = content;
            }
        }

        // 点击重新开始按钮后，清空当前选择并开始新的一局。
        private void HandleRestartClicked()
        {
            selectedPosition = null;
            ClearAnimationTiles();

            if (controller != null)
            {
                controller.NewGame();
            }
        }

        private readonly struct AnimatedTileState
        {
            public AnimatedTileState(Match3TileView tile, Vector2 from, Vector2 to, bool isSpawnedFromAbove)
            {
                Tile = tile;
                From = from;
                To = to;
                IsSpawnedFromAbove = isSpawnedFromAbove;
            }

            public Match3TileView Tile { get; }

            public Vector2 From { get; }

            public Vector2 To { get; }

            public bool IsSpawnedFromAbove { get; }
        }
    }
}
