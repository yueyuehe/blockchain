using QMBlockSDK.Ledger;
using System.Threading.Tasks;

namespace QMRaftCore.QMProvider
{
    public interface ITxPool
    {
        void Add(Envelope tx);

        string GetTxStatus(string txid);
    }
}
