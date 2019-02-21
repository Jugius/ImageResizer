using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ImageResizer
{
    internal static class StreamExtensionMethods
    {
        /// <summary>
        /// Copies the remaining data in the current stream to a new MemoryStream instance.
        /// </summary>        
        public static MemoryStream CopyToMemoryStream(this Stream s)
        {
            return CopyToMemoryStream(s, false);
        }
        /// <summary>
        /// Copies the current stream into a new MemoryStream instance.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="entireStream">True to copy entire stream if seekable, false to only copy remaining data</param>
        /// <returns></returns>
        public static MemoryStream CopyToMemoryStream(this Stream s, bool entireStream)
        {
            return CopyToMemoryStream(s, entireStream, 0x1000);
        }
        /// <summary>
        /// Copies the current stream into a new MemoryStream instance.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="entireStream">True to copy entire stream if seekable, false to only copy remaining data</param>
        /// <param name="chunkSize">The buffer size to use (in bytes) if a buffer is required. Default: 4KiB</param>
        /// <returns></returns>
        public static MemoryStream CopyToMemoryStream(this Stream s, bool entireStream, int chunkSize)
        {
            MemoryStream ms = new MemoryStream(s.CanSeek ? ((int)s.Length + 8 - (entireStream ? 0 : (int)s.Position)) : chunkSize);
            CopyToStream(s, ms, entireStream, chunkSize);
            ms.Position = 0;
            return ms;
        }
        /// <summary>
        /// Copies this stream into the given stream
        /// </summary>
        /// <param name="s"></param>
        /// <param name="other">The stream to write to</param>
        /// <param name="entireStream">True to copy entire stream if seekable, false to only copy remaining data</param>
        public static void CopyToStream(this Stream s, Stream other, bool entireStream)
        {
            CopyToStream(s, other, entireStream, 0x1000);
        }
        /// <summary>
        /// Copies this stream into the given stream
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dest">The stream to write to</param>
        /// <param name="entireStream">True to copy entire stream if seekable, false to only copy remaining data</param>
        /// <param name="chunkSize">True to copy entire stream if seekable, false to only copy remaining data</param>
        public static void CopyToStream(this Stream src, Stream dest, bool entireStream, int chunkSize)
        {
            if (entireStream && src.CanSeek) src.Seek(0, SeekOrigin.Begin);

            if (src is MemoryStream && src.CanSeek)
            {
                try
                {
                    int pos = (int)src.Position;
                    dest.Write(((MemoryStream)src).GetBuffer(), pos, (int)(src.Length - pos));
                    return;
                }
                catch (UnauthorizedAccessException) //If we can't slice it, then we read it like a normal stream
                { }
            }
            if (dest is MemoryStream && src.CanSeek)
            {
                try
                {
                    int srcPos = (int)src.Position;
                    int pos = (int)dest.Position;
                    int length = (int)(src.Length - srcPos) + pos;
                    dest.SetLength(length);

                    var data = ((MemoryStream)dest).GetBuffer();
                    while (pos < length)
                    {
                        pos += src.Read(data, pos, length - pos);
                    }
                    return;
                }
                catch (UnauthorizedAccessException) //If we can't write directly, fall back
                { }
            }
            int size = (src.CanSeek) ? Math.Min((int)(src.Length - src.Position), chunkSize) : chunkSize;
            byte[] buffer = new byte[size];
            int n;
            do
            {
                n = src.Read(buffer, 0, buffer.Length);
                dest.Write(buffer, 0, n);
            } while (n != 0);
        }
    }
}
