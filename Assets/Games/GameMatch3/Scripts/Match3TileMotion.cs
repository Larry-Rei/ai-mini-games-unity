namespace AiMiniGames.Match3
{
    // 描述单个方块从起点移动到终点的过程，可用于下落和补新块动画。
    public sealed class Match3TileMotion
    {
        public Match3TileMotion(Match3TileType tileType, GridPosition from, GridPosition to, bool isSpawnedFromAbove)
        {
            TileType = tileType;
            From = from;
            To = to;
            IsSpawnedFromAbove = isSpawnedFromAbove;
        }

        public Match3TileType TileType { get; }

        public GridPosition From { get; }

        public GridPosition To { get; }

        public bool IsSpawnedFromAbove { get; }
    }
}
