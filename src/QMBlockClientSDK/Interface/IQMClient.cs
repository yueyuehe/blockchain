using QMBlockSDK.TX;
using System.Threading.Tasks;

namespace QMBlockClientSDK.Interface
{
    public interface IQMClient
    {

        Imp.ChannelClient GetChannel(string channelId);

        Task<TxResponse> CreateNewChannel(string channelId);

        Task<TxResponse> JoinChannel(string channelId);

    }
}
