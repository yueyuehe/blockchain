using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using QMRaftCore;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Node;
using QMRaftCore.QMProvider;

namespace QMPeer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BlockController : ControllerBase
    {

        private readonly IMemoryCache _cache;
        public BlockController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }
        // POST: api/Block
        [HttpPost]
        public async Task<JsonResult> PostAsync([FromBody] HandOutRequest request)
        {
            var nodepper = _cache.Get<NodePeer>(CacheKeys.NodePeer);
            var node = nodepper.GetNode(request.ChannedId);
            var rs = await node.Handle(request);
            return new JsonResult(rs);
        }
    }
}
