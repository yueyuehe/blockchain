namespace QMBlockSDK.Idn
{
    public class PubliclyIdentity : Identity
    {
        /// <summary>
        /// 节点的证书
        /// </summary>
        public Certificate Certificate { get; set; }

        /// <summary>
        /// 当前节点的地址
        /// </summary>
        public string Address { get; set; }

    }
}
