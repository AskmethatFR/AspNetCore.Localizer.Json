using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace AspNetCore.Localizer.Json.Localizer.Modes
{
    /// <summary>
    /// Utility class for reading JSON streams with optimized memory usage.
    /// Uses ArrayPool and MemoryPool to minimize allocations.
    /// </summary>
    internal static class JsonStreamReader
    {
        private const int DefaultBufferSize = 8192;
        private const int DefaultPoolSize = 65536;

        /// <summary>
        /// Reads a stream and returns the JSON data as a byte array.
        /// Automatically handles UTF-8 BOM and uses pooled buffers to minimize allocations.
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        /// <param name="encoding">The encoding of the source data</param>
        /// <returns>A byte array containing the UTF-8 encoded JSON data without BOM</returns>
        public static byte[] ReadStreamToBuffer(Stream stream, Encoding encoding)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(DefaultBufferSize);
            try
            {
                using var pooledMemory = MemoryPool<byte>.Shared.Rent(DefaultPoolSize);
                var memoryOwner = pooledMemory;
                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (totalBytesRead + bytesRead > memoryOwner.Memory.Length)
                    {
                        // Fallback to MemoryStream for large files
                        using var memoryStream = new MemoryStream();
                        memoryStream.Write(memoryOwner.Memory.Span.Slice(0, totalBytesRead));
                        memoryStream.Write(buffer, 0, bytesRead);

                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            memoryStream.Write(buffer, 0, bytesRead);
                        }

                        return ProcessJsonData(memoryStream.GetBuffer(), (int)memoryStream.Length, encoding);
                    }

                    buffer.AsSpan(0, bytesRead).CopyTo(memoryOwner.Memory.Span.Slice(totalBytesRead));
                    totalBytesRead += bytesRead;
                }

                return ProcessJsonData(memoryOwner.Memory.Span.Slice(0, totalBytesRead).ToArray(), totalBytesRead, encoding);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        /// Processes JSON data: handles UTF-8 BOM and encoding conversion.
        /// </summary>
        /// <param name="jsonData">The raw JSON data</param>
        /// <param name="length">The length of the data</param>
        /// <param name="encoding">The encoding of the source data</param>
        /// <returns>A byte array containing the UTF-8 encoded JSON data without BOM</returns>
        private static byte[] ProcessJsonData(byte[] jsonData, int length, Encoding encoding)
        {
            // Skip BOM if present (UTF-8 BOM: 0xEF, 0xBB, 0xBF)
            int startIndex = 0;
            if (length >= 3 && jsonData[0] == 0xEF && jsonData[1] == 0xBB && jsonData[2] == 0xBF)
            {
                startIndex = 3;
            }

            // If encoding is not UTF-8, convert to UTF-8 first
            if (encoding != Encoding.UTF8)
            {
                string jsonString = encoding.GetString(jsonData, startIndex, length - startIndex);
                return Encoding.UTF8.GetBytes(jsonString);
            }

            // Return a slice without BOM
            if (startIndex > 0)
            {
                byte[] result = new byte[length - startIndex];
                Array.Copy(jsonData, startIndex, result, 0, length - startIndex);
                return result;
            }

            // If no BOM and already UTF-8, return as-is (but only the used portion)
            if (length < jsonData.Length)
            {
                byte[] result = new byte[length];
                Array.Copy(jsonData, 0, result, 0, length);
                return result;
            }

            return jsonData;
        }
    }
}