
using QMBlockClientSDK.Model;
using QMBlockSDK.Config;
using QMBlockSDK.TX;
using System.Threading.Tasks;

/**
 * 初始化网络
 * 
 * 添加组织
 * 添加组织节点
 * 
 * 打包上传链码
 * 安装链码
 * 初始化链码
 **/
namespace QMBlockClientSDK.Service.Interface
{
    public interface IConfigService
    {
        #region Invoke  

        Task<TxResponse> InitChannel(string channelId);

        Task<TxResponse> JoinChannel(string channelId);

        Task<TxResponse> AddOrg(string channelId, OrgConfig config);

        Task<TxResponse> AddOrgMember(OrgMemberConfig config);

        Task<TxResponse> InstallChaincode(string channelId, ChaincodeModel config);

        Task<TxResponse> InitChaincode(string channelId, string name, string[] args);

        #endregion

        #region Query

        #endregion


       

    }
}
