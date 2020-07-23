using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using QMBlockSDK.TX;
using QMRaftCore.Client;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using System;
using System.Threading.Tasks;


namespace QMRaftCore.Concensus.Peers
{
    public class GrpcPeer : IPeer
    {
        private readonly GrpcChannel _channel;
        private readonly string _id;
        private readonly ILogger<GrpcPeer> _logger;

        public GrpcPeer(string id, GrpcChannel channel, ILoggerFactory factory)
        {
            _logger = factory.CreateLogger<GrpcPeer>();
            _channel = channel;
            _id = id;
        }

        public string Id { get { return this._id; } }

        public async Task<HandOutResponse> BlockHandOut(HandOutRequest request)
        {
            try
            {
                var model = new NetRequest
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(request)
                };
                var rs = await GetClient().BlockHandOutAsync(model);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<HandOutResponse>(rs.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new HandOutResponse()
                {
                    Message = ex.Message,
                    Success = false
                };
            }
        }

        public async Task<EndorseResponse> Endorse(EndorseRequest request)
        {
            try
            {
                var model = new NetRequest
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(request)
                };
                var rs = await GetClient().EndorseAsync(model);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<EndorseResponse>(rs.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new EndorseResponse()
                {
                    Status = false,
                    Msg = ex.Message
                };
            }
        }

        public async Task<RequestVoteResponse> Request(RequestVote request)
        {
            try
            {
                var model = new NetRequest
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(request)
                };
                var rs = await GetClient().VoteAsync(model);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<RequestVoteResponse>(rs.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new RequestVoteResponse()
                {
                    VoteGranted = false,
                };
            }
        }

        public async Task<HeartBeatResponse> Request(HeartBeat request)
        {
            try
            {
                var model = new NetRequest
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(request)
                };
                var rs = await GetClient().HeartBeatAsync(model);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<HeartBeatResponse>(rs.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new HeartBeatResponse()
                {
                    Success = false
                };
            }

        }

        public async Task<AppendEntriesResponse> Request(AppendEntries request)
        {
            try
            {
                var model = new NetRequest
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(request)
                };
                var rs = await GetClient().AppendEntriesAsync(model);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AppendEntriesResponse>(rs.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new AppendEntriesResponse()
                {
                    Success = false
                };
            }
        }

        public Task<Response<T>> Request<T>(T command) where T : ICommand
        {
            throw new NotImplementedException();
        }

        public async Task<TxResponse> Transaction(TxRequest request)
        {
            try
            {
                var model = new NetRequest
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(request)
                };
                var rs = await GetClient().TransactionAsync(model);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<TxResponse>(rs.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new TxResponse()
                {
                    Status = false,
                    Msg = ex.Message
                };
            }
        }

        private Net.NetClient GetClient()
        {
            return new Net.NetClient(_channel);
        }
    }
}
