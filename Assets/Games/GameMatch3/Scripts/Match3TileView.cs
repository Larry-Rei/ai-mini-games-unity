using UnityEngine;
using UnityEngine.UI;

namespace AiMiniGames.Match3
{
    // 负责显示单个三消块的颜色、文字和选中状态。
    public sealed class Match3TileView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Text valueText;
        [SerializeField] private Outline selectionOutline;

        [Header("Colors")]
        [SerializeField] private Color noneColor = new(0.82f, 0.82f, 0.82f);
        [SerializeField] private Color redColor = new(0.89f, 0.33f, 0.29f);
        [SerializeField] private Color blueColor = new(0.28f, 0.53f, 0.90f);
        [SerializeField] private Color greenColor = new(0.34f, 0.74f, 0.40f);
        [SerializeField] private Color yellowColor = new(0.95f, 0.81f, 0.30f);
        [SerializeField] private Color purpleColor = new(0.65f, 0.41f, 0.87f);
        [SerializeField] private Color orangeColor = new(0.95f, 0.56f, 0.23f);

        private GridPosition position;

        public GridPosition Position => position;

        // 初始化方块所属坐标，方便点击时把自己告诉棋盘视图。
        public void Setup(GridPosition gridPosition)
        {
            position = gridPosition;
            gameObject.name = $"Tile_{gridPosition.X}_{gridPosition.Y}";
        }

        // 根据块类型刷新颜色和文本。
        public void SetTile(Match3TileType tileType)
        {
            if (background != null)
            {
                background.color = ResolveColor(tileType);
            }

            if (valueText != null)
            {
                valueText.text = ResolveLabel(tileType);
            }
        }

        // 用一个边框来表示当前是否被选中。
        public void SetSelected(bool isSelected)
        {
            if (selectionOutline != null)
            {
                selectionOutline.enabled = isSelected;
            }
        }

        private void OnValidate()
        {
            if (background == null)
            {
                background = GetComponent<Image>();
            }

            if (selectionOutline == null)
            {
                selectionOutline = GetComponent<Outline>();
            }
        }

        private Color ResolveColor(Match3TileType tileType)
        {
            return tileType switch
            {
                Match3TileType.Red => redColor,
                Match3TileType.Blue => blueColor,
                Match3TileType.Green => greenColor,
                Match3TileType.Yellow => yellowColor,
                Match3TileType.Purple => purpleColor,
                Match3TileType.Orange => orangeColor,
                _ => noneColor
            };
        }

        private static string ResolveLabel(Match3TileType tileType)
        {
            return tileType switch
            {
                Match3TileType.Red => "R",
                Match3TileType.Blue => "B",
                Match3TileType.Green => "G",
                Match3TileType.Yellow => "Y",
                Match3TileType.Purple => "P",
                Match3TileType.Orange => "O",
                _ => string.Empty
            };
        }
    }
}
