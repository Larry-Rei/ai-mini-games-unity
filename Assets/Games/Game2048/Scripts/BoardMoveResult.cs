using System.Collections.Generic;

namespace AiMiniGames.Game2048
{
    // 保存一次移动的完整结果，供显示层播放位移动画。
    public sealed class BoardMoveResult
    {
        public BoardMoveResult(bool changed, int scoreGained, List<TileMoveInfo> tileMoves, SpawnedTileInfo spawnedTile)
        {
            Changed = changed;
            ScoreGained = scoreGained;
            TileMoves = tileMoves ?? new List<TileMoveInfo>();
            SpawnedTile = spawnedTile;
        }

        public bool Changed { get; }

        public int ScoreGained { get; }

        public IReadOnlyList<TileMoveInfo> TileMoves { get; }

        public SpawnedTileInfo SpawnedTile { get; }
    }
}
