﻿using Microsoft.AspNetCore.StaticFiles;

namespace asp_net_core_mvc_unity_test.Utilities
{
    /// <summary>
    /// Handles WebGL specific MIME and encoding types as well as file path handling for the Gzip and Brotli compressions.
    /// Also exposes method for extending additional COMPRESSION_ENCODING types: AddCompressionEncoding
    /// </summary>
    public static class CustomStaticFileOptions
    {
        private const string GZIP_EXTENSION = ".gz";
        private const string BROTLI_EXTENSION = ".br";

        private static Dictionary<string, string> COMPRESSION_ENCODINGS = new Dictionary<string, string>()
        {
            { GZIP_EXTENSION, "gzip" },
            { BROTLI_EXTENSION, "br" }
        };

        public static IReadOnlyDictionary<string, string> CompressionEncodings => COMPRESSION_ENCODINGS;

        // This handles the MIME type mapping
        private class CustomContentTypeProvider : IContentTypeProvider
        {
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
                ArgumentNullException.ThrowIfNullOrWhiteSpace(filePath);
                
                // If this is a compressed file we need to get the actual extension
                var extension = Path.GetExtension(filePath);
                if (extension == GZIP_EXTENSION || extension == BROTLI_EXTENSION)
                {
                    // Truncating the .gz or .br compression extension and get the actual extension
                    // e.g. "Build/something/image.png.gz" -> "Build/something/image.png" -> ".png"
                    filePath = Path.GetFileNameWithoutExtension(filePath);
                }

                // then return the according MIME type
                return fileTypeProvider.TryGetContentType(filePath, out contentType);
            }

            private static string? GetExtension(string path)
            {
                // Don't use Path.GetExtension as that may throw an exception if there are
                // invalid characters in the path. Invalid characters should be handled
                // by the FileProviders

                if (string.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                int index = path.LastIndexOf('.');
                if (index < 0)
                {
                    return null;
                }

                return path.Substring(index);
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
                    // In addition to the MIME type also set the according encoding header TODO: the logic inside the if parens could be its own method?
                    if (CompressionEncodings.TryGetValue(Path.GetExtension(context.File.Name), out var encoding))
                    {
                        context.Context.Response.Headers["Content-Encoding"] = encoding;
                    }
                }
            };
        }

        /// <summary>
        /// Method to add new compression mappings
        /// </summary>
        /// <param name="extension">A File extension like ".gz"</param>
        /// <param name="encoding">The correct encoding type for the file extension type like "gzip"</param>
        /// <returns>True if compression mapping added, False if already exists</returns>
        public static bool AddCompressionEncoding(string extension, string encoding)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(extension);
            ArgumentNullException.ThrowIfNullOrWhiteSpace(encoding);

            // Returns 
            return COMPRESSION_ENCODINGS.TryAdd(extension, encoding);
        }
    }
}
