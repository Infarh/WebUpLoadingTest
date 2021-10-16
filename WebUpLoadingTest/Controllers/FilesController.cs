using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

using WebUpLoadingTest.Infrastructure.Filters;

namespace WebUpLoadingTest.Controllers
{
    public class FilesController : Controller
    {
        private readonly ILogger<FilesController> _Logger;

        public FilesController(ILogger<FilesController> Logger)
        {
            _Logger = Logger;
        }

        public IActionResult Index() => View();

        private const long MaxFileSize10 = 20L * 1024L * 1024L * 1024L;

        [HttpPost]
        [RequestSizeLimit(MaxFileSize10)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize10)]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> UploadSingle()
        {
            // fsutil file createnew File10GB.txt 10737418240

            if (!Request.ContentType.Contains("multipart/"))
            {
                ModelState.AddModelError("File", "The request couldn't be processed (Error 1).");
                _Logger.LogWarning("Поступивший запрос на загрузку файла не содержит нужного набора частей");
                return BadRequest(ModelState);
            }
            _Logger.LogInformation("Запрос на загрузку {0}", Request.ContentType);

            var header_content = MediaTypeHeaderValue.Parse(Request.ContentType);

            var boundary = header_content.Boundary is { Length: > 2 } b && b[0] == '"' && b[^1] == '"'
                ? b.Subsegment(1, b.Length - 2)
                : header_content.Boundary;

            var boundary_value = boundary.Value;
            _Logger.LogInformation("Область загрузки {0}", boundary_value);
            var reader = new MultipartReader(boundary_value, HttpContext.Request.Body);

            while (await reader.ReadNextSectionAsync() is { Body: var body } section)
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var position))
                {
                    if (position is not { DispositionType: { Value: "form-data" }, FileName: { Length: > 0 } })
                    {
                        ModelState.AddModelError("File", "The request couldn't be processed (Error 2).");
                        _Logger.LogWarning("В поступившем запросе отсутствует информация о положении файла в потоке");
                        return BadRequest(ModelState);
                    }

                    _Logger.LogInformation("Загружается файл {0}", position.FileName.Value);

                    var body_length = body.Length;
                    _Logger.LogInformation("Для загрузки в потоке {0} байт", body_length);

                    try
                    {
                        var buffer = new byte[1024 * 1024 * 20];
                        var total_readed = 0;
                        var i = 0;
                        int readed;
                        do
                        {
                            readed = await body.ReadAsync(buffer, 0, buffer.Length);
                            total_readed += readed;
                            if (i++ % 20 == 0)
                                _Logger.LogInformation("Прочитано {0} байт", total_readed);
                            //if (i == 100) await Task.Delay(5000);
                        }
                        while (readed > 0);
                    }
                    catch (BadHttpRequestException)
                    {
                        return BadRequest();
                    }
                }

            return CreatedAtAction("Index", "Home", null);
        }
    }
}
