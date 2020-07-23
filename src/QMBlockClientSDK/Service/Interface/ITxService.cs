using QMBlockSDK.TX;
using System.Threading.Tasks;

/**
 * 交易相关
 * 
 * 接收交易请求 
 *  如果当前节点不是leader节点转发到leader节点 
 *  
 * 接收背书请求
 *  
 * 接受区块验证以及提交
 * 
 * 
 * 
 * 
 * 
 * 
 */
namespace QMBlockClientSDK.Service.Interface
{
    public interface ITxService
    {
        Task<TxResponse> QueryTx(TxHeader request);

        Task<TxResponse> InvokeTx(TxHeader request);

    }
}
