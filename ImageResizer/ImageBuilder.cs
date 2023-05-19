using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;

namespace ImageResizer
{
    public static class ImageBuilder
    {
        private const string LoadFailureReasons = "Файл не является изображением, может быть поврежден, пуст или может содержать изображение в формате PNG, размер которого превышает 65 535 пикселей";
        public static Bitmap LoadImage(object source)
        {
            if (source == null) throw new ArgumentNullException("source", "Источник не может быть пустым");

            Bitmap bitmap = null;


            //Bitmap
            if (source is Bitmap bmp) return bmp;

            //Image
            if (source is System.Drawing.Image img)
                return new Bitmap(img); //Note, this clones just the raw bitmap data - doesn't copy attributes, bit depth, or anything.

            Stream stream = GetStreamFromSource(source, out string path);
            if (stream == null) throw new ArgumentException("Источник может быть только string, Bitmap, Image, Stream.", "source");

            try
            {
                bitmap = DecodeStream(stream, path);
            }
            catch (ArgumentException ex)
            {
                throw new ImageCorruptedException(LoadFailureReasons, ex);
            }
            finally
            {
                //Now, we can't dispose the stream if Bitmap is still using it. 
                if (bitmap != null && bitmap.Tag != null && bitmap.Tag is BitmapTag && ((BitmapTag)bitmap.Tag).Source == stream)
                {
                    //And, it looks like Bitmap is still using it.
                    stream = null;
                }
                //Dispose the stream if we opened it. If someone passed it to us, they're responsible.
                if (stream != null) { stream.Dispose(); stream = null; }


                //Make sure the bitmap is tagged with its path. DecodeStream usually handles this, only relevant for extension decoders.
                if (bitmap != null && bitmap.Tag == null && path != null) bitmap.Tag = new BitmapTag(path);
            }

            if (bitmap == null) throw new ImageCorruptedException("Ошибка чтения изображения. Поток вернул null.", null);

            return bitmap;

        }
        private static Stream GetStreamFromSource(Uri requestUri)
        {
            if (requestUri.Scheme == Uri.UriSchemeFile)
            {
                if (File.Exists(requestUri.LocalPath))
                {
                    return File.Open(requestUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                else
                {
                    throw new FileNotFoundException($"Файл не найден: {requestUri.LocalPath}.");
                }                
            }                

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                return response.GetResponseStream();
            }
            catch (WebException webex)
            {
                if (webex.Response is HttpWebResponse response)
                {
                    int statusCode = (int)response.StatusCode;
                    if (statusCode >= 300 && statusCode <= 399)
                    {
                        using (response)
                        {
                            var uriString = response.Headers["Location"];
                            return GetStreamFromSource(new Uri(uriString));
                        }
                    }
                }
                throw;
            }
        }
        private static Stream GetStreamFromSource(string imagePath)
        {
            if (IsValidUri(imagePath, out Uri requestUri))
            {
                return GetStreamFromSource(requestUri);
            }

            if (File.Exists(imagePath))
            {
                return File.Open(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }
            return null;
        }
        private static Stream GetStreamFromSource(object source, out string path)
        {
            if (source == null) throw new ArgumentNullException("source", "Источник не может быть пустым");

            Stream stream = null;
            path = null;

            if (source is Stream)
            {
                stream = (Stream)source;
            }
            else if (source is string str)
            {
                path = str;
                stream = GetStreamFromSource(str);
            }
            else if (source is Uri u)
            {
                path = u.Scheme == Uri.UriSchemeFile ? u.LocalPath : u.AbsolutePath;
                stream = GetStreamFromSource(u);
            }
            else if (source is byte[] bytes)
            {
                stream = new MemoryStream(bytes, 0, bytes.Length, false, true);
            }
            try
            {
                if (stream != null && stream.Length == 0)
                    throw new Exception("Source stream is empty; it has a length of 0. No bytes, no data. We can't work with this.");
            }
            catch (NotSupportedException)
            {
            }

            return stream;
        }
        private static Bitmap DecodeStream(Stream s, string optionalPath)
        {
            const bool useICM = true;

            //May 24, 2011 - Copying stream into memory so the original can be closed safely.
            MemoryStream memoryStream = s.CopyToMemoryStream();
            Bitmap b = new Bitmap(memoryStream, useICM);
            //May 25, 2011: Storing a ref to the MemorySteam so it won't accidentally be garbage collected.
            b.Tag = new BitmapTag(optionalPath, memoryStream);
            return b;
        }
        /// <summary>
        /// Сохраняет изображение в файл или поток
        /// </summary>
        /// <param name="image">Изображение для сохранения</param>
        /// <param name="destination">Путь к файлу или поток, куда необходимо записать изображение</param>
        /// <param name="imageFormat">Формат изображения. Может быть jpeg, gif, png</param>
        /// <param name="jpegQuality">Качество изображения для jpeg в диапазоне 10-100. Игнорируется при других форматах. Оптимальным считается 90</param>
        public static void SaveImage(Image image, object destination, ImageFormat imageFormat, int jpegQuality)
        {
            if (image == null)
                throw new ArgumentNullException("bitmap");

            // Определяем imageFormat изображения
            ImageFormat format = imageFormat;
            if (format == null)
            {
                if (destination is string)
                {
                    try
                    {
                        format = ImageEncoder.GetImageFormatFromPhysicalPath(destination as string);
                    }
                    catch
                    {
                        format = image.RawFormat;
                    }
                }
                else
                    format = image.RawFormat;
            }
            if (jpegQuality < 10 || jpegQuality > 100)
                throw new ArgumentException("Качество изображения может быть в диапазоне 10 - 100");

            if (destination is string)
            {
                string fileName = (string)destination;
                string dirName = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dirName)) Directory.CreateDirectory(dirName);

                bool finishedWrite = false;
                try
                {
                    System.IO.FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                    using (fileStream)
                    {
                        BuildBitmapToStream(image, fileStream, imageFormat, jpegQuality);
                        fileStream.Flush();
                        finishedWrite = true;
                    }
                }
                finally
                {
                    //Don't leave half-written files around.
                    if (!finishedWrite) try { if (File.Exists(fileName)) File.Delete(fileName); }
                        catch { }
                }
            }
            else if (destination is Stream)
            {
                BuildBitmapToStream(image, (Stream)destination, imageFormat, jpegQuality);
            }
            else throw new ArgumentException("Destination may be a string or Stream.", "Dest");
        }
        private static void BuildBitmapToStream(Image image, Stream dest, ImageFormat imageFormat, int jpegQuality)
        {
            ImageEncoder encoder = new ImageEncoder(imageFormat, jpegQuality);

            if (encoder == null) throw new ImageProcessingException("No image encoder was found for this request.");
            encoder.Write(image, dest);
        }
        private static bool IsValidUri(string s, out Uri uri)
        {
            bool result = Uri.TryCreate(s, UriKind.Absolute, out uri) &&
                (uri.Scheme == Uri.UriSchemeHttp ||
                 uri.Scheme == Uri.UriSchemeHttps ||
                 uri.Scheme == Uri.UriSchemeFtp);
            return result;
        }
    }
}
