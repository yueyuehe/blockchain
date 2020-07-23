namespace QMRaftCore
{
    public class Config
    {
        //选取客户端名称
        public const string VoteHttpClientName = "";

        public static QMRaftCore.Concensus.Node.Node Node;
    }

    public static class CacheKeys
    {
        public static string NodePeer { get { return "_NodePeer"; } }
    }


    /**
     * -----------
     * 区块头 
     *  -任期
     *  -区块高度
     *  -到期hash
     *  -前一个hash
     * -----------
     *
     * 区块交易数据
     * --交易1
     *      --交易方
     *      --链码 信息等
     * --交易2
     * --交易3
     * -----------
     * 出块组织签名
     * 
     * -----------
     *
     *
     * 
     * -----------
     */
    /**
     * leader节点出块  发送给其他peer节点，peer 节点对区块做校验，校验成功
     * 返回给leader结果，leader对结果收集，符合多数人的决定，则区块上链成功上链完成
     * 
     * 一定要leader节点的状态是最新的，leader节点接受交易，
     * 然后给通道中的背书者发生背书请求，
     * 需要背书者的状态也是最新的，
     * 
     * leader节点发送心跳消息，以及最新日志信息，不发送日志具体数据，因为数据可能比较大
     * follower节点接受心跳消息,并且自动向leader节点请求日志消息
     * （心跳消息只是通知follower节点 我是leader,）
     *  (fo
llower节点自动根据自身的日志数据高度向leader节点获取区块数据，并且验证区块数据)
     *  (区块数据验证失败，则重新发起选举)
     * 
     *      * 
     **/
    /**
     * 创世区块
     *     节点 以及 公钥
     * 
     * 
     * 
     * 
     */

}
