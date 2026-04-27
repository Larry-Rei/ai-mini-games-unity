using System;
using System.Collections.Generic;
using System.Text;

namespace AiMiniGames.Game2048
{
    // 负责保存 2048 的纯数据状态，不直接依赖 Unity 场景对象。
    public sealed class BoardState
    {
        private readonly int[,] cells;
        private readonly int[] lineBuffer;
        private readonly int[] mergedBuffer;

        public BoardState(int size)
        {
            if (size < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Board size must be at least 2.");
            }

            Size = size;
            cells = new int[size, size];
            lineBuffer = new int[size];
            mergedBuffer = new int[size];
        }

        public int Size { get; }

        public int Score { get; private set; }

        // 清空棋盘并生成两个初始数字。
        public void Reset(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            Array.Clear(cells, 0, cells.Length);
            Score = 0;

            SpawnRandomTile(random);
            SpawnRandomTile(random);
        }

        // 读取指定格子的数值，供界面层显示使用。
        public int GetValue(int x, int y)
        {
            return cells[x, y];
        }

        // 尝试朝某个方向移动；只有棋盘真的发生变化时才会生成新数字。
        public bool TryMove(MoveDirection direction, Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            var changed = false;

            for (var lineIndex = 0; lineIndex < Size; lineIndex++)
            {
                ReadLine(direction, lineIndex, lineBuffer);
                MergeLine(lineBuffer, mergedBuffer, out var lineChanged, out var gainedScore);

                if (!lineChanged)
                {
                    continue;
                }

                changed = true;
                Score += gainedScore;
                WriteLine(direction, lineIndex, mergedBuffer);
            }

            if (changed)
            {
                SpawnRandomTile(random);
            }

            return changed;
        }

        // 检查是否还有空位或可继续合并的相邻数字。
        public bool HasAvailableMoves()
        {
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    if (cells[x, y] == 0)
                    {
                        return true;
                    }

                    if (x + 1 < Size && cells[x, y] == cells[x + 1, y])
                    {
                        return true;
                    }

                    if (y + 1 < Size && cells[x, y] == cells[x, y + 1])
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 检查棋盘里是否已经出现目标数字，例如 2048。
        public bool HasReachedValue(int targetValue)
        {
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    if (cells[x, y] >= targetValue)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 以文本形式输出棋盘，方便先在 Console 中验证逻辑。
        public string ToDebugString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Score: {Score}");

            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    builder.Append(cells[x, y].ToString().PadLeft(5));
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        // 从全部空格中随机挑一个位置生成新数字：90% 是 2，10% 是 4。
        private void SpawnRandomTile(Random random)
        {
            var emptyCells = new List<CellPosition>();

            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    if (cells[x, y] == 0)
                    {
                        emptyCells.Add(new CellPosition(x, y));
                    }
                }
            }

            if (emptyCells.Count == 0)
            {
                return;
            }

            var index = random.Next(emptyCells.Count);
            var spawnValue = random.NextDouble() < 0.9 ? 2 : 4;
            var spawnPosition = emptyCells[index];
            cells[spawnPosition.X, spawnPosition.Y] = spawnValue;
        }

        // 按移动方向读取一整行或一整列到缓存数组中。
        private void ReadLine(MoveDirection direction, int lineIndex, int[] destination)
        {
            for (var offset = 0; offset < Size; offset++)
            {
                var position = ResolvePosition(direction, lineIndex, offset);
                destination[offset] = cells[position.X, position.Y];
            }
        }

        // 把处理后的结果写回棋盘原来的位置。
        private void WriteLine(MoveDirection direction, int lineIndex, int[] source)
        {
            for (var offset = 0; offset < Size; offset++)
            {
                var position = ResolvePosition(direction, lineIndex, offset);
                cells[position.X, position.Y] = source[offset];
            }
        }

        // 对单行进行“压缩 + 合并”：
        // 相同数字每次只能两两合并一次，结果写入 destination。
        private void MergeLine(int[] source, int[] destination, out bool changed, out int gainedScore)
        {
            Array.Clear(destination, 0, destination.Length);

            gainedScore = 0;
            var writeIndex = 0;
            var pendingValue = 0;

            for (var index = 0; index < source.Length; index++)
            {
                var value = source[index];
                if (value == 0)
                {
                    continue;
                }

                if (pendingValue == 0)
                {
                    pendingValue = value;
                    continue;
                }

                if (pendingValue == value)
                {
                    var mergedValue = pendingValue * 2;
                    destination[writeIndex++] = mergedValue;
                    gainedScore += mergedValue;
                    pendingValue = 0;
                    continue;
                }

                destination[writeIndex++] = pendingValue;
                pendingValue = value;
            }

            if (pendingValue != 0)
            {
                destination[writeIndex] = pendingValue;
            }

            changed = false;

            for (var index = 0; index < source.Length; index++)
            {
                if (source[index] == destination[index])
                {
                    continue;
                }

                changed = true;
                break;
            }
        }

        // 根据方向把逻辑上的偏移量映射成棋盘中的真实坐标。
        private CellPosition ResolvePosition(MoveDirection direction, int lineIndex, int offset)
        {
            switch (direction)
            {
                case MoveDirection.Left:
                    return new CellPosition(offset, lineIndex);
                case MoveDirection.Right:
                    return new CellPosition(Size - 1 - offset, lineIndex);
                case MoveDirection.Up:
                    return new CellPosition(lineIndex, offset);
                case MoveDirection.Down:
                    return new CellPosition(lineIndex, Size - 1 - offset);
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        // 轻量坐标结构，用来记录格子位置。
        private readonly struct CellPosition
        {
            public CellPosition(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }

            public int Y { get; }
        }
    }
}
