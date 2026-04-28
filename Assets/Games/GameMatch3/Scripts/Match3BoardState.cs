using System;
using System.Collections.Generic;
using System.Text;

namespace AiMiniGames.Match3
{
    // 负责维护三消棋盘的纯数据状态，不依赖 Unity 场景对象。
    public sealed class Match3BoardState
    {
        private static readonly Match3TileType[] AllPlayableTiles =
        {
            Match3TileType.Red,
            Match3TileType.Blue,
            Match3TileType.Green,
            Match3TileType.Yellow,
            Match3TileType.Purple,
            Match3TileType.Orange
        };

        private readonly Match3TileType[,] cells;
        private readonly Match3TileType[] playableTiles;

        public Match3BoardState(int width, int height, int tileTypeCount)
        {
            if (width < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "Board width must be at least 3.");
            }

            if (height < 3)
            {
                throw new ArgumentOutOfRangeException(nameof(height), "Board height must be at least 3.");
            }

            if (tileTypeCount < 3 || tileTypeCount > AllPlayableTiles.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tileTypeCount),
                    $"Tile type count must be between 3 and {AllPlayableTiles.Length}.");
            }

            Width = width;
            Height = height;
            cells = new Match3TileType[width, height];
            playableTiles = new Match3TileType[tileTypeCount];

            for (var index = 0; index < tileTypeCount; index++)
            {
                playableTiles[index] = AllPlayableTiles[index];
            }
        }

        public int Width { get; }

        public int Height { get; }

        public int Score { get; private set; }

        public int SuccessfulMoves { get; private set; }

        // 检查当前棋盘上是否还存在至少一步合法交换。
        public bool HasAnyValidMove()
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var current = new GridPosition(x, y);

                    if (x + 1 < Width && WouldCreateMatchAfterSwap(current, new GridPosition(x + 1, y)))
                    {
                        return true;
                    }

                    if (y + 1 < Height && WouldCreateMatchAfterSwap(current, new GridPosition(x, y + 1)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // 重置棋盘，并生成一个开局时没有三连的初始布局。
        public void Reset(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            Array.Clear(cells, 0, cells.Length);
            Score = 0;
            SuccessfulMoves = 0;

            FillInitialBoard(random);
        }

        // 读取指定位置的块类型，供界面层显示使用。
        public Match3TileType GetTile(int x, int y)
        {
            ValidatePosition(new GridPosition(x, y));
            return cells[x, y];
        }

        // 尝试交换两个相邻格子；只有形成消除时交换才算成功。
        public bool TrySwap(GridPosition first, GridPosition second, Random random, out Match3TurnResult result)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            ValidatePosition(first);
            ValidatePosition(second);

            if (!first.IsAdjacentTo(second))
            {
                result = null;
                return false;
            }

            SwapTiles(first, second);

            var matches = FindAllMatches();
            if (matches.Count == 0)
            {
                SwapTiles(first, second);
                result = null;
                return false;
            }

            SuccessfulMoves++;

            var totalCleared = 0;
            var cascadeCount = 0;
            var scoreGained = 0;

            while (matches.Count > 0)
            {
                cascadeCount++;

                var clearedThisCascade = ClearMatches(matches);
                totalCleared += clearedThisCascade;

                // 简单的计分规则：基础 10 分，连锁越高倍率越高。
                var cascadeScore = clearedThisCascade * 10 * cascadeCount;
                scoreGained += cascadeScore;

                CollapseColumns();
                RefillBoard(random);

                matches = FindAllMatches();
            }

            Score += scoreGained;
            result = new Match3TurnResult(totalCleared, cascadeCount, scoreGained);
            return true;
        }

        // 以文本形式输出当前棋盘，方便先在 Console 中验证规则逻辑。
        public string ToDebugString()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Score: {Score}  Moves: {SuccessfulMoves}");

            for (var y = Height - 1; y >= 0; y--)
            {
                for (var x = 0; x < Width; x++)
                {
                    builder.Append(ShortName(cells[x, y]).PadLeft(3));
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        // 生成开局棋盘时，主动避开横向或纵向的初始三连。
        private void FillInitialBoard(Random random)
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    cells[x, y] = GetRandomTileForInitialFill(x, y, random);
                }
            }

            // 标准三消开局应保证“无初始三连”且“至少有一步可走”。
            while (!HasAnyValidMove())
            {
                RebuildBoardWithoutStartingMatches(random);
            }
        }

        private void RebuildBoardWithoutStartingMatches(Random random)
        {
            Array.Clear(cells, 0, cells.Length);

            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    cells[x, y] = GetRandomTileForInitialFill(x, y, random);
                }
            }
        }

        private Match3TileType GetRandomTileForInitialFill(int x, int y, Random random)
        {
            var candidates = new List<Match3TileType>(playableTiles.Length);

            for (var index = 0; index < playableTiles.Length; index++)
            {
                var candidate = playableTiles[index];

                var wouldMakeHorizontalMatch =
                    x >= 2 &&
                    cells[x - 1, y] == candidate &&
                    cells[x - 2, y] == candidate;

                var wouldMakeVerticalMatch =
                    y >= 2 &&
                    cells[x, y - 1] == candidate &&
                    cells[x, y - 2] == candidate;

                if (!wouldMakeHorizontalMatch && !wouldMakeVerticalMatch)
                {
                    candidates.Add(candidate);
                }
            }

            if (candidates.Count == 0)
            {
                return playableTiles[random.Next(playableTiles.Length)];
            }

            return candidates[random.Next(candidates.Count)];
        }

        // 扫描整张棋盘，把所有横向和纵向三连及以上的位置找出来。
        private HashSet<GridPosition> FindAllMatches()
        {
            var matches = new HashSet<GridPosition>();

            for (var y = 0; y < Height; y++)
            {
                var x = 0;
                while (x < Width)
                {
                    var tile = cells[x, y];
                    if (tile == Match3TileType.None)
                    {
                        x++;
                        continue;
                    }

                    var runEnd = x + 1;
                    while (runEnd < Width && cells[runEnd, y] == tile)
                    {
                        runEnd++;
                    }

                    if (runEnd - x >= 3)
                    {
                        for (var matchX = x; matchX < runEnd; matchX++)
                        {
                            matches.Add(new GridPosition(matchX, y));
                        }
                    }

                    x = runEnd;
                }
            }

            for (var x = 0; x < Width; x++)
            {
                var y = 0;
                while (y < Height)
                {
                    var tile = cells[x, y];
                    if (tile == Match3TileType.None)
                    {
                        y++;
                        continue;
                    }

                    var runEnd = y + 1;
                    while (runEnd < Height && cells[x, runEnd] == tile)
                    {
                        runEnd++;
                    }

                    if (runEnd - y >= 3)
                    {
                        for (var matchY = y; matchY < runEnd; matchY++)
                        {
                            matches.Add(new GridPosition(x, matchY));
                        }
                    }

                    y = runEnd;
                }
            }

            return matches;
        }

        // 清除所有匹配到的格子，并返回本轮清掉的总数量。
        private int ClearMatches(HashSet<GridPosition> matches)
        {
            foreach (var position in matches)
            {
                cells[position.X, position.Y] = Match3TileType.None;
            }

            return matches.Count;
        }

        // 把每一列中还存在的方块向下压紧，模拟重力下落。
        private void CollapseColumns()
        {
            for (var x = 0; x < Width; x++)
            {
                var writeY = 0;

                for (var readY = 0; readY < Height; readY++)
                {
                    var tile = cells[x, readY];
                    if (tile == Match3TileType.None)
                    {
                        continue;
                    }

                    if (writeY != readY)
                    {
                        cells[x, writeY] = tile;
                        cells[x, readY] = Match3TileType.None;
                    }

                    writeY++;
                }
            }
        }

        // 给顶部出现的空位补入新方块，可能由此触发新的连锁消除。
        private void RefillBoard(Random random)
        {
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    if (cells[x, y] == Match3TileType.None)
                    {
                        cells[x, y] = playableTiles[random.Next(playableTiles.Length)];
                    }
                }
            }
        }

        private void SwapTiles(GridPosition first, GridPosition second)
        {
            var temp = cells[first.X, first.Y];
            cells[first.X, first.Y] = cells[second.X, second.Y];
            cells[second.X, second.Y] = temp;
        }

        // 用“临时交换 -> 检查是否产生匹配 -> 换回去”的方式判断这一步是否合法。
        private bool WouldCreateMatchAfterSwap(GridPosition first, GridPosition second)
        {
            SwapTiles(first, second);
            var wouldMatch = FindAllMatches().Count > 0;
            SwapTiles(first, second);
            return wouldMatch;
        }

        private void ValidatePosition(GridPosition position)
        {
            if (position.X < 0 || position.X >= Width || position.Y < 0 || position.Y >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(position), $"Invalid board position: {position}");
            }
        }

        private static string ShortName(Match3TileType tileType)
        {
            return tileType switch
            {
                Match3TileType.Red => "R",
                Match3TileType.Blue => "B",
                Match3TileType.Green => "G",
                Match3TileType.Yellow => "Y",
                Match3TileType.Purple => "P",
                Match3TileType.Orange => "O",
                _ => "."
            };
        }
    }
}
