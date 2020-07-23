using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using QMBlockGrpc.Service;
using QMBlockServer.Service.Intetface;
using System;
using System.Threading.Tasks;

namespace QMBlockGrpc.Services
{
    public class TxService : Tx.TxBase
    {
        private readonly ILogger<TxService> _logger;
        private readonly ITxService _txService;

        public TxService(ILogger<TxService> logger, ITxService txService)
        {
            _logger = logger;
            _txService = txService;
        }

        //member.admin

        //[Authorize(Roles = "Admin,User")]
        public override async Task<TxResponse> InvokeTx(TxHeader request, ServerCallContext context)
        {
            try
            {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<QMBlockSDK.TX.TxHeader>(request.Data);
                var rs = await _txService.InvokeTx(model);
                return new TxResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                var rs = new QMBlockSDK.TX.TxResponse();
                rs.Msg = ex.Message;
                rs.Status = false;
                return new TxResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
        }

        //member.query
        //[Authorize(Roles = "Admin,User,Reader")]
        public override async Task<TxResponse> QueryTx(TxHeader request, ServerCallContext context)
        {
            try
            {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<QMBlockSDK.TX.TxHeader>(request.Data);
                var rs = await _txService.QueryTx(model);
                return new TxResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                var rs = new QMBlockSDK.TX.TxResponse();
                rs.Msg = ex.Message;
                rs.Status = false;
                return new TxResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
        }

        //peer
        //[Authorize]
        //创建新通道并且启动
        public override async Task<TxResponse> InitChannel(TxHeader request, ServerCallContext context)
        {

            try
            {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<QMBlockSDK.TX.TxHeader>(request.Data);
                var rs = await _txService.InitChannel(model);
                return new TxResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                var rs = new QMBlockSDK.TX.TxResponse();
                rs.Msg = ex.Message;
                rs.Status = false;
                return new TxResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
        }

        //peer
        //[Authorize]
        //加入通道
        public override async Task<TxResponse> JoinChannel(TxHeader request, ServerCallContext context)
        {
            try
            {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<QMBlockSDK.TX.TxHeader>(request.Data);
                var rs = await _txService.JoinChannel(model);
                return new TxResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                var rs = new QMBlockSDK.TX.TxResponse();
                rs.Msg = ex.Message;
                rs.Status = false;
                return new TxResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
        }
    }
}
