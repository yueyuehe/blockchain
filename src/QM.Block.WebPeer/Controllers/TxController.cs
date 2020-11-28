using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using QMBlockSDK.TX;
using QMBlockServer.Service.Intetface;

namespace QM.Block.WebPeer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TxController : ControllerBase
    {
        private readonly ILogger<TxController> _logger;
        private readonly ITxService _txService;

        public TxController(ILogger<TxController> logger, ITxService txService)
        {
            _logger = logger;
            _txService = txService;
        }
        //member.admin
        //[Authorize(Roles = "Admin,User")]
        [HttpPost("/api/tx/invoke")]
        public async Task<JsonResult> InvokeTx(TxHeader request)
        {
            var rs = await _txService.InvokeTx(request);
            return new JsonResult(rs);
        }

        //member.query
        //[Authorize(Roles = "Admin,User,Reader")]
        [HttpPost("/api/tx/query")]
        public async Task<JsonResult> QueryTx(TxHeader request)
        {
            var rs = await _txService.QueryTx(request);
            return new JsonResult(rs);
        }

        //peer
        //[Authorize]
        //创建新通道并且启动
        public async Task<JsonResult> InitChannel(TxHeader request)
        {
            var rs = await _txService.InitChannel(request);
            return new JsonResult(rs);
        }

        //peer
        //[Authorize]
        //加入通道
        public async Task<JsonResult> JoinChannel(TxHeader request)
        {
            var rs = await _txService.JoinChannel(request);
            return new JsonResult(rs);
        }
    }
}
