namespace QMBlockSDK.Config
{
    public class ChaincodeConfig
    {
        public ChaincodeConfig()
        {
            Policy = new Policy();
        }

        /// <summary>
        /// 类名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 命名空间 和 生成的DLL文件名称一致
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// 版本号
        /// </summary>
        public string Version { get; set; }

        public ChaincodeStatus Status { get; set; }
        /// <summary>
        /// 背书策略 在链码初始化的时候 这些节点必须进行背书
        /// 即这些节点必须依据手动安装了链码
        /// </summary>
        public Policy Policy { get; set; }
    }

    public enum ChaincodeStatus
    {
        NONE,
        INSTALLED,
        INITIALIZED,
        SERVICE,
        STOP
    }
}
