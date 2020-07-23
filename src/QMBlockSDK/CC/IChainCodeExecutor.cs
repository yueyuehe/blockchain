using QMBlockSDK.TX;
using System.Threading.Tasks;

namespace QMBlockSDK.CC
{
    public interface IChainCodeExecutor
    {
        Task<ChainCodeInvokeResponse> Submit(TxRequest request);

        Task<ChainCodeInvokeResponse> ChaincodeInvoke(TxRequest request);

        Task<ChainCodeInvokeResponse> ChaincodeQuery(TxRequest request);

        Task<ChainCodeInvokeResponse> ChaincodeInit(TxRequest request);


    }
}
