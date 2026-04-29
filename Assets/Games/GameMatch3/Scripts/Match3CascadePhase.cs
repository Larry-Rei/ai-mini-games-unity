using System.Collections.Generic;

namespace AiMiniGames.Match3
{
    // 记录单次连锁中的一个阶段：当前盘面、被消除位置、以及下落/补新块的运动信息。
    public sealed class Match3CascadePhase
    {
        public Match3CascadePhase(
            Match3BoardSnapshot boardBeforeClear,
            Match3BoardSnapshot boardAfterCascade,
            List<GridPosition> clearedPositions,
            List<Match3TileMotion> tileMotions)
        {
            BoardBeforeClear = boardBeforeClear;
            BoardAfterCascade = boardAfterCascade;
            ClearedPositions = clearedPositions ?? new List<GridPosition>();
            TileMotions = tileMotions ?? new List<Match3TileMotion>();
        }

        public Match3BoardSnapshot BoardBeforeClear { get; }

        public Match3BoardSnapshot BoardAfterCascade { get; }

        public IReadOnlyList<GridPosition> ClearedPositions { get; }

        public IReadOnlyList<Match3TileMotion> TileMotions { get; }
    }
}
