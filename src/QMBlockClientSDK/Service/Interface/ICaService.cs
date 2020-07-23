using QMBlockSDK.Idn;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QMBlockClientSDK.Service.Interface
{
    public interface ICaService
    {
        //创建CA证书
        Task<UserAccount> CreateCertificateAsync(string username, string password, CAType type, string accountName);

        //注册CA证书
        Task<bool> RegistAccountAsync(string username, string password, string channelId, Certificate ca);

    }
}
