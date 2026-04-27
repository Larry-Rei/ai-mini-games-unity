using System;

namespace AiMiniGames.Match3
{
    // 用来表示棋盘中的一个格子坐标。
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        // 三消里只允许交换上下左右相邻的两个格子。
        public bool IsAdjacentTo(GridPosition other)
        {
            var deltaX = Math.Abs(X - other.X);
            var deltaY = Math.Abs(Y - other.Y);
            return deltaX + deltaY == 1;
        }

        public bool Equals(GridPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
