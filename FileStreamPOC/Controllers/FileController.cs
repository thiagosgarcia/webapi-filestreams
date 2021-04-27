using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileStreamPOC.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FormFileController : ControllerBase
    {

        [HttpPost]
        [Route("")]
        public Task Upload(IFormFile file, [FromServices] IUploadService service, CancellationToken cancellationToken = default)
            => service.UploadToDisk(file.OpenReadStream(), cancellationToken);
    }

    [ApiController]
    [Route("[controller]")]
    public class ByteArrayController : ControllerBase
    {

        [HttpPost]
        [Route("")]
        public async Task Upload([FromServices] IUploadService service,
            CancellationToken cancellationToken = default)
        {
            var str = new MemoryStream();
            await HttpContext.Request.Body.CopyToAsync(str, cancellationToken);
            await service.UploadToDisk(str, cancellationToken); 

        }
    }
}