using UnityEngine;
using UnityEngine.UI;

namespace AiMiniGames.Match3
{
    // 把 UI Button 的点击事件转发给棋盘视图。
    [RequireComponent(typeof(Button))]
    public sealed class Match3TileButton : MonoBehaviour
    {
        [SerializeField] private Match3TileView tileView;
        [SerializeField] private Match3BoardView boardView;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(HandleClicked);
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        private void OnValidate()
        {
            if (tileView == null)
            {
                tileView = GetComponent<Match3TileView>();
            }
        }

        // 由棋盘视图在初始化时回填引用。
        public void Setup(Match3BoardView targetBoardView, Match3TileView targetTileView)
        {
            boardView = targetBoardView;
            tileView = targetTileView;
        }

        private void HandleClicked()
        {
            if (boardView != null && tileView != null)
            {
                boardView.HandleTileClicked(tileView.Position);
            }
        }
    }
}
