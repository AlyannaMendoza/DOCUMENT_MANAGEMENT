using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TESS_GHOSTSTRIP_TRIAL.Data;
using TESS_GHOSTSTRIP_TRIAL.Models;
using TESS_GHOSTSTRIP_TRIAL.Services;

namespace TESS_GHOSTSTRIP_TRIAL.Controllers
{
    public class FilesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IOcrService _ocrService;
        private readonly IFileService _fileService;

        public FilesController(ApplicationDbContext context, IOcrService ocrService, IFileService fileService)
        {
            _context = context;
            _ocrService = ocrService;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchQuery)
        {
            var filesQuery = _context.fileMetadata.AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                filesQuery = filesQuery
                    .Include(f => f.OcrResult)
                    .Where(f => f.FileName.Contains(searchQuery) ||
                                (f.OcrResult != null && f.OcrResult.ExtractedText.Contains(searchQuery)));
            }

            var files = await filesQuery.ToListAsync();

            // Pass the searchString to the view using ViewData
            ViewData["CurrentFilter"] = searchQuery;

            return View(files);
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("File", "File is empty or not selected.");
                return View(); // Return with error message
            }

            var allowedContentTypes = new List<string> { "application/pdf", "image/jpeg", "image/jpg" };

            if (!allowedContentTypes.Contains(file.ContentType))
            {
                ModelState.AddModelError("File", "Unsupported file type.");
                return View(); // Return with error message
            }

            var uploadDate = DateTime.Now;
            var monthFolder = $"{uploadDate.ToString("MMMM")}_{uploadDate.Year.ToString()}";
            var dayFolder = $"{uploadDate.Month}-{uploadDate.Day}-{uploadDate.Year.ToString()}";

            var folderPath = Path.Combine(
               Directory.GetCurrentDirectory(),
               "wwwroot",
               "Documents Uploaded",
               uploadDate.Year.ToString(),
               monthFolder,
               dayFolder
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            var uniqueFileName = $"{timestamp}_{Guid.NewGuid()}_{file.Name}";
            var filePath = Path.Combine(folderPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            byte[] fileData;
            string ocrResult = string.Empty;
            string fileName = file.FileName;

            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);

                if (file.ContentType == "application/pdf")
                {
                    ocrResult = await _ocrService.ExtractTextFromPdfpigAsync(memoryStream);
                    fileData = await _fileService.PdfGhostscriptCompressionAsync(filePath);
                }
                else
                {
                    var performTesseract= await _ocrService.TesseractOcrAndSearchablePdfAsync(memoryStream, file.FileName);
                    ocrResult = performTesseract.OcrText;
                    fileName = performTesseract.PdfFileName;
                    fileData = performTesseract.PdfData;

                    fileData = await _fileService.ImageSharpCompressionAsync(memoryStream, 600, 800, 50);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await stream.WriteAsync(fileData, 0, fileData.Length);
                }

                // Save OCR result
                var ocrResultRecord = new OcrResult
                {
                    ExtractedText = ocrResult
                };

                // Save metadata and file data
                var uploadedFileMetadata = new FileMetadata
                {
                    FileName = fileName,
                    ContentType = file.ContentType,
                    Size = new FileInfo(filePath).Length,
                    FilePath = filePath,
                    UploadDate = uploadDate,
                    OcrResult = ocrResultRecord
                };

                var uploadedFileData = new FileData
                {
                    Data = fileData,
                    FileMetadata = uploadedFileMetadata
                };

                // Add ocr result, file metadata and file data to the database
                _context.ocrResults.Add(ocrResultRecord);
                _context.fileMetadata.Add(uploadedFileMetadata);
                _context.fileData.Add(uploadedFileData);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var file = await _context.fileMetadata
                .Include(f => f.OcrResult)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (file == null)
            {
                return NotFound();
            }

            return View(file);
        }

        [HttpGet]
        public async Task<IActionResult> GetFile(int id)
        {
            var fileData = await _context.fileData
                .Include(fd => fd.FileMetadata)
                .FirstOrDefaultAsync(fd => fd.FileMetadataId == id);

            if (fileData == null)
            {
                return NotFound();
            }

            var contentType = fileData.FileMetadata.ContentType;
            var fileName = fileData.FileMetadata.FileName;

            Response.Headers.Append("Content-Disposition", $"inline; filename={fileName}");
            return File(fileData.Data, contentType);

            //if (contentType.StartsWith("image/"))
            //{
            //    // Create searchable PDF from image data
            //    //using (var memoryStream = new MemoryStream(fileData.Data))
            //    //{
            //    //    //var pdfBytes = await _ocrService.ImageToSearchablePdfAsync(memoryStream, fileName);

            //    //    // Set the response to download the PDF
            //    //    Response.Headers.Append("Content-Disposition", $"attachment; filename={Path.GetFileNameWithoutExtension(fileName)}.pdf");
            //    //    return File(pdfBytes, "application/pdf");
            //    //}
        }
    }
}
