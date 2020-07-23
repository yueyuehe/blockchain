using Microsoft.EntityFrameworkCore;

namespace QMBlockSDK.DAL
{

    public class BlockContext : DbContext
    {
        public BlockContext(DbContextOptions<BlockContext> options)
            : base(options)
        {
        }
        public DbSet<KeyValueData> KeyValueData { get; set; }

        public DbSet<BlockEntity> BlockEntity { get; set; }

    }
}
