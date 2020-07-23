using QMBlockClientSDK.Interface;
using QMBlockClientSDK.Service.Interface;

namespace QMBlockClientSDK.Imp
{
    public class QMClientFactory : IQMClientFactory
    {

        public readonly ITxService _txService;
        public readonly IConfigService _configService;
        public readonly ICaService _caService;

        public QMClientFactory(ITxService txService, IConfigService configService, ICaService caService)
        {
            _txService = txService;
            _configService = configService;
            _caService = caService;
        }


        #region 获取通道的链接 链接绑定通道 有常用的方法
        public ChannelClient GetChannel(string channelId)
        {
            //判断channel是否存在
            return new ChannelClient(channelId, _txService, _configService);
        }
        #endregion


        #region 获取CA服务器
        //获取ca
        public ICaService GetCaService()
        {
            return _caService;
        }
        #endregion

    }
}
