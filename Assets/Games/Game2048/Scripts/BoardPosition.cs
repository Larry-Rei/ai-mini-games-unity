using System;

namespace AiMiniGames.Game2048
{
    // 表示 2048 棋盘中的一个坐标。
    public readonly struct BoardPosition : IEquatable<BoardPosition>
    {
        public BoardPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }

        public int Y { get; }

        public bool Equals(BoardPosition other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is BoardPosition other && Equals(other);
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
