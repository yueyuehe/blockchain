using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QMBlockSDK.TX;
using QMBlockServer.Service.Intetface;
using QMRaftCore.Concensus.Messages;

namespace QM.Block.WebPeer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NetController : ControllerBase
    {
        private readonly ILogger<NetController> _logger;
        private readonly IRaftNetService _raftNet;
        private readonly ITxService _txService;

        public NetController(ILogger<NetController> logger, IRaftNetService raftNet, ITxService txService)
        {
            _logger = logger;
            _raftNet = raftNet;
            _txService = txService;
        }

        #region 投票
        //peer级别
        //[Authorize(Roles = "Peer")]
        public async Task<JsonResult> Vote(RequestVote model)
        {
            var rs = await _raftNet.Handle(model);
            return new JsonResult(rs);
        }
        #endregion

        //peer级别
        //[Authorize(Roles = "Peer")]
        public async Task<JsonResult> HeartBeat(HeartBeat model)
        {
            var rs = await _raftNet.Handle(model);
            return new JsonResult(rs);
        }


        //peer级别

        //[Authorize(Roles = "Peer")]
        public async Task<JsonResult> AppendEntries(AppendEntries model)
        {
            var rs = await _raftNet.Handle(model);
            return new JsonResult(rs);
        }



        //peer级别

        //[Authorize(Roles = "Peer")]
        public async Task<JsonResult> Transaction(TxRequest model)
        {
            var rs = await _txService.Transaction(model);
            return new JsonResult(rs);
        }



        //peer级别

        //[Authorize(Roles = "Peer")]
        public async Task<JsonResult> Endorse(EndorseRequest request)
        {
            var rs = await _txService.Endorse(request);
            return new JsonResult(rs);
        }




        //peer级别

        //[Authorize(Roles = "Peer")]
        public async Task<JsonResult> BlockHandOut(HandOutRequest request)
        {
            var rs = await _txService.BlockHandOut(request);
            return new JsonResult(rs);
        }
    }


}
