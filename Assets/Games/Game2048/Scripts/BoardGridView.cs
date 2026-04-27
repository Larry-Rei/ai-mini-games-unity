using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AiMiniGames.Game2048
{
    // 负责把 BoardState 中的数据同步到 UI 棋盘上。
    public sealed class BoardGridView : MonoBehaviour
    {
        [SerializeField] private Game2048Controller controller;
        [SerializeField] private List<BoardCellView> cellViews = new();
        [SerializeField] private Text scoreText;
        [SerializeField] private Text stateText;

        private void OnEnable()
        {
            if (controller != null)
            {
                controller.BoardChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.BoardChanged -= Refresh;
            }
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
        public void Refresh()
        {
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
    }
}
