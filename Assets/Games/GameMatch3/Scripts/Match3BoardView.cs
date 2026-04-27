using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AiMiniGames.Match3
{
    // 负责生成三消棋盘 UI，并处理简单的点击交换交互。
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

        private readonly List<Match3TileView> tileViews = new();
        private GridPosition? selectedPosition;

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.BoardChanged += RefreshBoard;
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
                controller.BoardChanged -= RefreshBoard;
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }
        }

        private void Start()
        {
            RefreshBoard();
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
            if (controller == null || controller.Board == null)
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
                UpdateStateText("交换失败：需要相邻且能形成消除");
                RefreshSelection();
                RefreshBoard();
                return;
            }

            UpdateStateText($"交换成功：{first} -> {position}");
            RefreshSelection();
            RefreshBoard();
        }

        [ContextMenu("Refresh Board")]
        // 根据当前棋盘状态重新生成或刷新整个 UI。
        public void RefreshBoard()
        {
            if (controller == null || controller.Board == null || boardRoot == null || tilePrefab == null)
            {
                UpdateStateText("缺少棋盘引用");
                return;
            }

            EnsureGridLayout(controller.Board.Width, controller.Board.Height);
            EnsureTileViews(controller.Board.Width, controller.Board.Height);

            for (var y = 0; y < controller.Board.Height; y++)
            {
                for (var x = 0; x < controller.Board.Width; x++)
                {
                    var flatIndex = y * controller.Board.Width + x;
                    // 逻辑层把 y=0 视为最底部一行，而 GridLayoutGroup 的第一行显示在最上方。
                    // 这里把显示行和逻辑行做一次翻转，保证“上方掉落到下方”的视觉是正确的。
                    var logicalY = controller.Board.Height - 1 - y;
                    var tileView = tileViews[flatIndex];
                    tileView.Setup(new GridPosition(x, logicalY));
                    tileView.SetTile(controller.GetTile(x, logicalY));
                }
            }

            UpdateScoreText(controller.Board.Score);
            RefreshSelection();

            if (selectedPosition == null && stateText != null && string.IsNullOrWhiteSpace(stateText.text))
            {
                UpdateStateText("请选择两个相邻方块进行交换");
            }
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

            // 如果棋盘尺寸被调小，先把多余的旧格子清掉，避免界面残留。
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

        private void RefreshSelection()
        {
            for (var index = 0; index < tileViews.Count; index++)
            {
                var tileView = tileViews[index];
                tileView.SetSelected(selectedPosition.HasValue && tileView.Position.Equals(selectedPosition.Value));
            }
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

            if (controller != null)
            {
                controller.NewGame();
            }

            UpdateStateText("新的一局已开始");
        }
    }
}
