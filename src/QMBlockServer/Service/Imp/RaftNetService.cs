using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using QMBlockServer.Service.Intetface;
using QMRaftCore;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Node;
using System;
using System.Threading.Tasks;

namespace QMBlockServer.Service.Imp
{
    public class RaftNetService : IRaftNetService
    {
        private readonly IMemoryCache _cache;
        private readonly NodePeer _nodePeer;
        private readonly ILogger<RaftNetService> _logger;

        public RaftNetService(IMemoryCache memoryCache, ILogger<RaftNetService> logger)
        {
            _cache = memoryCache;
            _nodePeer = _cache.Get<NodePeer>(CacheKeys.NodePeer);
            _logger = logger;
        }

        //追加 peer
        public async Task<AppendEntriesResponse> Handle(AppendEntries appendEntries)
        {
            var node = _nodePeer.GetNode(appendEntries.ChannelId);
            if (node == null)
            {
                throw new Exception($"节点未加入{appendEntries.ChannelId} 通道");
            }
            return await node.Handle(appendEntries);
        }

        //心跳 peer
        public async Task<HeartBeatResponse> Handle(HeartBeat request)
        {
            var node = _nodePeer.GetNode(request.ChannelId);
            if (node == null)
            {
                throw new Exception($"节点未加入{request.ChannelId} 通道");
            }
            return await node.Handle(request);
        }

        //投票 peer
        public async Task<RequestVoteResponse> Handle(RequestVote requestVote)
        {
            var node = _nodePeer.GetNode(requestVote.ChannelId);
            if (node == null)
            {
                throw new Exception($"节点未加入{requestVote.ChannelId} 通道");
            }

            return await node.Handle(requestVote);
        }
    }
}
