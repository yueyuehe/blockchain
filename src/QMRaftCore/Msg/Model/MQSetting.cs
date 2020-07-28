using System;
using System.Collections.Generic;
using System.Text;

namespace QMRaftCore.Msg.Model
{

    public class MQSetting
    {
        public string Host { get; set; }
        public Int32 Port { get; set; }
        public string UserName { get; internal set; }
        public string Password { get; internal set; }
        public string VirtualHost { get; internal set; }
    }
}
