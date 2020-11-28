using Microsoft.Extensions.Logging;
using QMBlockClientSDK.Interface;
using QMBlockClientSDK.Model;
using QMBlockSDK.Idn;
using QMBlockSDK.TX;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QMBlockClientSDK.Imp
{
    public class WebApiClient : IRequestClient
    {
        private string _token;
        private readonly ILogger<WebApiClient> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        public string Url { get; set; }
        public WebApiClient(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, PeerSetting setting)
        {
            _logger = loggerFactory.CreateLogger<WebApiClient>();
            _loggerFactory = loggerFactory;
            _httpClientFactory = httpClientFactory;
            Url = setting.Url;
        }

        #region 身份认证 账号注册

        /// <summary>
        /// 创建CA账号
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="type"></param>
        /// <param name="accountName"></param>
        /// <returns></returns>
        public Task<UserAccount> GenerateAccountAsync(string username, string password, CAType type, string accountName)
        {




            throw new NotImplementedException();
        }

        /// <summary>
        /// 判断是否登录
        /// </summary>
        /// <returns></returns>
        public Task<bool> IsLoginAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="canumber"></param>
        /// <returns></returns>
        public Task<string> LoginAsync(string pk, string canumber)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 刷新token
        /// </summary>
        /// <returns></returns>
        public Task<bool> RefreshTokenAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 注册用户
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="channelId"></param>
        /// <param name="ca"></param>
        /// <returns></returns>
        public Task<bool> RegistAccountAsync(string username, string password, string channelId, Certificate ca)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region 交易


        /// <summary>
        /// 交易执行
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<TxResponse> TxInvoke(TxHeader request)
        {
            try
            {
                if (string.IsNullOrEmpty(_token))
                {
                    //使用身份登录
                }
                using (var client = _httpClientFactory.CreateClient())
                {
                    HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(request));
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    var response = await client.PostAsync(this.Url + "/api/tx/invoke", content);//改成自己的
                    response.EnsureSuccessStatusCode();//用来抛异常的
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<TxResponse>(responseBody);
                }
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

        /// <summary>
        /// 交易查询
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<TxResponse> TxQuery(TxHeader request)
        {
            try
            {
                if (string.IsNullOrEmpty(_token))
                {
                    //使用身份登录
                }
                using (var client = _httpClientFactory.CreateClient())
                {
                    HttpContent content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(request));
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    var response = await client.PostAsync(this.Url + "/api/tx/query", content);
                    response.EnsureSuccessStatusCode();//用来抛异常的
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<TxResponse>(responseBody);
                }
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
    }
}
