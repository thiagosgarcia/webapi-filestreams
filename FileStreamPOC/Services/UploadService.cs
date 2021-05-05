using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FileStreamPOC.Services
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
            if (file == null)
                throw new ArgumentNullException(nameof(file));

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