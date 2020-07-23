using QMBlockSDK.TX;
using System.Threading.Tasks;

namespace QMBlockClientSDK.Interface
{

    /**负责连接CRPC Server端的，主要用于ClientSDK连接，
     * 对交易执行，
     * 对配置修改，
     * 对账号的管理等
     */

    public interface IGrpcClient
    {
        Task<TxResponse> TxInvoke(TxHeader request);
        Task<TxResponse> TxQuery(TxHeader request);

        /// <summary>
        /// 用于登录与服务端的链接 获取token
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="canumber"></param>
        /// <returns></returns>
        Task<string> LoginAsync(string pk, string canumber);

        /// <summary>
        /// 用于判断当前client是否已经成功连接到server
        /// </summary>
        /// <returns></returns>
        Task<bool> IsLoginAsync();

        /// <summary>
        /// 刷新token 用于防止token过期
        /// </summary>
        /// <param name="pk"></param>
        /// <param name="canumber"></param>
        /// <returns></returns>
        Task<bool> RefreshTokenAsync();

        //创建用户
        Task<QMBlockSDK.Idn.UserAccount> GenerateAccountAsync(string username, string password, QMBlockSDK.Idn.CAType type, string accountName);

        //登记注册账号
        Task<bool> RegistAccountAsync(string username, string password, string channelId, QMBlockSDK.Idn.Certificate ca);


    }
}
