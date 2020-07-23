using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using QMRaftCore;
using QMRaftCore.Concensus.Node;
using QMRaftCore.QMProvider;

namespace QMPeer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VoteController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        public VoteController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }
        // POST: api/Vote
        [HttpPost]
        public async Task<JsonResult> PostAsync([FromBody] QMRaftCore.Concensus.Messages.RequestVote requestVote)
        {
            var nodepper = _cache.Get<NodePeer>(CacheKeys.NodePeer);
            var node = nodepper.GetNode(requestVote.ChannelId);
            var rs = await node.Handle(requestVote);
            return new JsonResult(rs);
        }
    }
}
