using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using WebUpLoadingTest.Handlers.Queries;
using WebUpLoadingTest.Infrastructure;
using WebUpLoadingTest.Infrastructure.Filters;
using WebUpLoadingTest.Models;

namespace WebUpLoadingTest.Controllers
{
    public class HomeController : Controller
    {
        private string _FilesPath = "Files";

        private readonly IMediator _Mediator;
        private readonly ILogger<HomeController> _Logger;

        public HomeController(IMediator Mediator, ILogger<HomeController> logger)
        {
            _Mediator = Mediator;
            _Logger = logger;
        }

        public IActionResult Index() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        private const long MaxFileSize = 1L * 1024L * 1024L * 1024L;

        [DisableFormValueModelBinding]
        [RequestSizeLimit(MaxFileSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
        [ValidateAntiForgeryToken]
        [GenerateAntiforgeryTokenCookie]
        public async Task<IActionResult> ReceiveFile()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File", "The request couldn't be processed (Error 1).");
                _Logger.LogWarning("Поступивший запрос на загрузку файла не содержит нужного набора частей");
                return BadRequest(ModelState);
            }

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                (int)MaxFileSize);

            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            while (await reader.ReadNextSectionAsync() is { } section)
                if (ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var content_disposition))
                {
                    // Здесь проверяется наличие данных в файле.
                    // Если данных в форме нет, немедленно прерываем работу
                    // и устанавливаем сообщение об ошибке в модели
                    if (!MultipartRequestHelper.HasFileContentDisposition(content_disposition))
                    {
                        ModelState.AddModelError("File", "The request couldn't be processed (Error 2).");
                        _Logger.LogWarning("В поступившем запросе отсутствует информация о положении файла в потоке");
                        return BadRequest(ModelState);
                    }

                    // Не доверяем имени файла, передаваемого клиентом.
                    // Для отображения имени файла используется HTML-кодирование строки.
                    var display_file = WebUtility.HtmlEncode(content_disposition!.FileName.Value);

                    var random_file = Path.GetRandomFileName();

                    // **!Внимание!**
                    // В данном примере, файл сохраняется без проверки
                    // его содержимого. В реальном проекте
                    // служба антивируса должна быть использована до того, как файл станет доступным
                    // для скачивания, или использования другими системами. 
                    var processing = new WalidateUploadingFileQuery(section, content_disposition, ModelState);
                    var file_content = await _Mediator.Send(processing);

                    if (!ModelState.IsValid)
                        return BadRequest(ModelState);

                    await using var file_stream = System.IO.File.Create(Path.Combine(_FilesPath, random_file));
                    await file_stream.WriteAsync(file_content);

                    _Logger.LogInformation(
                        "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                        "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                        display_file, _FilesPath, random_file);
                }

            return Created(nameof(HomeController), null);
        }
    }
}
