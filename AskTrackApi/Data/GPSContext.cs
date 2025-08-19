using Microsoft.EntityFrameworkCore;
using AskTrackApi.Models.GPS;

namespace AskTrackApi.Data
{
    public class GPSContext : DbContext
    {
        public GPSContext(DbContextOptions<GPSContext> options) : base(options) { }

        public DbSet<UserInfo> UserInfos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserInfo>().ToTable("UserInfo");
        }
    }
}
