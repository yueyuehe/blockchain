namespace QMRaftCore.Infrastructure
{
    public interface ISettings
    {
        /// <summary>
        /// The minimum follower timeout in milliseconds.
        /// </summary>
        int MinTimeout { get; }

        /// <summary>
        /// The maximum follower timeout in milliseconds.
        /// </summary>
        int MaxTimeout { get; }

        /// <summary>
        /// The leader heartbeat timeout in milliseconds.
        /// 心跳的超时时间
        /// </summary>
        int HeartbeatTimeout { get; }

        /// <summary>
        /// The command timeout in milliseconds.
        /// 指定超时时间
        /// </summary>
        int CommandTimeout { get; }

        /// <summary>
        /// 交易背书时间
        /// </summary>
        int EndorseTimeOut { get; }

        int MaxTxCount { get; }

        /// <summary>
        /// 出块时间
        /// </summary>
        int BatchTimeout { get; }

    }
}