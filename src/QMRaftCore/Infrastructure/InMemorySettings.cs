namespace QMRaftCore.Infrastructure
{
    public class InMemorySettings : ISettings
    {
        public InMemorySettings()
        {
            MinTimeout = 1000;
            MaxTimeout = 3500;
            HeartbeatTimeout = 50;
            CommandTimeout = 5000;
            EndorseTimeOut = 5000;

            MaxTxCount = 100;
            BatchTimeout = 2000;
        }

        public int MaxTxCount { get; private set; }

        /// <summary>
        /// ³ö¿éÊ±¼ä
        /// </summary>
        public int BatchTimeout { get; private set; }

        public int MinTimeout { get; private set; }
        public int MaxTimeout { get; private set; }
        public int HeartbeatTimeout { get; private set; }
        public int CommandTimeout { get; private set; }
        public int EndorseTimeOut { get; private set; }
    }
}