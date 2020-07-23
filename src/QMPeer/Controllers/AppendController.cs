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
    public class AppendController : ControllerBase
    {
        private readonly IMemoryCache _cache;
        public AppendController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        // POST: api/Append
        [HttpPost]
        public async Task<JsonResult> PostAsync([FromBody] QMRaftCore.Concensus.Messages.AppendEntries appendEntries)
        {
            var nodepper = _cache.Get<NodePeer>(CacheKeys.NodePeer);
            var node = nodepper.GetNode(appendEntries.ChannelId);
            /// 保证单线程访问，不会因为节点状态受影响
            var rs = await node.Handle(appendEntries);
            return new JsonResult(rs);
        }
    }
}
