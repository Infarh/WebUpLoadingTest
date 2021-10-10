using System;
using System.IO;
using Microsoft.Net.Http.Headers;

namespace WebUpLoadingTest.Infrastructure
{
    public static class MultipartRequestHelper
    {
        // Content-Type: multipart/form-data; boundary="----WebKitFormBoundarymx2fSWqWSd0OxQqq"
        // The spec at https://tools.ietf.org/html/rfc2046#section-5.1 states that 70 characters is a reasonable limit.
        public static string GetBoundary(MediaTypeHeaderValue Content, int Limit)
        {
            var boundary = HeaderUtilities.RemoveQuotes(Content.Boundary).Value;

            if (string.IsNullOrWhiteSpace(boundary))
                throw new InvalidDataException("Missing content-type boundary.");

            if (boundary.Length > Limit)
                throw new InvalidDataException($"Multipart boundary length limit {Limit} exceeded.");

            return boundary;
        }

        public static bool IsMultipartContentType(string ContentType) =>
            ContentType is { Length: > 0 }
            && ContentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;

        // Content-Disposition: form-data; name="key";
        public static bool HasFormDataContentDisposition(ContentDispositionHeaderValue ContentDisposition) =>
            ContentDisposition != null
            && ContentDisposition.DispositionType.Equals("form-data")
            && ContentDisposition.FileName.Value is not { Length: > 0 }
            && ContentDisposition.FileNameStar.Value is not { Length: > 0 };

        // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
        public static bool HasFileContentDisposition(ContentDispositionHeaderValue ContentDisposition) =>
            ContentDisposition != null
            && ContentDisposition.DispositionType.Equals("form-data")
            && (ContentDisposition.FileName.Value is { Length: > 0 } || ContentDisposition.FileNameStar.Value is { Length: > 0 });
    }
}
