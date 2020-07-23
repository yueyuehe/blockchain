using QMBlockSDK.Config;
using QMBlockSDK.Idn;
using QMBlockSDK.TX;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QMBlockServer.Service.Intetface
{
    public interface IInvokeBLL
    {
        Task<TxResponse> RegistMember(string channelId, OrgMemberConfig ca);
    }
}
