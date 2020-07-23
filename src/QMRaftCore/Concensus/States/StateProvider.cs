using Microsoft.Extensions.Logging;
using QMRaftCore.Concensus.Node;
using QMRaftCore.Data;
using QMRaftCore.QMProvider;

namespace QMRaftCore.Concensus.States
{
    public class StateProvider
    {
        private readonly Candidate _candidate;
        private readonly Follower _follower;
        private readonly Leader _leader;
        public StateProvider(
            IConfigProvider config,
            INode node,
            ILoggerFactory loggerFactory,
            IBlockDataManager blockDataManager)
        {
            _candidate = new Candidate(config, node, loggerFactory, blockDataManager);
            _follower = new Follower(config, node, loggerFactory, blockDataManager);
            _leader = new Leader(config, node, loggerFactory, blockDataManager);
        }

        public Candidate GetCandidate(CurrentState state)
        {
            _follower.Stop();
            _leader.Stop();
            state.VotedFor = "";
            state.LeaderId = "";
            _candidate.Reset(state);
            return _candidate;
        }
        public Follower GetFollower(CurrentState state)
        {
            _candidate.Stop();
            _leader.Stop();
            state.VotedFor = "";
            state.LeaderId = "";
            _follower.Reset(state);
            return _follower;
        }
        public Leader GetLeader(CurrentState state)
        {
            _follower.Stop();
            _candidate.Stop();
            _leader.Reset(state);
            return _leader;
        }

    }
}
