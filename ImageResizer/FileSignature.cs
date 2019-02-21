
namespace ImageResizer
{
    public class FileSignature
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="signature">Байтовая сигнатура файла</param>
        /// <param name="ext">Расширение</param>
        /// <param name="mime">MimeType</param>
        public FileSignature(byte[] signature, string ext, string mime)
        {
            this.Signature = signature;
            this.PrimaryFileExtension = ext;
            this.MimeType = mime;
        }
        public byte[] Signature { get; protected set; }
        public string PrimaryFileExtension { get; protected set; }
        public string MimeType { get; protected set; }
    }
}
