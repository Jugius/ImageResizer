using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;


namespace ImageResizer
{
    public static class ImageBuilder
    {
        public static Bitmap LoadImage(object source)
        {
            if (source == null) throw new ArgumentNullException("source", "Источник не может быть пустым");

            Bitmap bitmap = null;
            string path = null;
            string loadFailureReasons = "Файл может быть поврежден, пуст или может содержать изображение в формате PNG, размер которого превышает 65 535 пикселей";

            //Bitmap
            if (source is Bitmap) return source as Bitmap;
            //Image
            if (source is System.Drawing.Image)
                return new Bitmap((System.Drawing.Image)source); //Note, this clones just the raw bitmap data - doesn't copy attributes, bit depth, or anything.

            Stream stream = GetStreamFromSource(source, out path);
            if (stream == null) throw new ArgumentException("Источник может быть только string, Bitmap, Image, Stream.", "source");

            try
            {
                try
                {
                    bitmap = DecodeStream(stream, path);
                    if (bitmap == null) throw new ImageCorruptedException("Ошибка чтения изображения. Поток вернул null.", null);
                }
                catch (Exception ex)
                {
                    if (!stream.CanSeek)
                        throw new ImageCorruptedException("Cannot attempt fallback decoding path on a non-seekable stream", ex);
                }

            }
            catch (ArgumentException ae)
            {
                ae.Data.Add("path", path);
                throw new ImageCorruptedException(loadFailureReasons, ae);
            }
            catch (ExternalException ee)
            {
                ee.Data.Add("path", path);
                throw new ImageCorruptedException(loadFailureReasons, ee);

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
            return bitmap;
        }
        private static Stream GetStreamFromSource(Uri requestUri)
        {
            WebRequest request = WebRequest.Create(requestUri);
            Stream stream = null;
            using (WebResponse response = request.GetResponse())
            {
                stream = response.GetResponseStream();
            }
            return stream;
        }
        private static Stream GetStreamFromSource(string imagePath)
        {
            Stream stream = null;
            if (File.Exists(imagePath))
            {
                stream = File.Open(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            }            
            else
            {
                Uri requestUri;
                if (Uri.TryCreate(imagePath, UriKind.Absolute, out requestUri))
                {
                    stream = GetStreamFromSource(requestUri);
                }
            }
            return stream;
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
            else if (source is string)
            {
                path = (string)source;
                stream = GetStreamFromSource(path);
            }
            else if (source is byte[])
            {
                stream = new MemoryStream((byte[])source, 0, ((byte[])source).Length, false, true);
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
            Bitmap b = null;
            bool useICM = true;            

            //May 24, 2011 - Copying stream into memory so the original can be closed safely.
            MemoryStream memoryStream = s.CopyToMemoryStream();
            b = new Bitmap(memoryStream, useICM);
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
            if (format==null)
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
    }
}
