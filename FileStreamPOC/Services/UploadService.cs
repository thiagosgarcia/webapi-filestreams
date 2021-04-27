using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FileStreamPOC.Controllers
{
    public interface IUploadService
    {
        Task UploadToDisk(Stream file, CancellationToken cancellationToken);
    }
    public class UploadService : IUploadService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public UploadService(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task UploadToDisk(Stream file, CancellationToken cancellationToken)
        {
            var fileName = Path.GetTempFileName();
            await using (var fileOnDisk = File.Create(fileName))
            {
                await file.CopyToAsync(fileOnDisk, cancellationToken);
            }
            _contextAccessor.HttpContext.Response.Headers.TryAdd("file-size", file.Length.ToString());
            File.Delete(fileName);
        }
    }
}