using QMBlockSDK.Config;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QMBlockServer.Service.Intetface
{
    public interface IQueryBLL
    {
        Task<ChannelConfig> QueryChannelConfigAsync(string channelId);
    }
}
