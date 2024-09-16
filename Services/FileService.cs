using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Diagnostics;

namespace TESS_GHOSTSTRIP_TRIAL.Services
{
    public interface IFileService
    {
        Task<byte[]> PdfGhostscriptCompressionAsync(string inputFilePath);
        Task<byte[]> ImageSharpCompressionAsync(Stream imageStream, int maxWidth, int maxHeight, int quality);
    }

    public class FileService : IFileService
    {
        //PDF Compression
        public async Task<byte[]> PdfGhostscriptCompressionAsync(string filePath)
        {
            return await Task.Run(async () =>
            {
                //Path of Ghostscript
                string ghostscriptPath = Path.Combine(Environment.CurrentDirectory, "gs", "gs10.03.1", "bin", "gswin64c.exe");

                // Define file paths for temporary files
                string tempFilePath = Path.GetTempFileName();

                try
                {
                    // Prepare the Ghostscript command arguments based on your bash function
                    string arguments = $"-q -dNOPAUSE -dBATCH -dSAFER -sDEVICE=pdfwrite " +
                                       $"-dPDFSETTINGS=/screen " +
                                       $"-dEmbedAllFonts=true " +
                                       $"-dSubsetFonts=true " +
                                       $"-dColorImageDownsampleType=/Bicubic -dColorImageResolution=120 " + //Adjust resolution
                                       $"-dGrayImageDownsampleType=/Bicubic -dGrayImageResolution=120 " + //Adjust resolution
                                       $"-dMonoImageDownsampleType=/Bicubic -dMonoImageResolution=120 " + //Adjust resolution
                                       $"-sOutputFile=\"{tempFilePath}\" \"{filePath}\"";

                    // Setup the process start info for Ghostscript
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = ghostscriptPath,
                        Arguments = arguments,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = new Process { StartInfo = processStartInfo })
                    {
                        process.Start();


                        // Capture the output and error logs
                        string output = await process.StandardOutput.ReadToEndAsync();
                        string error = await process.StandardError.ReadToEndAsync();
                        
                        // Wait for the process to complete
                        await process.WaitForExitAsync();

                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"Ghostscript failed with error: {error}");
                        }

                        // Read the compressed PDF into a byte array
                        using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
                        using (var memoryStream = new MemoryStream())
                        {
                            await fileStream.CopyToAsync(memoryStream);
                            return memoryStream.ToArray();
                        }
                    }
                }
                finally
                {
                    // Clean up temporary files
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
            });
        }

        //Image Compression
        public async Task<byte[]> ImageSharpCompressionAsync(Stream imageStream, int maxWidth, int maxHeight, int quality)
        {
            return await Task.Run(() =>
            {
                imageStream.Position = 0;

                using (var image = Image.Load(imageStream))
                {
                    // Resize image
                    image.Mutate(x => x.Resize(maxWidth, maxHeight));

                    // Compress and save to memory stream
                    using (var outputStream = new MemoryStream())
                    {
                        var encoder = new JpegEncoder
                        {
                            Quality = quality
                        };
                        image.Save(outputStream, encoder);

                        return outputStream.ToArray();
                    }
                }
            });
        }
    }
}
