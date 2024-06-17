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
        /// Handles the MIME type mapping for <see cref="StaticFileOptions"/>
        /// to pass to <see cref="WebApplication.UseStaticFiles"/> middleware pipeline config
        /// <remarks>
        /// <see cref="FileExtensionContentTypeProvider"/> IDictionary property already handles
        /// the MIME type mappings for the 380 most commonly used file types this extends it 
        /// for Unity WebGL build .data file extension type and according MIME type
        /// </remarks>
        /// </summary>
        private class CustomContentTypeProvider : IContentTypeProvider
        {
            private readonly FileExtensionContentTypeProvider fileTypeProvider = new();

            public CustomContentTypeProvider()
            {
                fileTypeProvider.Mappings[".data"] = "application/octet-stream";
            }

            #region Private Class Methods
            /// <summary>
            /// Interface method used when the app tries to find a MIME mapping for a served static file
            /// Leveraging System.IO.Path methods normalizes path separators
            /// </summary>
            /// <example>
            /// Truncating the .gz or .br compression extension and get the actual extension
            /// "Build\something\image.png.gz" -> "Build\something\image.png" -> ".png"
            /// </example>
            /// <remarks>
            /// Edge Case: <see cref="Path.GetDirectoryName"/>
            /// "root directory" arg returns null however <see cref="Path.Combine"/>
            /// leverages <see cref="Path.CombineInternal"/> handling null or empty...
            /// But I pass string.Empty so the interpreter stops complaining
            /// </remarks>
            /// <param name="filePath">Static file path string containing compression extension</param>
            /// <param name="contentType">
            /// Code smell out param from eventual .NET<see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/>
            /// </param>
            /// <returns>The according MIME type</returns>
            public bool TryGetContentType(string filePath, out string contentType)
            {
                ArgumentNullException.ThrowIfNullOrWhiteSpace(filePath);

                // Gets the last "." exentsion (compression type)
                var extension = Path.GetExtension(filePath);
                // TODO: extend -- if (COMPRESSION_ENCODINGS.ContainsKey(extension))
                if (extension == GZIP_EXTENSION || extension == BROTLI_EXTENSION)
                {
                    var filePathDirectory = Path.GetDirectoryName(filePath);
                    var fileNameTruncated = Path.GetFileNameWithoutExtension(filePath);
                    filePath = Path.Combine(filePathDirectory ?? string.Empty, fileNameTruncated);
                }

                return fileTypeProvider.TryGetContentType(filePath, out contentType);
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
            #endregion
        }
    }
}
