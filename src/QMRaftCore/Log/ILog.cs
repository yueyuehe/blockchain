﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace QMRaftCore.Log
{
    public interface ILog
    {
        /// <summary>
        /// This will apply a log entry and return its index
        /// </summary>
        Task<int> Apply(LogEntry log);
        /// <summary>
        /// This will return the log entry at the index passed in
        /// </summary>
        Task<LogEntry> Get(int index);
        /// <summary>
        /// This will return all the log entries from a certain point based on index including the first match on the index passed in
        /// </summary>
        Task<List<(int index, LogEntry logEntry)>> GetFrom(int index);
        /// <summary>
        /// This will return the last known log index or 1
        /// </summary>
        Task<int> LastLogIndex();
        /// <summary>
        /// This will return the last know log term or 0
        /// </summary>
        Task<long> LastLogTerm();
        /// <summary>
        /// This will get the term at the index passed in
        /// </summary>
        Task<long> GetTermAtIndex(int index);
        /// <summary>
        /// This will delete any conflicts from the log, if the log entry passed in doesnt match the log entry
        //in the log for the given index it will also delete any further logs
        /// </summary>
        Task DeleteConflictsFromThisLog(int index, LogEntry logEntry);

        /// <summary>
        /// This says if the log passed it at the given index is already in the log
        /// </summary>
        Task<bool> IsDuplicate(int index, LogEntry logEntry);
        /// <summary>
        /// This returns a count of the logs
        /// </summary>
        Task<int> Count();
        /// <summary>
        /// This removes the command at the index passed in.
        /// </summary>
        Task Remove(int indexOfCommand);


        #region 新增的方法

        /// <summary>
        /// 获取最后一个区块
        /// </summary>
        /// <returns></returns>
        Task<LogEntry> GetLastLogEntry();

        /// <summary>
        /// 根据区块高度获取区块
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        Task<LogEntry> GetLogEntry(long height);

        /// <summary>
        /// 根据区块高度获取区块
        /// </summary>
        /// <param name="heights"></param>
        /// <returns></returns>
        Task<Dictionary<long, LogEntry>> GetLogEntrys(long[] heights);

        /// <summary>
        /// 保存区块
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        Task<bool> SaveLogEntrys(LogEntry log);




        #endregion


    }
}