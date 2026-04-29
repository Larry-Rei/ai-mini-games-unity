namespace AiMiniGames.Match3
{
    // 记录一次成功交换后的结算结果，方便界面层显示分数和连锁信息。
    public sealed class Match3TurnResult
    {
        public Match3TurnResult(
            int clearedTileCount,
            int cascadeCount,
            int scoreGained,
            Match3SwapInfo swapInfo,
            System.Collections.Generic.List<Match3CascadePhase> cascadePhases)
        {
            ClearedTileCount = clearedTileCount;
            CascadeCount = cascadeCount;
            ScoreGained = scoreGained;
            SwapInfo = swapInfo;
            CascadePhases = cascadePhases ?? new System.Collections.Generic.List<Match3CascadePhase>();
        }

        public int ClearedTileCount { get; }

        public int CascadeCount { get; }

        public int ScoreGained { get; }

        public Match3SwapInfo SwapInfo { get; }

        public System.Collections.Generic.IReadOnlyList<Match3CascadePhase> CascadePhases { get; }
    }
}
