using QMBlockClientSDK.Imp;
using QMBlockClientSDK.Service.Interface;

namespace QMBlockClientSDK.Interface
{
    public interface IQMClientFactory
    {
        ChannelClient GetChannel(string channelId);

        ICaService GetCaService();
    }
}
