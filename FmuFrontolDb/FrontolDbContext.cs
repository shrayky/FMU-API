using FmuApiDomain.Frontol;
using FmuApiSettings;
using Microsoft.EntityFrameworkCore;

namespace FmuFrontolDb
{
    public class FrontolDbContext : DbContext
    {
        private readonly string _connectionString = string.Empty;

        public DbSet<Sprt> Sprts { get; set; }
        public DbSet<Barcode> Barcodes { get; set; }
        public DbSet<PrintGroup> PrintGroups { get; set; }


        public FrontolDbContext()
        {
            _connectionString = Constants.Parametrs.FrontolConnectionSettings.ConnectionStringBuild();
            
        }
        public FrontolDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            if (_connectionString != "")
                optionsBuilder.UseFirebird(_connectionString);
             
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Sprt>()
                .HasKey(c => new { c.Id });

            modelBuilder.Entity<Barcode>()
                .HasKey(c => new { c.Id });

            modelBuilder.Entity<PrintGroup>()
                .HasKey(c => new { c.Id });
        }
    }
}
