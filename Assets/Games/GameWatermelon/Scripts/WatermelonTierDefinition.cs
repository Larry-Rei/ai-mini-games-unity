using System;
using UnityEngine;

namespace AiMiniGames.GameWatermelon
{
    // 描述某一级水果的基础信息：名字、颜色、尺寸和得分。
    [Serializable]
    public sealed class WatermelonTierDefinition
    {
        [SerializeField] private string displayName = "Fruit";
        [SerializeField] private string shortLabel = "";
        [SerializeField] private Color color = Color.white;
        [SerializeField] private float diameter = 1f;
        [SerializeField] private int score = 10;

        public string DisplayName => displayName;

        // 球体中央显示的简短标记，留空时会自动取名字的第一个字。
        public string ShortLabel => shortLabel;

        public Color Color => color;

        public float Diameter => diameter;

        public int Score => score;
    }
}
