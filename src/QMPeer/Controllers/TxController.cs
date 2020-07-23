using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using QMBlockSDK.TX;
using QMPeer.Helper;
using QMRaftCore;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.Concensus.Node;
using QMRaftCore.QMProvider;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace QMPeer.Controllers
{
    [Route("api/[controller]")]
    public class TxController : Controller
    {
        private readonly IMemoryCache _cache;
        public TxController(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        // POST api/<controller>
        [HttpPost]
        public void Post([FromBody]string value)
        {

        }


        /// <summary>
        /// 接受来自客户端的消息
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("submit")]
        public async Task<JsonResult> Submit([FromBody]TxRequest request)
        {
            try
            {
                var nodepper = _cache.Get<NodePeer>(CacheKeys.NodePeer);
                var node = nodepper.GetNode(request.Data.Channel.ChannelId);
                request.TxId = Guid.NewGuid().ToString();
                request.Timestamp = DateTime.Now.Ticks;
                var rs = await node.TransactionCommit(request);
                return ApiHelper.Reponse("ok", rs, true);
            }
            catch (Exception ex)
            {
                return ApiHelper.Reponse("err", ex.Message, false);
            }

        }


        /// <summary>
        /// 接收来自peer节点的消息
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("peersubmit")]

        public async Task<JsonResult> PeerSubmit([FromBody]TxRequest request)
        {
            try
            {
                var nodepper = _cache.Get<NodePeer>(CacheKeys.NodePeer);
                var node = nodepper.GetNode(request.Data.Channel.ChannelId);
                var rs = await node.Transaction(request);
                return ApiHelper.Reponse("ok", rs, true);
            }
            catch (Exception ex)
            {
                return ApiHelper.Reponse(ex.Message, null, false);
            }
        }

        /// <summary>
        /// 接受leader节点的背书请求
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<JsonResult> Endorse([FromBody]TxRequest request)
        {
            try
            {
                var nodepper = _cache.Get<NodePeer>(CacheKeys.NodePeer);
                var node = nodepper.GetNode(request.Channel.ChannelId);

                var rs = await node.Endorse(request);
                return ApiHelper.Reponse("ok", rs, true);
            }
            catch (Exception ex)
            {
                return ApiHelper.Reponse(ex.Message, null, false);
            }
        }


    }
}
