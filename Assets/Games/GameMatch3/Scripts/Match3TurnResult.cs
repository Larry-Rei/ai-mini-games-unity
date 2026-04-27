namespace AiMiniGames.Match3
{
    // 记录一次成功交换后的结算结果，方便界面层显示分数和连锁信息。
    public sealed class Match3TurnResult
    {
        public Match3TurnResult(int clearedTileCount, int cascadeCount, int scoreGained)
        {
            ClearedTileCount = clearedTileCount;
            CascadeCount = cascadeCount;
            ScoreGained = scoreGained;
        }

        public int ClearedTileCount { get; }

        public int CascadeCount { get; }

        public int ScoreGained { get; }
    }
}
