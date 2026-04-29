namespace AiMiniGames.Match3
{
    // 记录一次成功交换时，两块最初的起点和块类型。
    public sealed class Match3SwapInfo
    {
        public Match3SwapInfo(GridPosition firstPosition, GridPosition secondPosition, Match3TileType firstTileType, Match3TileType secondTileType)
        {
            FirstPosition = firstPosition;
            SecondPosition = secondPosition;
            FirstTileType = firstTileType;
            SecondTileType = secondTileType;
        }

        public GridPosition FirstPosition { get; }

        public GridPosition SecondPosition { get; }

        public Match3TileType FirstTileType { get; }

        public Match3TileType SecondTileType { get; }
    }
}
