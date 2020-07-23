using QMBlockClientSDK.Interface;
using QMBlockClientSDK.Service.Interface;
using QMBlockSDK.Idn;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QMBlockClientSDK.Service.Imp
{
    public class CaService : ICaService
    {
        private readonly IGrpcClient _client;
        public CaService(IGrpcClient client)
        {
            _client = client;
        }

        /// <summary>
        /// 创建证书
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="type"></param>
        /// <param name="accountName"></param>
        /// <returns></returns>
        public async Task<UserAccount> CreateCertificateAsync(string username, string password, CAType type, string accountName)
        {
            var account = await _client.GenerateAccountAsync(username, password, type, accountName);
            return account;
        }

        /// <summary>
        /// 注册用户
        /// </summary>
        /// <param name="username">ca账号</param>
        /// <param name="password">ca密码</param>
        /// <param name="channelId">通道ID</param>
        /// <param name="ca">ca证书</param>
        /// <returns></returns>
        public async Task<bool> RegistAccountAsync(string username, string password, string channelId, Certificate ca)
        {
            return await _client.RegistAccountAsync(username, password, channelId, ca);
        }

    }
}
