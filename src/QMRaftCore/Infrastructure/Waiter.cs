using System;
using System.Diagnostics;

namespace QMRaftCore.Infrastructure
{
    using System.Threading.Tasks;

    public class Wait
    {
        public static Waiter WaitFor(int milliSeconds)
        {
            return new Waiter(milliSeconds);
        }
    }

    public class Waiter
    {
        private readonly int _milliSeconds;

        public Waiter(int milliSeconds)
        {
            _milliSeconds = milliSeconds;
        }

        public bool Until(Func<bool> condition)
        {
            var stopwatch = Stopwatch.StartNew();
            var passed = false;
            while (stopwatch.ElapsedMilliseconds < _milliSeconds)
            {
                if (condition.Invoke())
                {
                    passed = true;
                    break;
                }
            }

            return passed;
        }

        public async Task<bool> Until(Func<Task<bool>> condition)
        {
            var stopwatch = Stopwatch.StartNew();
            var passed = false;
            while (stopwatch.ElapsedMilliseconds < _milliSeconds)
            {
                if (await condition.Invoke())
                {
                    passed = true;
                    break;
                }
            }

            return passed;
        }

        public bool Until<T>(Func<bool> condition)
        {
            var stopwatch = Stopwatch.StartNew();
            var passed = false;
            while (stopwatch.ElapsedMilliseconds < _milliSeconds)
            {
                if (condition.Invoke())
                {
                    passed = true;
                    break;
                }
            }

            return passed;
        }
    }
}
