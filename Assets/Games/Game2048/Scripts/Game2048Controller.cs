using System;
using UnityEngine;

namespace AiMiniGames.Game2048
{
    // 挂在场景中的主控制器，负责输入、开局、重开和通知界面刷新。
    public sealed class Game2048Controller : MonoBehaviour
    {
        [SerializeField] private int boardSize = 4;
        [SerializeField] private bool logBoardAfterMoves = true;
        [SerializeField] private bool autoStartOnPlay = true;

        private BoardState boardState;
        private System.Random random;
        private bool hasLoggedWin;

        public BoardState Board => boardState;

        public event Action BoardChanged;

        // 避免在 Inspector 里填入无效的棋盘尺寸。
        private void OnValidate()
        {
            if (boardSize < 2)
            {
                boardSize = 2;
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

        // 监听键盘输入，推动棋盘状态变化。
        private void Update()
        {
            if (boardState == null)
            {
                return;
            }

            if (TryGetMoveInput(out var direction))
            {
                TryMove(direction);
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                NewGame();
            }
        }

        [ContextMenu("New Game")]
        // 开始新的一局，并通知界面重新显示。
        public void NewGame()
        {
            EnsureRuntimeState();
            boardState = new BoardState(boardSize);
            boardState.Reset(random);
            hasLoggedWin = false;

            if (logBoardAfterMoves)
            {
                Debug.Log(boardState.ToDebugString(), this);
            }

            BoardChanged?.Invoke();
        }

        // 执行一次移动；如果没有变化，就不刷新棋盘。
        public bool TryMove(MoveDirection direction)
        {
            EnsureRuntimeState();

            if (!boardState.TryMove(direction, random))
            {
                return false;
            }

            if (logBoardAfterMoves)
            {
                Debug.Log(boardState.ToDebugString(), this);
            }

            if (!hasLoggedWin && boardState.HasReachedValue(2048))
            {
                hasLoggedWin = true;
                Debug.Log("2048 reached.", this);
            }

            if (!boardState.HasAvailableMoves())
            {
                Debug.Log("No moves left. Game over.", this);
            }

            BoardChanged?.Invoke();
            return true;
        }

        // 给外部界面读取指定格子的数字使用。
        public int GetCellValue(int x, int y)
        {
            EnsureRuntimeState();
            return boardState.GetValue(x, y);
        }

        // 保证运行时用到的随机数和棋盘对象已经初始化。
        private void EnsureRuntimeState()
        {
            if (random == null)
            {
                random = new System.Random();
            }

            if (boardState == null)
            {
                boardState = new BoardState(boardSize);
            }
        }

        // 把键盘输入统一转换成移动方向。
        private static bool TryGetMoveInput(out MoveDirection direction)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                direction = MoveDirection.Up;
                return true;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                direction = MoveDirection.Down;
                return true;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                direction = MoveDirection.Left;
                return true;
            }

            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                direction = MoveDirection.Right;
                return true;
            }

            direction = default;
            return false;
        }
    }
}
