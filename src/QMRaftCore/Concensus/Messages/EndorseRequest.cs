using QMBlockSDK.TX;

namespace QMRaftCore.Concensus.Messages
{
    public class EndorseRequest
    {
        public string ChannelId { get; set; }
        public TxRequest Request { get; set; }
    }
}
