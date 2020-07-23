using QMBlockSDK.CC;
using System;
using System.IO;
using System.Reflection;

namespace QMRaftCore.QMProvider.Imp
{
    /// <summary>
    /// 程序集提供程序
    /// </summary>
    public class AssemblyProvider : IAssemblyProvider
    {
        public Assembly GetAssembly(string channelID, string name, string namespeac, string version)
        {
            try
            {
                var basepath = AppContext.BaseDirectory;
                var path = Path.Combine(basepath, ConfigKey.ChaincodePath, channelID, name, namespeac, version, namespeac + ".dll");
                return Assembly.LoadFrom(path);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
