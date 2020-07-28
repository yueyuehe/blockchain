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
        //private readonly IGrpcClient _client;
        private readonly ITxService _service;

        public ConfigService(ITxService service)
        {
            _service = service;
        }

        public async Task<TxResponse> InitChannel(string channelId)
        {
            var tx = new QMBlockSDK.TX.TxHeader
            {
                ChannelId = channelId,
                Type = TxType.Invoke,
                FuncName = QMBlockSDK.CC.ConfigKey.InitChannelFunc,
                ChaincodeName = QMBlockSDK.CC.ConfigKey.SysNetConfigChaincode
            };
            return await _service.InvokeTx(tx);
            //return await _client.TxInvoke(tx);
        }

        public async Task<TxResponse> JoinChannel(string channelId)
        {
            var tx = new QMBlockSDK.TX.TxHeader
            {
                ChannelId = channelId,
                Type = TxType.Invoke,
                FuncName = QMBlockSDK.CC.ConfigKey.JoinChannelFunc,
                ChaincodeName = QMBlockSDK.CC.ConfigKey.SysNetConfigChaincode
            };
            return await _service.InvokeTx(tx);
            //return await _client.TxInvoke(tx);
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

            var txHeader = new TxHeader
            {
                Args = new string[] { Newtonsoft.Json.JsonConvert.SerializeObject(config) },
                ChaincodeName = ConfigKey.SysNetConfigChaincode,
                FuncName = ConfigKey.AddOrgFunc,
                Type = TxType.Invoke,
                ChannelId = channelid
            };
            return await _service.InvokeTx(txHeader);
            //return await _client.TxInvoke(txHeader);
        }

        public Task<TxResponse> AddOrgMember(OrgMemberConfig config)
        {
            throw new NotImplementedException();
        }

        public async Task<TxResponse> InstallChaincode(string channelId, ChaincodeModel config)
        {
            var txHeader = new TxHeader
            {
                Args = new string[] { config.Name, config.Namespace, config.Version, config.Policy },
                ChaincodeName = ConfigKey.SysCodeLifeChaincode,
                FuncName = ConfigKey.InstallChaincodeFunc,
                ChannelId = channelId
            };
            return await _service.InvokeTx(txHeader);
            //return await _client.TxInvoke(txHeader);
        }

        public async Task<TxResponse> InitChaincode(string channelId, string chaincodeName, string[] args)
        {
            var txHeader = new TxHeader();
            var list = new List<string>
            {
                chaincodeName
            };
            foreach (var item in args)
            {
                list.Add(item);
            }

            txHeader.Args = list.ToArray();
            txHeader.ChaincodeName = ConfigKey.SysCodeLifeChaincode;
            txHeader.FuncName = ConfigKey.InitChaincodeFunc;
            txHeader.Type = TxType.Invoke;
            txHeader.ChannelId = channelId;
            return await _service.InvokeTx(txHeader);
            //return await _client.TxInvoke(txHeader);
        }

    }
}
