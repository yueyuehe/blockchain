using System;
using System.Collections.Generic;

namespace QMBlockSDK.Config
{
    public class Policy
    {
        public Policy()
        {
            OrgIds = new List<string>();
        }
        public List<string> OrgIds { get; set; }

        public Int32 Number { get; set; }
    }
}
