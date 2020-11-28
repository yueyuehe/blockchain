using QMBlockSDK.TX;
using QMRaftCore.Concensus.Messages;
using QMRaftCore.FiniteStateMachine;
using QMRaftCore.Infrastructure;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace QMRaftCore.Concensus.Peers
{

    public class Peer : IPeer
    {
        private readonly string _id;

        private readonly IHttpClientFactory _httpClientFactory;

        public string Id { get { return this._id; } }


        public Peer(IHttpClientFactory factory, string id)
        {
            _id = id;
            _httpClientFactory = factory;
        }
        public async Task<RequestVoteResponse> Request(RequestVote requestVote)
        {
            try
            {
                using (var client = _httpClientFactory.CreateClient(QMRaftCore.Config.VoteHttpClientName))
                {
                    HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(requestVote));
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    var response = await client.PostAsync(this.Id + "/api/vote", content);//改成自己的
                    response.EnsureSuccessStatusCode();//用来抛异常的
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var model = Newtonsoft.Json.JsonConvert.DeserializeObject<RequestVoteResponse>(responseBody);
                    return model;
                }
            }
            catch (Exception e)
            {
                return new RequestVoteResponse(false, 0);
            }

        }

        public async Task<AppendEntriesResponse> Request(AppendEntries appendEntries)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(QMRaftCore.Config.VoteHttpClientName);
                HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(appendEntries));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(this.Id + "/api/append", content);//改成自己的
                response.EnsureSuccessStatusCode();//用来抛异常的
                var responseBody = await response.Content.ReadAsStringAsync();
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<AppendEntriesResponse>(responseBody);
                return model;
            }
            catch (Exception e)
            {
                return new AppendEntriesResponse { Term = 0, Success = false };
            }

        }

        public Task<Response<T>> Request<T>(T command) where T : ICommand
        {
            throw new NotImplementedException();
        }

        public async Task<HeartBeatResponse> Request(HeartBeat request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(QMRaftCore.Config.VoteHttpClientName);
                HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(request));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(this.Id + "/api/heartbeat", content);//改成自己的
                response.EnsureSuccessStatusCode();//用来抛异常的
                var responseBody = await response.Content.ReadAsStringAsync();
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<HeartBeatResponse>(responseBody);
                return model;
            }
            catch (Exception e)
            {
                return new HeartBeatResponse { Term = 0, Success = false };
            }
        }

        public async Task<TxResponse> Transaction(TxRequest request)
        {

            try
            {
                var client = _httpClientFactory.CreateClient(QMRaftCore.Config.VoteHttpClientName);
                HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(request));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(this.Id + "/api/Tx/peersubmit", content);//改成自己的
                response.EnsureSuccessStatusCode();//用来抛异常的
                var responseBody = await response.Content.ReadAsStringAsync();
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<TxResponse>(responseBody);
                return model;
            }
            catch (Exception e)
            {
                return new TxResponse { Status = false, Msg = e.Message };
            }
        }

        /// <summary>
        /// 交易背书
        /// </summary>
        /// <param name="endorseRequest"></param>
        /// <returns></returns>
        public async Task<EndorseResponse> Endorse(EndorseRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(QMRaftCore.Config.VoteHttpClientName);
                HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(request));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(this.Id + "/api/Tx/Endorse", content);//改成自己的
                response.EnsureSuccessStatusCode();//用来抛异常的
                var responseBody = await response.Content.ReadAsStringAsync();
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<EndorseResponse>(responseBody);
                return model;
            }
            catch (Exception e)
            {
                return new EndorseResponse { Status = false, Msg = e.Message };
            }
        }

        public async Task<HandOutResponse> BlockHandOut(HandOutRequest request)
        {
            try
            {
                var client = _httpClientFactory.CreateClient(QMRaftCore.Config.VoteHttpClientName);
                HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(request));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = await client.PostAsync(this.Id + "/api/Block/HandOut", content);//改成自己的
                response.EnsureSuccessStatusCode();//用来抛异常的
                var responseBody = await response.Content.ReadAsStringAsync();
                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<HandOutResponse>(responseBody);
                return model;
            }
            catch (Exception e)
            {
                return new HandOutResponse { Success = false, Message = e.Message };
            }
        }

    }
}