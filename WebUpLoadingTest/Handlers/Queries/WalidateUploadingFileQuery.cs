using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace WebUpLoadingTest.Handlers.Queries
{
    public record WalidateUploadingFileQuery(MultipartSection Section, ContentDispositionHeaderValue ContentDisposition, ModelStateDictionary ModelState)
        : IRequest<ReadOnlyMemory<byte>>
    {
        public class Handler : IRequestHandler<WalidateUploadingFileQuery, ReadOnlyMemory<byte>>
        {
            public async Task<ReadOnlyMemory<byte>> Handle(WalidateUploadingFileQuery query, CancellationToken Cancel)
            {

                return ReadOnlyMemory<byte>.Empty;
            }
        }
    }
}
