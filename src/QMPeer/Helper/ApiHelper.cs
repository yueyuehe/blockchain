using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QMPeer.Helper
{
    public class ApiHelper
    {
        public static JsonResult Reponse(string msg, object obj = null, bool status = true)
        {
            var data = new { Msg = msg, data = obj, Status = status };
            JsonResult jr = new JsonResult(data);
            return jr;
        }
    }
}
