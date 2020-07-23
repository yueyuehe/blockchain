using Microsoft.Extensions.Logging;
using QMBlockSDK.TX;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Peers;
using QMRaftCore.Data.Imp;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace QMRaftCore.QMProvider.Imp
{
    public class PolicyProvider : IPolicyProvider
    {
        private readonly IIdentityProvider _identityProvider;
        private readonly IPeersProvider _peersprovider;
        private readonly ILogger<PolicyProvider> _logger;
        private readonly DataManager _dataManager;

        public PolicyProvider(
            ILoggerFactory factory,
            IIdentityProvider identityProvider,
            IPeersProvider peersProvider,
            DataManager dataManager)
        {
            _logger = factory.CreateLogger<PolicyProvider>();
            _dataManager = dataManager;
            _identityProvider = identityProvider;
            _peersprovider = peersProvider;
        }



        /// <summary>
        /// 获取 需要背书的节点
        /// </summary>
        /// <param name="ChannelId"></param>
        /// <param name="chainCode"></param>
        /// <returns></returns>
        public List<IPeer> GetEndorsePeer(string channelId, Chaincode chainCode)
        {
            _logger.LogInformation("获取背书需要的节点");

            var list = new List<IPeer>();
            //如果是其他 获取最新配置
            var channelconfig = _dataManager.GetChannelConfig(channelId);
            //获取链码名称的背书策略
            var chaincodeConfig = channelconfig.ChainCodeConfigs.Where(p => p.Name == chainCode.Name).FirstOrDefault();
            foreach (var item in chaincodeConfig.Policy.OrgIds)
            {
                var org = channelconfig.OrgConfigs.Where(p => p.OrgId == item).FirstOrDefault();
                var peer = _peersprovider.GetById(channelconfig.ChannelID, org.Address);
                list.Add(peer);
            }
            return list;
        }

        public bool ValidateEndorse(string channelId, Chaincode chainCode, Dictionary<string, EndorseResponse> endorseDir)
        {
            _logger.LogInformation("开始校验背书策略");
            foreach (var item in endorseDir)
            {
                if (item.Value.Status == false)
                {
                    return false;
                }
            }
            //验证背书节点的策略
            var peers = GetEndorsePeer(channelId, chainCode);
            var channelconfig = _dataManager.GetChannelConfig(channelId);
            //获取链码名称的背书策略
            var chaincodeConfig = channelconfig.ChainCodeConfigs.Where(p => p.Name == chainCode.Name).FirstOrDefault();

            //如果没有达到指定的背书数量则返回false
            if (chaincodeConfig.Policy.Number != endorseDir.Count)
            {
                return false;
            }
            var list = peers.Select(p => p.Id).Distinct().ToList();

            //判断是否是指定节点的背书
            var ips = peers.Select(p => p.Id).ToList();
            foreach (var item in endorseDir)
            {
                if (!ips.Contains(item.Key))
                {
                    return false;
                }
            }
            //判断几个节点的背书节点是否一致
            var checkdata = "";
            foreach (var item in endorseDir)
            {
                if (string.IsNullOrEmpty(checkdata))
                {
                    checkdata = Newtonsoft.Json.JsonConvert.SerializeObject(item.Value.TxReadWriteSet);
                    continue;
                }
                if (checkdata != Newtonsoft.Json.JsonConvert.SerializeObject(item.Value.TxReadWriteSet))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
