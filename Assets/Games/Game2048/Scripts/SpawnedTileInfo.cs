namespace AiMiniGames.Game2048
{
    // 记录一次移动后新生成的数字块。
    public sealed class SpawnedTileInfo
    {
        public SpawnedTileInfo(BoardPosition position, int value)
        {
            Position = position;
            Value = value;
        }

        public BoardPosition Position { get; }

        public int Value { get; }
    }
}
