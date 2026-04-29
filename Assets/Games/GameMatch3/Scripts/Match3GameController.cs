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
        [SerializeField] private bool autoShuffleWhenNoMoves = true;

        [Header("Debug Swap")]
        [SerializeField] private Vector2Int debugSwapA = new(0, 0);
        [SerializeField] private Vector2Int debugSwapB = new(1, 0);

        private Match3BoardState boardState;
        private System.Random random;
        private bool inputLocked;
        private bool pendingShuffle;

        public Match3BoardState Board => boardState;

        public string LastStatusMessage { get; private set; } = "等待开始";

        public Match3TurnResult LastTurnResult { get; private set; }

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
            pendingShuffle = false;
            inputLocked = false;
            LastTurnResult = null;
            LastStatusMessage = "新的一局已开始";

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

            if (inputLocked)
            {
                return false;
            }

            var swapped = boardState.TrySwap(
                new GridPosition(first.x, first.y),
                new GridPosition(second.x, second.y),
                random,
                out var result);

            if (!swapped)
            {
                LastStatusMessage = "交换失败：需要相邻且能形成消除";

                if (logBoardChanges)
                {
                    Debug.Log($"Swap failed: {first} <-> {second}", this);
                }

                BoardChanged?.Invoke();
                return false;
            }

            LastTurnResult = result;
            LastStatusMessage = $"交换成功，消除了 {result.ClearedTileCount} 个方块";

            if (autoShuffleWhenNoMoves && !boardState.HasAnyValidMove())
            {
                pendingShuffle = true;
                LastStatusMessage = "当前无合法交换，动画后将自动洗牌";
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

        // 给显示层判断这一回合动画播完后，是否还需要补一个自动洗牌。
        public bool HasPendingShuffle()
        {
            return pendingShuffle;
        }

        // 由显示层在动画播放期间加锁，避免玩家连续点击导致界面和数据脱节。
        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
        }

        // 一次交换的整套表现播放结束后，由显示层通知控制器继续处理尾部逻辑。
        public void CompleteTurnPresentation()
        {
            LastTurnResult = null;

            if (!pendingShuffle)
            {
                return;
            }

            pendingShuffle = false;
            ShuffleBoardUntilPlayable();
            LastStatusMessage = "当前无合法交换，已自动洗牌";
            BoardChanged?.Invoke();
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

        // 当棋盘进入死局时，重新随机生成一盘“无初始消除且至少有一步可走”的布局。
        private void ShuffleBoardUntilPlayable()
        {
            boardState.Reset(random);

            if (logBoardChanges)
            {
                Debug.Log($"Board shuffled because there were no valid moves.\n{boardState.ToDebugString()}", this);
            }
        }
    }
}
