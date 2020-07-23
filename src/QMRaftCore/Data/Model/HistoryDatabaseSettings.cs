using System;
using System.Collections.Generic;
using System.Text;

namespace QMRaftCore.Data.Model
{
    public class HistoryDatabaseSettings : IHistoryDatabaseSettings
    {
        public string CollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface IHistoryDatabaseSettings
    {
        string CollectionName { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
}
