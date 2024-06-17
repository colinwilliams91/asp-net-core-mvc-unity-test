using System.Text;
using Microsoft.AspNetCore.StaticFiles;

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

        private static readonly Dictionary<string, string> COMPRESSION_ENCODINGS = new ()
        {
            { GZIP_EXTENSION, "gzip" },
            { BROTLI_EXTENSION, "br" }
        };

        public static IReadOnlyDictionary<string, string> CompressionEncodings => COMPRESSION_ENCODINGS;

        public static StaticFileOptions GetOptions()
        {
            var customFileTypeProvider = new CustomContentTypeProvider();
            return new StaticFileOptions
            {
                ContentTypeProvider = customFileTypeProvider,
                OnPrepareResponse = (context) =>
                {
                    // In addition to the MIME type also set the according encoding header TODO: the logic inside the if parens could be its own method?
                    if (CompressionEncodings.TryGetValue(Path.GetExtension(context.File.Name), out string? encoding))
                    {
                        context.Context.Response.Headers.ContentEncoding = encoding;
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

            return COMPRESSION_ENCODINGS.TryAdd(extension, encoding);
        }

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
                    // TODO: use Substring instead?
                    filePath = filePath[..^extension.Length];
                }

                // then return the according MIME type
                return fileTypeProvider.TryGetContentType(filePath, out contentType);
            }
        }

        private static class FileUtilities
        {
            /// <summary>
            /// I elected to use a custom method to get the last file extension including the "." 
            /// instead of using Path.GetExtension as that may throw an exception if there are
            /// invalid characters in the path. Invalid characters should be handled by the FileProviders
            /// </summary>
            /// <param name="filePath">The string that represents the full file path</param>
            /// <returns>The last extension in the file path or Null</returns>
            private static string? GetExtension(string filePath)
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return null;
                }

                int index = filePath.LastIndexOf('.');
                if (index < 0)
                {
                    return null;
                }

                return filePath.Substring(index);
            }

            private static string TruncateExtension(string filePath, string extension)
            {
                ArgumentNullException.ThrowIfNullOrWhiteSpace(filePath);
                ArgumentNullException.ThrowIfNullOrWhiteSpace(extension);

                return filePath.Substring(0, filePath.Length - extension.Length);
            }
        }
    }
}
