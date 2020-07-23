using QMBlockSDK.Idn;
using System.Collections.Generic;

namespace QMBlockSDK.Config
{
    public class OrgConfig
    {
        /// <summary>
        ///组织ID
        /// </summary>
        public string OrgId { get; set; }

        //名称
        public string Name { get; set; }

        /// <summary>
        /// 节点域名
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// 证书
        /// </summary>
        public Certificate Certificate { get; set; }

        /// <summary>
        /// 组织下的成员
        /// </summary>
        public List<OrgMemberConfig> OrgMember { get; set; }

        public OrgConfig()
        {
            OrgMember = new List<OrgMemberConfig>();
        }
    }

    public class OrgMemberConfig
    {
        public string OrgId { get; set; }
        public string Name { get; set; }
        public Certificate Certificate { get; set; }

    }
}
