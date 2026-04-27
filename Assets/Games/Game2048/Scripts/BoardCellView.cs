using UnityEngine;
using UnityEngine.UI;

namespace AiMiniGames.Game2048
{
    // 负责显示单个格子的数字和底色。
    public sealed class BoardCellView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Text valueText;

        [Header("Colors")]
        [SerializeField] private Color emptyColor = new(0.78f, 0.73f, 0.68f);
        [SerializeField] private Color lowValueColor = new(0.93f, 0.89f, 0.85f);
        [SerializeField] private Color mediumValueColor = new(0.95f, 0.69f, 0.47f);
        [SerializeField] private Color highValueColor = new(0.93f, 0.49f, 0.35f);
        [SerializeField] private Color maxValueColor = new(0.93f, 0.81f, 0.45f);
        [SerializeField] private Color darkTextColor = new(0.47f, 0.43f, 0.40f);
        [SerializeField] private Color lightTextColor = Color.white;

        // 根据当前数字刷新格子的视觉表现。
        public void SetValue(int value)
        {
            if (valueText != null)
            {
                valueText.text = value > 0 ? value.ToString() : string.Empty;
                valueText.color = value >= 8 ? lightTextColor : darkTextColor;
            }

            if (background != null)
            {
                background.color = ResolveBackgroundColor(value);
            }
        }

        private Color ResolveBackgroundColor(int value)
        {
            if (value <= 0)
            {
                return emptyColor;
            }

            if (value <= 4)
            {
                return lowValueColor;
            }

            if (value <= 64)
            {
                return mediumValueColor;
            }

            if (value <= 512)
            {
                return highValueColor;
            }

            return maxValueColor;
        }
    }
}
