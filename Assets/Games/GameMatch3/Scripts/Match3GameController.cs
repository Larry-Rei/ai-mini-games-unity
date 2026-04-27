using System;
using UnityEngine;

namespace AiMiniGames.Match3
{
    // 挂在场景中的三消控制器，负责开局、调试交换和通知界面刷新。
    public sealed class Match3GameController : MonoBehaviour
    {
        [SerializeField] private int boardWidth = 8;
        [SerializeField] private int boardHeight = 8;
        [SerializeField] private int tileTypeCount = 6;
        [SerializeField] private bool autoStartOnPlay = true;
        [SerializeField] private bool logBoardChanges = true;

        [Header("Debug Swap")]
        [SerializeField] private Vector2Int debugSwapA = new(0, 0);
        [SerializeField] private Vector2Int debugSwapB = new(1, 0);

        private Match3BoardState boardState;
        private System.Random random;

        public Match3BoardState Board => boardState;

        public event Action BoardChanged;

        // 对 Inspector 输入做基础约束，避免填出无效尺寸。
        private void OnValidate()
        {
            if (boardWidth < 3)
            {
                boardWidth = 3;
            }

            if (boardHeight < 3)
            {
                boardHeight = 3;
            }

            if (tileTypeCount < 3)
            {
                tileTypeCount = 3;
            }

            if (tileTypeCount > 6)
            {
                tileTypeCount = 6;
            }
        }

        private void Awake()
        {
            EnsureRuntimeState();
        }

        private void Start()
        {
            if (autoStartOnPlay)
            {
                NewGame();
            }
        }

        [ContextMenu("New Game")]
        // 开始一局新的三消，并把当前棋盘状态打印出来。
        public void NewGame()
        {
            EnsureRuntimeState();
            boardState = new Match3BoardState(boardWidth, boardHeight, tileTypeCount);
            boardState.Reset(random);

            LogBoard("Match-3 new game created.");
            BoardChanged?.Invoke();
        }

        [ContextMenu("Try Debug Swap")]
        // 使用 Inspector 中的两个测试坐标，尝试做一次交换。
        public void TryDebugSwap()
        {
            TrySwap(debugSwapA, debugSwapB);
        }

        // 给后续点击交互层调用的交换入口。
        public bool TrySwap(Vector2Int first, Vector2Int second)
        {
            EnsureRuntimeState();

            var swapped = boardState.TrySwap(
                new GridPosition(first.x, first.y),
                new GridPosition(second.x, second.y),
                random,
                out var result);

            if (!swapped)
            {
                if (logBoardChanges)
                {
                    Debug.Log($"Swap failed: {first} <-> {second}", this);
                }

                return false;
            }

            if (logBoardChanges)
            {
                Debug.Log(
                    $"Swap success: {first} <-> {second}\n" +
                    $"Cleared: {result.ClearedTileCount}, Cascades: {result.CascadeCount}, Score Gained: {result.ScoreGained}\n" +
                    boardState.ToDebugString(),
                    this);
            }

            BoardChanged?.Invoke();
            return true;
        }

        // 给界面层读取指定格子的块类型使用。
        public Match3TileType GetTile(int x, int y)
        {
            EnsureRuntimeState();
            return boardState.GetTile(x, y);
        }

        // 保证运行时依赖对象已经初始化完成。
        private void EnsureRuntimeState()
        {
            if (random == null)
            {
                random = new System.Random();
            }

            if (boardState == null)
            {
                boardState = new Match3BoardState(boardWidth, boardHeight, tileTypeCount);
            }
        }

        private void LogBoard(string prefix)
        {
            if (!logBoardChanges || boardState == null)
            {
                return;
            }

            Debug.Log($"{prefix}\n{boardState.ToDebugString()}", this);
        }
    }
}
