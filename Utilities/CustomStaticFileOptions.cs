using Microsoft.AspNetCore.StaticFiles;

namespace asp_net_core_mvc_unity_test.Utilities
{
    // Handles WebGL specific MIME and encoding types as well as file path handling for the Gzip and Brotli compressions
    public static class CustomStaticFileOptions
    {
        // This handles the MIME type mapping
        private class CustomContentTypeProvider : IContentTypeProvider
        {
            private const string GZIP_EXTENSION = ".gz";
            private const string BROTLI_EXTENSION = ".br";

            public static readonly IReadOnlyDictionary<string, string> COMPRESSION_ENCODINGS = new Dictionary<string, string>()
            {
                { GZIP_EXTENSION, "gzip" },
                { BROTLI_EXTENSION, "br" }
            };

            // This by default already knows the MIME type mappings for the most commonly used file types
            private readonly FileExtensionContentTypeProvider fileTypeProvider = new();

            public CustomContentTypeProvider()
            {
                // Can extend it with our own mappings - as mentioned in particular this one
                fileTypeProvider.Mappings[".data"] = "application/octet-stream";
            }

            // interface method used when the app tries to find a MIME mapping for a served static file 
            public bool TryGetContentType(string filePath, out string contentType)
            {
                // If this is a compressed file we need to get the actual extension
                var extension = Path.GetExtension(filePath);
                if (extension == GZIP_EXTENSION || extension == BROTLI_EXTENSION)
                {
                    // Cutting off the .gz or .br extension and get the actual extension
                    // e.g. "Build/something/image.png.gz" -> "Build/something/image.png" -> ".png"
                    filePath = filePath[..^extension.Length];
                    extension = Path.GetExtension(filePath);
                }

                // then return the according MIME type
                return fileTypeProvider.TryGetContentType(filePath, out contentType);
            }
        }

        public static StaticFileOptions GetOptions()
        {
            var fileTypeProvider = new CustomContentTypeProvider();
            return new StaticFileOptions
            {
                ContentTypeProvider = fileTypeProvider,
                OnPrepareResponse = (context) =>
                {
                    // In addition to the MIME type also set the according encoding header (the logic inside the if parens could be its own class fn?)
                    if (CustomContentTypeProvider.COMPRESSION_ENCODINGS.TryGetValue(Path.GetExtension(context.File.Name), out var encoding))
                    {
                        context.Context.Response.Headers["Content-Encoding"] = encoding;
                    }
                }
            };
        }
    }
}
