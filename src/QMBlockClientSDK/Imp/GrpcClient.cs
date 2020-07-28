using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using QMBlockClientSDK.Interface;
using QMBlockSDK.Idn;
using QMBlockSDK.TX;
using QMBlockUtils;
using System;
using System.Threading.Tasks;

namespace QMBlockClientSDK.Imp
{
    public class GrpcClient : IGrpcClient
    {

        private readonly GrpcChannel _grpcChannel;
        private string _token;
        private Client.Tx.TxClient Client { get { return new QMBlockClientSDK.Client.Tx.TxClient(_grpcChannel); } }
        private Client.Auth.AuthClient AuthClient { get { return new QMBlockClientSDK.Client.Auth.AuthClient(_grpcChannel); } }

        private readonly ILogger<GrpcClient> _logger;
        private Metadata Header
        {
            get
            {
                var headers = new Metadata();
                headers.Add("Authorization", $"Bearer {_token}");
                return headers;
            }
        }

        public GrpcClient(string ip, ILoggerFactory factory)
        {
            _logger = factory.CreateLogger<GrpcClient>();
            var channel = GrpcChannel.ForAddress(ip);
            _grpcChannel = channel;
        }


        #region  invock类型的交易
        public async Task<TxResponse> TxInvoke(TxHeader request)
        {
            try
            {
                if (string.IsNullOrEmpty(_token))
                {
                    //使用身份登录

                }

                QMBlockClientSDK.Client.TxHeader model = new Client.TxHeader()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(request)
                };
                var rs = await Client.InvokeTxAsync(model, Header);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<TxResponse>(rs.Data);
            }
            catch (Exception ex)
            {
                return new TxResponse()
                {
                    Msg = ex.Message,
                    Status = false
                };
            }
        }
        #endregion


        #region 查询类型的交易 (严格来说只是查询数据 不算交易)
        public async Task<TxResponse> TxQuery(TxHeader request)
        {
            try
            {
                QMBlockClientSDK.Client.TxHeader model = new Client.TxHeader()
                {
                    Data = Newtonsoft.Json.JsonConvert.SerializeObject(request)
                };
                var rs = await Client.QueryTxAsync(model, Header);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<TxResponse>(rs.Data);
            }
            catch (Exception ex)
            {
                return new TxResponse()
                {
                    Msg = ex.Message,
                    Status = false
                };
            }

        }


        #endregion


        #region 身份认证
        public async Task<string> LoginAsync(string pk, string canumber)
        {
            try
            {
                var replay = await AuthClient.GetCodeAsync(new QMBlockClientSDK.Client.AuthRequest() { CaNumber = canumber });
                if (replay.Status)
                {
                    //签名
                    var signature = RSAHelper.SignData(pk, replay.Code);
                    var rs = await AuthClient.GetTokenAsync(new QMBlockClientSDK.Client.AuthRequest() { CaNumber = canumber, Data = signature });
                    if (rs.Status)
                    {
                        _token = rs.Token;
                        return "";
                    }
                    else
                    {
                        return string.IsNullOrEmpty(rs.Msg) ? "获取token失败" : rs.Msg;
                    }
                }
                else
                {
                    return string.IsNullOrEmpty(replay.Msg) ? "获取token失败" : replay.Msg;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return ex.Message;
            }
        }
        /// <summary>
        /// 判断是否已经登录
        /// </summary>
        /// <returns></returns>
        public async Task<bool> IsLoginAsync()
        {
            try
            {
                //判断是否已经登录
                var rs = await AuthClient.RefreshTokenAsync(new QMBlockClientSDK.Client.AuthRequest() { Data = "" }, Header);
                return rs.Status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 刷新
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="canumber"></param>
        /// <returns></returns>
        public async Task<bool> RefreshTokenAsync()
        {
            try
            {
                var rs = await AuthClient.RefreshTokenAsync(new QMBlockClientSDK.Client.AuthRequest() { Data = _token }, Header);
                if (rs.Status)
                {
                    _token = rs.Token;
                }
                return rs.Status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        #endregion




        #region 申请账号
        /// <summary>
        /// 用户名或者密码错误会直接抛错
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="type"></param>
        /// <param name="accountName"></param>
        /// <returns></returns>
        public async Task<UserAccount> GenerateAccountAsync(string username, string password, CAType type, string accountName)
        {
            var accountType = "0";
            if (type == CAType.Peer)
            {
                accountType = "0";
            }
            else if (type == CAType.Admin)
            {
                accountType = "1";
            }
            else if (type == CAType.User)
            {
                accountType = "2";
            }
            else if (type == CAType.Reader)
            {
                accountType = "3";
            }

            var request = new QMBlockClientSDK.Client.AccountRequest()
            {
                Username = username,
                Password = password,
                AccountType = accountType,
                AccountName = accountName
            };
            var rs = await AuthClient.GenerateAccountAsync(request);
            if (rs.Status)
            {
                return new UserAccount
                {
                    PrivateKey = rs.PravateKey,
                    Certificate = Newtonsoft.Json.JsonConvert.DeserializeObject<Certificate>(rs.Certificate)
                };
            }
            else
            {
                //如果发生错误 这里的privateKey是错误消息
                throw new Exception(rs.PravateKey);
            }
        }

        //登记注册账号
        public async Task<bool> RegistAccountAsync(string username, string password, string channelId, Certificate ca)
        {
            var request = new Client.RegistRequest
            {
                Certificate = Newtonsoft.Json.JsonConvert.SerializeObject(ca),
                ChannelId = channelId,
                Username = username,
                Password = password
            };

            var rs = await AuthClient.RegistAsync(request);
            if (rs.Status)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

    }
}
