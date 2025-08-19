using AskTrackApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AskTrackApi.Data
{
    public class RemkDataContext : DbContext
    {
        public RemkDataContext(DbContextOptions<RemkDataContext> options) : base(options) { }

        public DbSet<Installer> Installers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Installer>().ToTable("installers");
        }
    }
}
