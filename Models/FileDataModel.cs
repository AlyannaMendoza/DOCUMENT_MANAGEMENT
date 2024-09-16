namespace TESS_GHOSTSTRIP_TRIAL.Models
{
    public class FileData
    {
        public int Id { get; set; }
        public byte[] Data { get; set; }

        public int FileMetadataId { get; set; }
        public FileMetadata FileMetadata { get; set; }
    }
}
