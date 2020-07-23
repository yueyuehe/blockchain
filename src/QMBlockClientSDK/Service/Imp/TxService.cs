using QMBlockClientSDK.Interface;
using QMBlockClientSDK.Service.Interface;
using QMBlockSDK.TX;
using System.Threading.Tasks;

namespace QMBlockClientSDK.Service.Imp
{
    public class TxService : ITxService
    {
        private readonly IGrpcClient _client;

        public TxService(IGrpcClient client)
        {
            _client = client;
        }

        public async Task<TxResponse> InvokeTx(TxHeader request)
        {
            request.Type = TxType.Invoke;
            return await _client.TxInvoke(request);
        }

        public async Task<TxResponse> QueryTx(TxHeader request)
        {
            request.Type = TxType.Query;
            return await _client.TxQuery(request);
        }

    }
}
