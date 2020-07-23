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
        /// �����ĳ�ʱʱ��
        /// </summary>
        int HeartbeatTimeout { get; }

        /// <summary>
        /// The command timeout in milliseconds.
        /// ָ����ʱʱ��
        /// </summary>
        int CommandTimeout { get; }

        /// <summary>
        /// ���ױ���ʱ��
        /// </summary>
        int EndorseTimeOut { get; }

        int MaxTxCount { get; }

        /// <summary>
        /// ����ʱ��
        /// </summary>
        int BatchTimeout { get; }

    }
}