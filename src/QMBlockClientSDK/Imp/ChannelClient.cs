using QMBlockClientSDK.Model;
using QMBlockClientSDK.Service.Interface;
using QMBlockSDK.Config;
using QMBlockSDK.TX;
using System;
using System.Threading.Tasks;

namespace QMBlockClientSDK.Imp
{
    public class ChannelClient
    {
        private readonly string _channelId;
        private readonly ITxService _tx;
        private readonly IConfigService _config;
        public ChannelClient(string channelId, ITxService txService, IConfigService configService)
        {
            _channelId = channelId;
            _tx = txService;
            _config = configService;
        }

        #region 通道中加入节点

        public async Task<TxResponse> AddOrg(string orgId, string name, string address, string cert)
        {
            var config = new OrgConfig()
            {
                OrgId = orgId,
                Name = name,
                Address = address,
                Certificate = Newtonsoft.Json.JsonConvert.DeserializeObject<QMBlockSDK.Idn.Certificate>(cert)
            };
            return await _config.AddOrg(_channelId, config);
        }

        #endregion

        public Task<TxResponse> AddOrgPeer(OrgMemberConfig config)
        {
            throw new NotImplementedException();
        }

        #region 安装链码

        /// <summary>
        /// 安装链码
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public async Task<TxResponse> InstallChaincode(string name, string Namespace, string version, string policy)
        {
            var config = new ChaincodeModel()
            {
                Name = name,
                Namespace = Namespace,
                Version = version,
                Policy = policy
            };
            var model = new { number = 0, OrgIds = new string[] { } };
            try
            {
                Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(policy, model);
            }
            catch (Exception ex)
            {
                throw new Exception("policy 格式不正确 {number:int,OrgIds:string[]}");
            }

            return await _config.InstallChaincode(_channelId, config);
        }


        #endregion

        #region 初始化链码
        public async Task<TxResponse> InitChaincode(string chaincodeName, string[] args)
        {
            return await _config.InitChaincode(_channelId, chaincodeName, args);
        }
        #endregion

        #region invoke类型的交易 需要上链

        public async Task<TxResponse> InvokeTx(string chaincode, string func, string[] args)
        {
            var tx = new QMBlockSDK.TX.TxHeader();
            tx.ChannelId = _channelId;
            tx.Args = args;
            tx.FuncName = func;
            tx.ChaincodeName = chaincode;
            return await _tx.InvokeTx(tx);
        }

        #endregion

        #region 查询本地的
        public async Task<TxResponse> QueryTx(string chaincode, string func, string[] args)
        {
            var tx = new QMBlockSDK.TX.TxHeader();
            tx.ChannelId = _channelId;
            tx.Args = args;
            tx.FuncName = func;
            tx.ChaincodeName = chaincode;
            return await _tx.QueryTx(tx);
        }

        #endregion

        #region 创建新通道
        public async Task<TxResponse> CreateNewChannel(string channelId)
        {
            return await _config.InitChannel(channelId);
        }

        #endregion

        #region 新节点加入某个通道 去前提是通道中已经配置了该节点

        public async Task<TxResponse> JoinChannel(string channelId)
        {
            return await _config.JoinChannel(channelId);
        }


        #endregion

    }
}
