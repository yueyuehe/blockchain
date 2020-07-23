using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using QMRaftCore.Concensus.Peers;
using QMRaftCore.Data.Imp;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using System.Collections.Generic;
using System.Linq;

namespace QMRaftCore.QMProvider.Imp
{
    public class GrpcPeerProvider : IPeersProvider
    {
        //private readonly IFiniteStateMachine _stateMachine;
        private readonly ILoggerFactory _factory;
        private readonly ILogger<GrpcPeerProvider> _logger;
        private Dictionary<string, GrpcChannel> _grpcChannel;
        private readonly DataManager _dataManager;

        public GrpcPeerProvider(ILoggerFactory factory, DataManager dataManager)
        {
            _dataManager = dataManager;
            _logger = factory.CreateLogger<GrpcPeerProvider>();
            _factory = factory;
            _grpcChannel = new Dictionary<string, GrpcChannel>();
        }

        //获取通道中所有节点
        public List<IPeer> Get(string channelId)
        {
            var config = _dataManager.GetChannelConfig(channelId);
            var list = config.OrgConfigs.Select(p => p.Address).ToList();
            var peerList = new List<IPeer>();
            foreach (var item in list)
            {
                //如果不存在
                if (!_grpcChannel.ContainsKey(item))
                {
                    var channel = GrpcChannel.ForAddress(item);
                    _grpcChannel.Add(item, channel);
                }
                peerList.Add(new GrpcPeer(item, _grpcChannel[item], _factory));
            }
            return peerList;
        }

        public IPeer GetById(string channelid, string peerId)
        {
            if (_grpcChannel.ContainsKey(peerId))
            {
                return new GrpcPeer(peerId, _grpcChannel[peerId], _factory);
            }
            else
            {
                var channel = GrpcChannel.ForAddress(peerId);
                _grpcChannel.Add(peerId, channel);
                return new GrpcPeer(peerId, channel, _factory);
            }
        }

        public IPeer GetByIp(string ip)
        {
            if (!_grpcChannel.ContainsKey(ip))
            {
                var channel = GrpcChannel.ForAddress(ip);
                _grpcChannel.Add(ip, channel);
            }
            return new GrpcPeer(ip, _grpcChannel[ip], _factory);
        }
    }
}
