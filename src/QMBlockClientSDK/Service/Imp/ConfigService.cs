using QMBlockClientSDK.Interface;
using QMBlockClientSDK.Model;
using QMBlockClientSDK.Service.Interface;
using QMBlockSDK.CC;
using QMBlockSDK.Config;
using QMBlockSDK.TX;
using QMBlockUtils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QMBlockClientSDK.Service.Imp
{
    public class ConfigService : IConfigService
    {
        private readonly IGrpcClient _client;

        public ConfigService(IGrpcClient client)
        {
            _client = client;
        }

        public async Task<TxResponse> InitChannel(string channelId)
        {
            var tx = new QMBlockSDK.TX.TxHeader();
            tx.ChannelId = channelId;
            tx.Type = TxType.Invoke;
            tx.FuncName = QMBlockSDK.CC.ConfigKey.InitChannelFunc;
            tx.ChaincodeName = QMBlockSDK.CC.ConfigKey.SysNetConfigChaincode;
            return await _client.TxInvoke(tx);
        }

        public async Task<TxResponse> JoinChannel(string channelId)
        {
            var tx = new QMBlockSDK.TX.TxHeader();
            tx.ChannelId = channelId;
            tx.Type = TxType.Invoke;
            tx.FuncName = QMBlockSDK.CC.ConfigKey.JoinChannelFunc;
            tx.ChaincodeName = QMBlockSDK.CC.ConfigKey.SysNetConfigChaincode;
            return await _client.TxInvoke(tx);
        }

        public async Task<TxResponse> AddOrg(string channelid, OrgConfig config)
        {
            if (string.IsNullOrEmpty(config.Address))
            {
                throw new Exception("请输入组织地址");
            }
            if (string.IsNullOrEmpty(config.Name))
            {
                throw new Exception("请输入组织名称");
            }
            if (string.IsNullOrEmpty(config.OrgId))
            {
                throw new Exception("请输入组织ID");
            }
            if (config.Certificate == null)
            {
                throw new Exception("未找到组织证书");
            }

            var checkRs = RSAHelper.VerifyData(config.Certificate.TBSCertificate.PublicKey, Newtonsoft.Json.JsonConvert.SerializeObject(config.Certificate.TBSCertificate), config.Certificate.SignatureValue);
            if (!checkRs)
            {
                throw new Exception("证书校验不通过");
            }

            var txHeader = new TxHeader();
            txHeader.Args = new string[] { Newtonsoft.Json.JsonConvert.SerializeObject(config) };
            txHeader.ChaincodeName = ConfigKey.SysNetConfigChaincode;
            txHeader.FuncName = ConfigKey.AddOrgFunc;
            txHeader.Type = TxType.Invoke;
            txHeader.ChannelId = channelid;
            return await _client.TxInvoke(txHeader);
        }

        public Task<TxResponse> AddOrgMember(OrgMemberConfig config)
        {
            throw new NotImplementedException();
        }

        public async Task<TxResponse> InstallChaincode(string channelId, ChaincodeModel config)
        {
            var txHeader = new TxHeader();
            txHeader.Args = new string[] { config.Name, config.Namespace, config.Version, config.Policy };
            txHeader.ChaincodeName = ConfigKey.SysCodeLifeChaincode;
            txHeader.FuncName = ConfigKey.InstallChaincodeFunc;
            txHeader.ChannelId = channelId;
            return await _client.TxInvoke(txHeader);
        }

        public async Task<TxResponse> InitChaincode(string channelId, string chaincodeName, string[] args)
        {
            var txHeader = new TxHeader();
            var list = new List<string>();
            list.Add(chaincodeName);
            foreach (var item in args)
            {
                list.Add(item);
            }

            txHeader.Args = list.ToArray();
            txHeader.ChaincodeName = ConfigKey.SysCodeLifeChaincode;
            txHeader.FuncName = ConfigKey.InitChaincodeFunc;
            txHeader.Type = TxType.Invoke;
            txHeader.ChannelId = channelId;
            return await _client.TxInvoke(txHeader);
        }

    }
}
