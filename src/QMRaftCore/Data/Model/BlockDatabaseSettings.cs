using System;
using System.Collections.Generic;
using System.Text;

namespace QMRaftCore.Data.Model
{
    public class BlockDatabaseSettings : IBlockDatabaseSettings
    {
        public string CollectionName { get; set; }
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }

    public interface IBlockDatabaseSettings
    {
        string CollectionName { get; set; }
        string ConnectionString { get; set; }
        string DatabaseName { get; set; }
    }
}
