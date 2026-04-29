using System;

namespace AiMiniGames.Match3
{
    // 保存某一时刻整张棋盘的静态快照，供显示层逐阶段播放动画。
    public sealed class Match3BoardSnapshot
    {
        private readonly Match3TileType[,] cells;

        public Match3BoardSnapshot(int width, int height, Match3TileType[,] sourceCells)
        {
            Width = width;
            Height = height;
            cells = new Match3TileType[width, height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    cells[x, y] = sourceCells[x, y];
                }
            }
        }

        public int Width { get; }

        public int Height { get; }

        public Match3TileType GetTile(int x, int y)
        {
            return cells[x, y];
        }

        // 复制一个快照，并把指定位置清空，方便显示“消除完成后的空洞”。
        public Match3BoardSnapshot CreateWithClearedPositions(GridPosition[] clearedPositions)
        {
            var clonedCells = new Match3TileType[Width, Height];

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    clonedCells[x, y] = cells[x, y];
                }
            }

            for (var index = 0; index < clearedPositions.Length; index++)
            {
                var position = clearedPositions[index];
                clonedCells[position.X, position.Y] = Match3TileType.None;
            }

            return new Match3BoardSnapshot(Width, Height, clonedCells);
        }
    }
}
