using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FileStreamPOC.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace FileStreamPOC.Controllers
{
    public class StreamingController : Controller
    {
        //private readonly AppDbContext _context;
        private readonly long _fileSizeLimit = int.MaxValue;
        //2147483647
        //2717863708
        //private readonly ILogger<StreamingController> _logger;
        private readonly string[] _permittedExtensions = { ".txt" }; //not actually used for this example
        private readonly string _targetFilePath = Path.GetTempPath();

        // Get the default form options so that we can use them to set the default 
        // limits for request body data.
        private static readonly FormOptions _defaultFormOptions = new FormOptions();

        //public StreamingController(ILogger<StreamingController> logger,
        //    AppDbContext context, IConfiguration config)
        //{
        //    _logger = logger;
        //    _context = context;
        //    _fileSizeLimit = config.GetValue<long>("FileSizeLimit");

        //    // To save physical files to a path provided by configuration:
        //    _targetFilePath = config.GetValue<string>("StoredFilesPath");

        //    // To save physical files to the temporary files folder, use:
        //    //_targetFilePath = Path.GetTempPath();
        //}

        // The following upload methods:
        //
        // 1. Disable the form value model binding to take control of handling 
        //    potentially large files.
        //
        // 2. Typically, antiforgery tokens are sent in request body. Since we 
        //    don't want to read the request body early, the tokens are sent via 
        //    headers. The antiforgery token filter first looks for tokens in 
        //    the request header and then falls back to reading the body.


        [HttpPost]
        [Route("Streaming")]
        //[DisableFormValueModelBinding]

        //For production code you need to implement a Anti-Forgery handling
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhysical()
        {
            Console.WriteLine($"Receiving stream...");

            var sw = new Stopwatch();
            sw.Start();
            //if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            //{
            //    ModelState.AddModelError("File",
            //        $"The request couldn't be processed (Error 1).");
            //    // Log error

            //    return BadRequest(ModelState);
            //}

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();
            var stored = string.Empty;
            var partCount = 0L;
            var size = 0L;
            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader)
                {
                    // This check assumes that there's a file
                    // present without form data. If form data
                    // is present, this method immediately fails
                    // and returns the model error.
                    if (!MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        ModelState.AddModelError("File",
                            $"The request couldn't be processed (Error 2).");
                        // Log error

                        return BadRequest(ModelState);
                    }
                    else
                    {
                        // Don't trust the file name sent by the client. To display
                        // the file name, HTML-encode the value.
                        var trustedFileNameForDisplay = WebUtility.HtmlEncode(
                            contentDisposition.FileName.Value);
                        //var trustedFileNameForFileStorage = Path.GetRandomFileName();

                        if (HttpContext.Request.Headers.ContainsKey("CorrelationId"))
                            //This example uses a CorrelationId header to indicate that multiple requests are dealing with the same file
                            //This is NOT safe, and should be handled differently in production code - if even used
                             stored = new Guid(HttpContext.Request.Headers["CorrelationId"]).ToString();
                        else if(HttpContext.Request.Headers.ContainsKey("Upload-Metadata"))
                        {
                            stored = HttpContext.Request.Headers["Upload-Metadata"];
                            stored = Regex.Match(stored, "(?:filename )(.+)(?:,filetype.*)").ToString();
                        }
                        else
                            stored = Guid.NewGuid().ToString();

                        var trustedFileNameForFileStorage = stored;

                        // **WARNING!**
                        // In the following example, the file is saved without
                        // scanning the file's contents. In most production
                        // scenarios, an anti-virus/anti-malware scanner API
                        // is used on the file before making the file available
                        // for download or for use by other systems. 
                        // For more information, see the topic that accompanies 
                        // this sample.

                        var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                            section, contentDisposition, ModelState,
                            _permittedExtensions, _fileSizeLimit);

                        ++partCount;
                        size += streamedFileContent.Length;
                        //Console.WriteLine($"Streamed part length {streamedFileContent.Length} - {partCount}");

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }

                        //Stream do Null device
                        //await using var targetStream = Stream.Null;

                        //Stream to temp folder
                        var filename = Path.Combine(_targetFilePath, trustedFileNameForFileStorage);
                        var fileMode = System.IO.File.Exists(filename) ? FileMode.Append : FileMode.Create;

                        await using FileStream targetStream = new FileStream(filename, fileMode);
                        await targetStream.WriteAsync(streamedFileContent);
                    }
                }

                // Drain any remaining section body that hasn't been consumed and
                // read the headers for the next section.
                section = await reader.ReadNextSectionAsync();
            }

            Console.WriteLine($"Received - Parts #: {partCount} / {size/1024}KB - Elapsed {sw.ElapsedMilliseconds}ms");
            return Created(nameof(StreamingController), null);
        }
    }
}