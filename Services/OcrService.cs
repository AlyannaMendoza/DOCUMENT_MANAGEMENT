using TESS_GHOSTSTRIP_TRIAL.Data;
using Tesseract;
using UglyToad.PdfPig;
using static TESS_GHOSTSTRIP_TRIAL.Services.OcrService;

namespace TESS_GHOSTSTRIP_TRIAL.Services
{
    public interface IOcrService
    {
        Task<string> ExtractTextFromPdfpigAsync(Stream pdfStream);
        Task<PerformTesseract> TesseractOcrAndSearchablePdfAsync(Stream imageStream, string fileName);
    }

    public class OcrService : IOcrService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _tessdataPath;

        public OcrService(ApplicationDbContext context)
        {
            _context = context;
            _tessdataPath = Path.Combine(Environment.CurrentDirectory, "tessdata");
        }

        public async Task<string> ExtractTextFromPdfpigAsync(Stream pdfStream)
        {
            return await Task.Run(() =>
            {
                pdfStream.Position = 0;

                using (var document = PdfDocument.Open(pdfStream))
                {
                    var text = string.Empty;
                    for (int i = 0; i < document.NumberOfPages; i++)
                    {
                        var page = document.GetPage(i + 1);
                        text += $"Page {i + 1}:\n{page.Text}\n\n";
                    }
                    return text;
                }
            });
        }

        public class PerformTesseract
        {
            public byte[] PdfData { get; set; }
            public string PdfFileName { get; set; }
            public string OcrText { get; set; }
        }

        public async Task<PerformTesseract> TesseractOcrAndSearchablePdfAsync(Stream imageStream, string fileName)
        {
            return await Task.Run(() =>
            {
                imageStream.Position = 0;

                string ocrText;

                using (var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromMemory(((MemoryStream)imageStream).ToArray()))
                    {
                        using (var page = engine.Process(img))
                        {
                            ocrText = page.GetText();
                        }
                    }
                }

                imageStream.Position = 0;

                //string tempFolder = Path.Combine(Path.GetTempPath(), "TesseractPdfs");
                string tempFilePath = Path.GetTempFileName();

                var imageName = Path.GetFileNameWithoutExtension(fileName);

            //if (!Directory.Exists(tempFilePath))
            //{
            //    Directory.CreateDirectory(tempFilePath);
            //}

            //string outputPath = Path.Combine(tempFilePath, $"searchable_{Guid.NewGuid()}.pdf");

            try
            {
                using (IResultRenderer renderer = ResultRenderer.CreatePdfRenderer(tempFilePath, _tessdataPath, false))
                {
                    using (renderer.BeginDocument(imageName))
                    {
                        using (TesseractEngine engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default))
                        {
                            byte[] imageData = ((MemoryStream)imageStream).ToArray();

                            using (var img = Pix.LoadFromMemory(imageData))
                            {
                                using (var page = engine.Process(img, imageName))
                                {
                                    renderer.AddPage(page);
                                }
                            }
                        }
                    }
                }

                    using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                    using (var memoryStream = new MemoryStream())
                    {
                       fileStream.CopyToAsync(memoryStream);
                        return new PerformTesseract
                        {
                            OcrText = ocrText,
                            PdfData = memoryStream.ToArray(),
                            PdfFileName = imageName
                        };
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to create searchable PDF.", ex);
                }
                finally
                {
                    //Clean up temporary file
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
            });
        }
    }
}
