using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AiMiniGames.Game2048
{
    // 负责把 BoardState 中的数据同步到 UI 棋盘上，并在移动时播放位移动画。
    public sealed class BoardGridView : MonoBehaviour
    {
        [SerializeField] private Game2048Controller controller;
        [SerializeField] private List<BoardCellView> cellViews = new();
        [SerializeField] private Text scoreText;
        [SerializeField] private Text stateText;
        [SerializeField] private float moveDuration = 0.12f;
        [SerializeField] private float mergePulseDuration = 0.08f;
        [SerializeField] private float mergePulseScale = 1.12f;
        [SerializeField] private float spawnPopDuration = 0.10f;
        [SerializeField] private float spawnStartScale = 0.6f;

        private readonly List<BoardCellView> animationTiles = new();
        private Coroutine animationCoroutine;
        private RectTransform boardRoot;
        private RectTransform animationRoot;

        private void Awake()
        {
            EnsureRoots();
        }

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.BoardChanged += HandleBoardChanged;
            }

            RefreshImmediate();
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.BoardChanged -= HandleBoardChanged;
            }

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            controller?.SetInputLocked(false);
            ClearAnimationTiles();
        }

        private void OnValidate()
        {
            if (controller == null)
            {
                controller = GetComponent<Game2048Controller>();
            }
        }

        [ContextMenu("Refresh Board View")]
        // 手动刷新一次界面，方便在 Inspector 中排查引用是否正确。
        public void RefreshImmediate()
        {
            EnsureRoots();

            if (controller == null || controller.Board == null)
            {
                UpdateScoreText(0);
                UpdateStateText("等待开始");
                ClearCells();
                return;
            }

            var board = controller.Board;
            var size = board.Size;

            if (cellViews.Count < size * size)
            {
                Debug.LogWarning($"BoardGridView needs at least {size * size} cell references.", this);
                UpdateScoreText(board.Score);
                UpdateStateText("格子引用不足");
                return;
            }

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var flatIndex = y * size + x;
                    var cellView = cellViews[flatIndex];
                    if (cellView != null)
                    {
                        cellView.SetValue(board.GetValue(x, y));
                    }
                }
            }

            UpdateScoreText(board.Score);
            UpdateStateText(ResolveStateText(board));
        }

        private void HandleBoardChanged()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            controller?.SetInputLocked(false);
            ClearAnimationTiles();

            if (controller == null || controller.LastMoveResult == null || !controller.LastMoveResult.Changed)
            {
                RefreshImmediate();
                return;
            }

            animationCoroutine = StartCoroutine(PlayMoveAnimation(controller.LastMoveResult));
        }

        // 先把参与移动的数字块临时复制出来播放动画，结束后再同步最终棋盘。
        private IEnumerator PlayMoveAnimation(BoardMoveResult moveResult)
        {
            EnsureRoots();

            if (controller == null || controller.Board == null || boardRoot == null || animationRoot == null)
            {
                RefreshImmediate();
                yield break;
            }

            controller.SetInputLocked(true);

            var animatedMoves = CollectAnimatedMoves(moveResult);
            HideAnimatedSources(animatedMoves);

            var tileAnimations = new List<AnimatedTileState>(animatedMoves.Count);
            for (var index = 0; index < animatedMoves.Count; index++)
            {
                var move = animatedMoves[index];
                var tile = CreateAnimationTile(move.Value, move.From);
                tileAnimations.Add(new AnimatedTileState(
                    tile,
                    GetAnchoredPosition(move.From),
                    GetAnchoredPosition(move.To)));
            }

            var elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var progress = moveDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / moveDuration);
                var easedProgress = 1f - Mathf.Pow(1f - progress, 3f);

                for (var index = 0; index < tileAnimations.Count; index++)
                {
                    var state = tileAnimations[index];
                    var rectTransform = state.Tile != null ? state.Tile.GetComponent<RectTransform>() : null;
                    if (rectTransform == null)
                    {
                        continue;
                    }

                    rectTransform.anchoredPosition = Vector2.LerpUnclamped(
                        state.From,
                        state.To,
                        easedProgress);
                }

                yield return null;
            }

            RefreshImmediate();
            ClearAnimationTiles();

            yield return PlayBoardFeedback(moveResult);

            controller.SetInputLocked(false);
            animationCoroutine = null;
        }

        private void ClearCells()
        {
            for (var index = 0; index < cellViews.Count; index++)
            {
                var cellView = cellViews[index];
                if (cellView != null)
                {
                    cellView.SetValue(0);
                }
            }
        }

        private List<TileMoveInfo> CollectAnimatedMoves(BoardMoveResult moveResult)
        {
            var animatedMoves = new List<TileMoveInfo>();

            for (var index = 0; index < moveResult.TileMoves.Count; index++)
            {
                var move = moveResult.TileMoves[index];
                var shouldAnimate = move.ParticipatesInMerge || !move.From.Equals(move.To);
                if (shouldAnimate)
                {
                    animatedMoves.Add(move);
                }
            }

            return animatedMoves;
        }

        private void HideAnimatedSources(List<TileMoveInfo> animatedMoves)
        {
            RefreshImmediate();

            var hiddenSources = new HashSet<BoardPosition>();
            for (var index = 0; index < animatedMoves.Count; index++)
            {
                var source = animatedMoves[index].From;
                if (!hiddenSources.Add(source))
                {
                    continue;
                }

                var cellView = GetCellView(source);
                if (cellView != null)
                {
                    cellView.SetValue(0);
                }
            }
        }

        private BoardCellView CreateAnimationTile(int value, BoardPosition startPosition)
        {
            var template = cellViews.Count > 0 ? cellViews[0] : null;
            if (template == null)
            {
                return null;
            }

            var tile = Instantiate(template, animationRoot);
            tile.name = $"AnimatedTile_{value}_{startPosition.X}_{startPosition.Y}";
            tile.SetValue(value);

            var rectTransform = tile.GetComponent<RectTransform>();
            var sourceRect = template.GetComponent<RectTransform>();
            if (rectTransform != null && sourceRect != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = sourceRect.pivot;
                rectTransform.sizeDelta = sourceRect.sizeDelta;
                rectTransform.localScale = Vector3.one;
                rectTransform.anchoredPosition = GetAnchoredPosition(startPosition);
            }

            animationTiles.Add(tile);
            return tile;
        }

        private BoardCellView GetCellView(BoardPosition position)
        {
            if (controller == null || controller.Board == null)
            {
                return null;
            }

            var flatIndex = position.Y * controller.Board.Size + position.X;
            return flatIndex >= 0 && flatIndex < cellViews.Count ? cellViews[flatIndex] : null;
        }

        private Vector2 GetAnchoredPosition(BoardPosition position)
        {
            var cellView = GetCellView(position);
            var cellRect = cellView != null ? cellView.GetComponent<RectTransform>() : null;
            if (cellRect == null || animationRoot == null)
            {
                return Vector2.zero;
            }

            var screenPoint = RectTransformUtility.WorldToScreenPoint(null, cellRect.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(animationRoot, screenPoint, null, out var localPoint);
            return localPoint;
        }

        private void EnsureRoots()
        {
            if (cellViews.Count == 0)
            {
                return;
            }

            if (boardRoot == null)
            {
                boardRoot = cellViews[0].transform.parent as RectTransform;
            }

            if (boardRoot == null)
            {
                return;
            }

            if (animationRoot != null)
            {
                return;
            }

            var existingRoot = boardRoot.parent != null ? boardRoot.parent.Find("AnimatedTiles") : null;
            if (existingRoot != null)
            {
                animationRoot = existingRoot as RectTransform;
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

        // 在最终棋盘落地后，给合并结果和新生成数字补一个轻量反馈。
        private IEnumerator PlayBoardFeedback(BoardMoveResult moveResult)
        {
            ResetCellScales();

            var mergedTargets = CollectMergedTargets(moveResult);
            var spawnedTarget = moveResult.SpawnedTile != null ? GetCellView(moveResult.SpawnedTile.Position) : null;

            if (spawnedTarget != null)
            {
                var spawnedRect = spawnedTarget.GetComponent<RectTransform>();
                if (spawnedRect != null)
                {
                    spawnedRect.localScale = Vector3.one * spawnStartScale;
                }
            }

            var duration = Mathf.Max(mergePulseDuration, spawnPopDuration);
            if (duration <= 0f)
            {
                ResetCellScales();
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                var mergeProgress = mergePulseDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / mergePulseDuration);
                var spawnProgress = spawnPopDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / spawnPopDuration);

                var mergeScale = EvaluatePulseScale(mergeProgress);
                var spawnScale = Mathf.LerpUnclamped(spawnStartScale, 1f, 1f - Mathf.Pow(1f - spawnProgress, 3f));

                for (var index = 0; index < mergedTargets.Count; index++)
                {
                    var mergedRect = mergedTargets[index] != null ? mergedTargets[index].GetComponent<RectTransform>() : null;
                    if (mergedRect != null)
                    {
                        mergedRect.localScale = Vector3.one * mergeScale;
                    }
                }

                if (spawnedTarget != null)
                {
                    var spawnedRect = spawnedTarget.GetComponent<RectTransform>();
                    if (spawnedRect != null)
                    {
                        spawnedRect.localScale = Vector3.one * spawnScale;
                    }
                }

                yield return null;
            }

            ResetCellScales();
        }

        private List<BoardCellView> CollectMergedTargets(BoardMoveResult moveResult)
        {
            var mergeCounts = new Dictionary<BoardPosition, int>();
            var mergedTargets = new List<BoardCellView>();

            for (var index = 0; index < moveResult.TileMoves.Count; index++)
            {
                var move = moveResult.TileMoves[index];
                if (!move.ParticipatesInMerge)
                {
                    continue;
                }

                if (!mergeCounts.TryGetValue(move.To, out var count))
                {
                    count = 0;
                }

                count++;
                mergeCounts[move.To] = count;
            }

            foreach (var pair in mergeCounts)
            {
                if (pair.Value < 2)
                {
                    continue;
                }

                var cellView = GetCellView(pair.Key);
                if (cellView != null)
                {
                    mergedTargets.Add(cellView);
                }
            }

            return mergedTargets;
        }

        private void ResetCellScales()
        {
            for (var index = 0; index < cellViews.Count; index++)
            {
                var rectTransform = cellViews[index] != null ? cellViews[index].GetComponent<RectTransform>() : null;
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.one;
                }
            }
        }

        private float EvaluatePulseScale(float progress)
        {
            if (progress <= 0.5f)
            {
                return Mathf.LerpUnclamped(1f, mergePulseScale, progress / 0.5f);
            }

            return Mathf.LerpUnclamped(mergePulseScale, 1f, (progress - 0.5f) / 0.5f);
        }

        // 根据棋盘状态给出简洁的界面提示。
        private static string ResolveStateText(BoardState board)
        {
            if (board.HasReachedValue(2048))
            {
                return "已到达 2048";
            }

            if (!board.HasAvailableMoves())
            {
                return "游戏结束";
            }

            return "进行中";
        }

        private readonly struct AnimatedTileState
        {
            public AnimatedTileState(BoardCellView tile, Vector2 from, Vector2 to)
            {
                Tile = tile;
                From = from;
                To = to;
            }

            public BoardCellView Tile { get; }

            public Vector2 From { get; }

            public Vector2 To { get; }
        }
    }
}
