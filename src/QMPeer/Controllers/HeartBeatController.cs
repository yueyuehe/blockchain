using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using QMRaftCore;
using QMRaftCore.Concensus.Node;
using QMRaftCore.QMProvider;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace QMPeer.Controllers
{
    [Route("api/[controller]")]
    public class HeartBeatController : Controller
    {
        private readonly IMemoryCache _cache;
        public HeartBeatController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        // POST: api/Append
        [HttpPost]
        public async Task<JsonResult> PostAsync([FromBody] QMRaftCore.Concensus.Messages.HeartBeat heartBeat)
        {
            /// 保证单线程访问，不会因为节点状态受影响
            var nodepper = _cache.Get<NodePeer>(CacheKeys.NodePeer);
            var node = nodepper.GetNode(heartBeat.ChannelId);

            var rs = await node.Handle(heartBeat);
            return new JsonResult(rs);
        }
    }
}
