using Microsoft.EntityFrameworkCore;
using TESS_GHOSTSTRIP_TRIAL.Models;

namespace TESS_GHOSTSTRIP_TRIAL.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FileData> fileData { get; set; }
        public DbSet<FileMetadata> fileMetadata { get; set; }
        public DbSet<OcrResult> ocrResults { get; set; }
    }
}
