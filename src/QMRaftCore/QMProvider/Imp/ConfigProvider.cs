using QMBlockSDK.Idn;
using QMBlockSDK.TX;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Peers;
using QMRaftCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QMRaftCore.QMProvider.Imp
{
    /// <summary>
    /// 配置提供器
    /// </summary>
    public class ConfigProvider : IConfigProvider
    {
        //private readonly IAssemblyProvider _assemblyProvider;
        private readonly IPolicyProvider _policyProvider;
        private readonly ISettings _settings;
        private readonly IIdentityProvider _identityProvider;
        private readonly IPeersProvider _peersProvider;

        public ConfigProvider(
            IAssemblyProvider assemblyProvider,
            IPolicyProvider policyProvider,
            IIdentityProvider identityProvider,
            IPeersProvider peersProvider)
        {
            _settings = new InMemorySettings();
            //_assemblyProvider = assemblyProvider;
            _identityProvider = identityProvider;
            _policyProvider = policyProvider;
            _peersProvider = peersProvider;
        }

        public List<IPeer> GetAllPeer(string channelid)
        {
            return _peersProvider.Get(channelid);
        }

        public int GetBatchTimeout()
        {
            return _settings.BatchTimeout;
        }

        public List<IPeer> GetEndorsePeer(string channelId, Chaincode chainCode)
        {
            return _policyProvider.GetEndorsePeer(channelId, chainCode);


        }

        public long GetEndorseTimeOut()
        {
            return _settings.EndorseTimeOut;
        }

        public long GetHeartbeatTimeout()
        {
            return _settings.HeartbeatTimeout;
        }

        public int GetMaxTxCount()
        {
            return _settings.MaxTxCount;
        }

        public long GetMinTimeout()
        {
            return _settings.MinTimeout;
        }

        public IPeer GetPeer(string channelId, string id)
        {
            return _peersProvider.GetById(channelId, id);
        }

        public PeerIdentity GetPeerIdentity()
        {
            return _identityProvider.GetPeerIdentity();
        }

        public List<IPeer> GetPeersExcludeSelf(string channelId)
        {
            var identity = _identityProvider.GetPeerIdentity();
            return _peersProvider.Get(channelId).Where(p => p.Id != identity.Address).ToList();
        }

        public string GetPrivateKey()
        {
            return _identityProvider.GetPrivateKey();
        }

        public PubliclyIdentity GetPublicIndentity()
        {
            return _identityProvider.GetPeerIdentity().GetPublic();
        }

        public TimeSpan GetToCandidateTimeOut()
        {
            var _random = new Random(Guid.NewGuid().GetHashCode());
            var randomMs = _random.Next(_settings.MinTimeout, _settings.MaxTimeout);
            return TimeSpan.FromMilliseconds(randomMs);
        }

        public TxRequest SignatureForTx(TxRequest tx)
        {
            return tx;
        }

        public bool ValidateEndorse(string channelId, Chaincode chainCode, Dictionary<string, EndorseResponse> endorseDir)
        {
            return _policyProvider.ValidateEndorse(channelId, chainCode, endorseDir);
        }
    }
}
