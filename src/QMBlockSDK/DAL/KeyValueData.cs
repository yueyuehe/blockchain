namespace QMBlockSDK.DAL
{
    public class KeyValueData
    {
        public string Id { get; set; }
        public string ChannelId { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Version { get; set; }
        public bool Deleted { get; set; }
    }
}
