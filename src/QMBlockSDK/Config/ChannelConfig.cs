using System.Collections.Generic;

namespace QMBlockSDK.Config
{
    /// <summary>
    /// 通道配置
    /// </summary>
    public class ChannelConfig
    {
        public string ChannelID { get; set; }

        /// <summary>
        /// 通道下的链码
        /// </summary>
        public List<ChaincodeConfig> ChainCodeConfigs { get; set; }

        /// <summary>
        /// 通道下的组织
        /// </summary>
        public List<OrgConfig> OrgConfigs { get; set; }

        public ChannelConfig()
        {
            OrgConfigs = new List<OrgConfig>();
            ChainCodeConfigs = new List<ChaincodeConfig>();
        }

    }
}
