namespace AiMiniGames.Game2048
{
    // 描述单个数字块从起点移动到终点的过程。
    public sealed class TileMoveInfo
    {
        public TileMoveInfo(BoardPosition from, BoardPosition to, int value, bool participatesInMerge)
        {
            From = from;
            To = to;
            Value = value;
            ParticipatesInMerge = participatesInMerge;
        }

        public BoardPosition From { get; }

        public BoardPosition To { get; }

        public int Value { get; }

        // 为 true 时表示这个块会参与合并，即使它自己不发生位移也需要参与动画。
        public bool ParticipatesInMerge { get; }
    }
}
