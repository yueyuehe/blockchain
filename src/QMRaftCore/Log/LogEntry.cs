using QMRaftCore.FiniteStateMachine;
using System;

namespace QMRaftCore.Log
{

    public class LogEntry
    {
        public LogEntry(ICommand commandData, Type type, long term)
        {
            CommandData = commandData;
            Type = type;
            Term = term;
        }
        public LogEntry()
        {

        }

        public ICommand CommandData { get; private set; }
        public Type Type { get; private set; }
        public long Term { get; private set; }

        public string CurrentHash { get; set; }
        public string PreviousHash { get; set; }
        public long Height { get; set; }
        public long LogTerm { get; set; }

    }
}