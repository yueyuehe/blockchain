using QMRaftCore.Concensus.Node;

namespace QMRaftCore.QMProvider
{
    public interface INodeProvider
    {
        /// <summary>
        /// 根据通道ID获取Peer节点在通道中的Node实例
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        INode GetNode(string channelId);
    }
}
