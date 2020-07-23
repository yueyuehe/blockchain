using QMBlockSDK.Ledger;
using System.Threading.Tasks;

namespace QMRaftCore.QMProvider
{
    public interface ITxPool
    {
        Task AddAsync(Envelope tx);

    }
}
