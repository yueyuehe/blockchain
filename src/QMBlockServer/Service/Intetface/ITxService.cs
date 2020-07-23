using QMBlockSDK.TX;
using QMRaftCore.Concensus.Messages;
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
namespace QMBlockServer.Service.Intetface
{
    public interface ITxService
    {
        
        Task<TxResponse> QueryTx(TxHeader request);

        Task<TxResponse> InvokeTx(TxHeader request);

        Task<TxResponse> InitChannel(TxHeader request);

        Task<TxResponse> JoinChannel(TxHeader request);

        Task<EndorseResponse> Endorse(EndorseRequest request);

        Task<TxResponse> Transaction(TxRequest request);

        Task<HandOutResponse> BlockHandOut(HandOutRequest request);
    }
}
