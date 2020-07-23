using System.Reflection;

namespace QMRaftCore.QMProvider
{
    public interface IAssemblyProvider
    {
        Assembly GetAssembly(string channelID, string name, string namespeac, string version);
    }
}
