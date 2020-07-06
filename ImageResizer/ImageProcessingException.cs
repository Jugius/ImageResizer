using System;

namespace ImageResizer
{
    public class ImageProcessingException : Exception
    {
        public ImageProcessingException(string message) : base(message) { }
        public ImageProcessingException(string message, string safeMessage)
            : base(message)
        {
            PublicSafeMessage = safeMessage;
        }
        public ImageProcessingException(string message, string safeMessage, Exception innerException)
            : base(message, innerException)
        {
            PublicSafeMessage = safeMessage;
        }
        /// <summary>
        /// This error message is safe to display to the public (should not contain any sensitive information)
        /// </summary>
        protected string PublicSafeMessage { get; set; }
    }
    /// <summary>
    /// Исходный файл поврежден
    /// </summary>
    public class ImageCorruptedException : ImageProcessingException
    {
        public ImageCorruptedException(string message, Exception innerException) : base(message, message, innerException) { }
    }
 }
