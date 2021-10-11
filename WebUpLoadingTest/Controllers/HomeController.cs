using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        private readonly IWebHostEnvironment _HostingEnvironment;

        public HomeController(IWebHostEnvironment HostingEnvironment, IMediator Mediator, ILogger<HomeController> logger)
        {
            _Mediator = Mediator;
            _Logger = logger;
            _HostingEnvironment = HostingEnvironment;
        }

        public IActionResult Index()
        {
            var upload = _HostingEnvironment.WebRootFileProvider.GetDirectoryContents("Uploads");
            var upload_exists = ViewBag.UploadsExist = upload.Exists;
            if (upload_exists) ViewBag.UploadFiles = upload.Select(f => f.Name);
            return View();
        }

        [HttpPost]
        public IActionResult DeleteUploadedFile(string FileName)
        {
            var upload = _HostingEnvironment.WebRootFileProvider.GetDirectoryContents("Uploads");
            if (upload.Exists)
            {
                if (upload.FirstOrDefault(f => f.Name == FileName) is not { } file)
                    return View(nameof(Index));
                var server_file = new FileInfo(file.PhysicalPath);
                server_file.Delete();
            }

            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

        private const long MaxFileSize = 1L * 1024L * 1024L * 1024L;

        [DisableFormValueModelBinding]
        [RequestSizeLimit(MaxFileSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
        [ValidateAntiForgeryToken]
        [GenerateAntiforgeryTokenCookie]
        public async Task<IActionResult> Upload()
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

        private const long MaxFileSize10 = 10L * 1024L * 1024L * 1024L;

        [HttpPost]
        [RequestSizeLimit(MaxFileSize10)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize10)]
        public async Task<IActionResult> UploadSimple(IList<IFormFile> files)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            foreach (var source in files)
            {
                var disposition = ContentDispositionHeaderValue.Parse(source.ContentDisposition);
                var filename = disposition.FileName.Value.Trim('"');

                filename = Path.GetFileName(filename);

                var server_file = new FileInfo(Path.Combine(_HostingEnvironment.WebRootPath, "Uploads", filename));
                if(!server_file.Directory!.Exists) server_file.Directory.Create();
                await using var output = server_file.Create();
                await source.CopyToAsync(output);
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult SingleFileUpload() => View();
    }
}
