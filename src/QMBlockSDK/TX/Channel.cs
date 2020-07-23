namespace QMBlockSDK.TX
{
    public class Channel
    {
        public Channel()
        {
            Chaincode = new Chaincode();
        }
        public string ChannelId { get; set; }

        public Chaincode Chaincode { get; set; }

    }
}
