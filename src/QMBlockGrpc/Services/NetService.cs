using Grpc.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using QMBlockGrpc.Service;
using QMBlockServer.Service.Intetface;
using QMRaftCore.Concensus.Messages;
using System;
using System.Threading.Tasks;

namespace QMBlockGrpc.Services
{
    /// <summary>
    /// 通道中的节点进行通信
    /// </summary>
    public class NetService : Net.NetBase
    {
        private readonly ILogger<NetService> _logger;
        private readonly IRaftNetService _raftNet;
        private readonly ITxService _txService;

        public NetService(ILogger<NetService> logger, IRaftNetService raftNet, ITxService txService)
        {
            _logger = logger;
            _raftNet = raftNet;
            _txService = txService;
        }

        //peer级别
        //[Authorize(Roles = "Peer")]
        public override async Task<NetResponse> Vote(NetRequest request, ServerCallContext context)
        {
            try
            {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<RequestVote>(request.Data);
                var rs = await _raftNet.Handle(model);
                return new NetResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                var rs = new RequestVoteResponse();
                rs.VoteGranted = false;
                return new NetResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }


        }

        //peer级别
        //[Authorize(Roles = "Peer")]
        public override async Task<NetResponse> HeartBeat(NetRequest request, ServerCallContext context)
        {
            try
            {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<HeartBeat>(request.Data);
                var rs = await _raftNet.Handle(model);
                return new NetResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                var rs = new HeartBeatResponse();
                rs.Success = false;
                return new NetResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
        }

        //peer级别

        //[Authorize(Roles = "Peer")]
        public override async Task<NetResponse> AppendEntries(NetRequest request, ServerCallContext context)
        {
            try
            {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<AppendEntries>(request.Data);
                var rs = await _raftNet.Handle(model);
                return new NetResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                var rs = new AppendEntriesResponse();
                rs.Success = false;
                return new NetResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }

        }

        //peer级别

        //[Authorize(Roles = "Peer")]
        public override async Task<NetResponse> Transaction(NetRequest request, ServerCallContext context)
        {
            try
            {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<QMBlockSDK.TX.TxRequest>(request.Data);
                var rs = await _txService.Transaction(model);
                return new NetResponse()
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
                return new NetResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
        }

        //peer级别

        //[Authorize(Roles = "Peer")]
        public override async Task<NetResponse> Endorse(NetRequest request, ServerCallContext context)
        {
            try
            {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<EndorseRequest>(request.Data);
                var rs = await _txService.Endorse(model);
                return new NetResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                var rs = new EndorseResponse();
                rs.Status = false;
                rs.Msg = ex.Message;
                return new NetResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
        }

        //peer级别

        //[Authorize(Roles = "Peer")]
        public override async Task<NetResponse> BlockHandOut(NetRequest request, ServerCallContext context)
        {
            try
            {
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<HandOutRequest>(request.Data);
                var rs = await _txService.BlockHandOut(model);
                return new NetResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                var rs = new HandOutResponse();
                rs.Success = false;
                rs.Message = ex.Message;
                return new NetResponse()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(rs)
                };
            }
        }
    }
}
