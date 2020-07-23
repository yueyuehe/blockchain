using System;
namespace QMRaftCore.Infrastructure
{

    public interface IRandomDelay
    {
        TimeSpan Get(int leastMilliseconds, int maxMilliseconds);
    }
}