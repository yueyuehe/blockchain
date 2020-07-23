namespace QMBlockSDK.CC
{
    public class ConfigKey
    {

        #region 系统配置链码 

        public const string Channel = "_channel";
        public const string InitChannelFunc = "_InitChannelFunc";
        public const string JoinChannelFunc = "_JoinChannelFunc";


        public const string AddOrgFunc = "_addorgfunc";
        public const string UpdateOrgFunc = "_updateorgfunc";

        public const string AddOrgMemberFunc = "_addorgmemberfunc";
        public const string UpdateOrgMemberFunc = "_updateorgmemberfunc";

        #endregion

        #region 系统配置查询链码
        //查询通道配置
        public const string QueryChannelConfig = "_QueryChannelConfig";
        //通道中注册成员
        //public const string RegistMember = "_RegistMember";
        #endregion


        #region 节点链码配置
        public const string ChaincodePath = "Chaincodes";
        public const string InstallChaincodeFunc = "_InstallChaincodeFunc";
        public const string InitChaincodeFunc = "_InitChaincodeFunc";
        #endregion

        #region 系统链码名称

        //交易查询链码
        public const string SysBlockQueryChaincode = "_SysBlockQueryChaincode";

        //链码生命周期管理
        public const string SysCodeLifeChaincode = "_SysCodeLifeChaincode";

        //网络配置链码
        public const string SysNetConfigChaincode = "_SysNetConfigChaincode";

        //身份管理链码
        //public const string SysIdentityChaincode = "_SysIdentityChaincode";

        #endregion

        /// <summary>
        /// block缓存
        /// </summary>
        public const string CahceBlock = "_CahceBlock";


        #region mongo document

        //状态数据存放的document
        public const string DataStatusDocument = "datastatusdocument";

        //区块存放的document
        public const string BlockDocument = "blockdocument";


        public const string ChannelConfig = "channelconfig";
        #endregion


    }
}
