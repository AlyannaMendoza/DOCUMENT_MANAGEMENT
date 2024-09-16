namespace TESS_GHOSTSTRIP_TRIAL.Models
{
    public class FileMetadata
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long Size { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }

        public int OcrResultId { get; set; }
        public OcrResult OcrResult { get; set; }

        public string FormattedSize
        {
            get
            {
                if (Size < 1024)
                    return $"{Size} B";  // Less than 1 KB
                else if (Size < 1024 * 1024)
                    return $"{Size / 1024.0:F2} KB";  // Less than 1 MB
                else if (Size < 1024 * 1024 * 1024)
                    return $"{Size / (1024.0 * 1024.0):F2} MB";  // Less than 1 GB
                else
                    return $"{Size / (1024.0 * 1024.0 * 1024.0):F2} GB";  // More than 1 GB
            }
        }
    }
}
