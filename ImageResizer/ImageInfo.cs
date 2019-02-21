using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer
{
    public class ImageInfo : IDisposable
    {
        public Bitmap SourceBitmap { get { return sourceBitmap; } }
        private Bitmap sourceBitmap;
        public Bitmap DestinationBitmap { get { return destinationBitmap; } }
        private Bitmap destinationBitmap = null;
        string SourcePath { get; }
        string SourceExtention { get; }
        internal ImageInfo(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("Значение Null Bitmap при создании экземпляра ImageInfo");

            this.sourceBitmap = bitmap;

            if (bitmap.Tag != null && bitmap.Tag is BitmapTag)
                this.SourcePath = (bitmap.Tag as BitmapTag).Path;

            this.SourceExtention = ImageEncoder.GetExtensionFromImageFormat(bitmap.RawFormat);
        }
        public Bitmap ResizeImage(ResizeSettings settings)
        {
            destinationBitmap = Resizer.Resize(SourceBitmap, settings);
            return this.DestinationBitmap;
        }
        public void Save() => this.SaveAs(this.SourcePath);
        public void SaveAs(string fileName)
        {
            Bitmap bitmap = GetFinalBitmapForSave();
            this.SaveAs(fileName, bitmap.RawFormat);
        }
        public void SaveAs(string fileName, ImageFormat imageFormat)
        {
            if (imageFormat == null)
                throw new ArgumentNullException("ImageFormat");

            Bitmap bitmap = GetFinalBitmapForSave();

            if (ImageFormat.Jpeg.Equals(imageFormat))
            {
                if (imageFormat.Equals(bitmap.RawFormat))
                    this.SaveAs(fileName, imageFormat, 100);
                else
                    this.SaveAs(fileName, imageFormat, 90);
            }
            else               
            {
                ImageBuilder.SaveImage(bitmap, fileName, imageFormat, 100);
            }            
        }   
             
        public void SaveAs(string fileName, ImageFormat imageFormat, int jpegQuality)
        {
            if(string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException("FileName");

            if (imageFormat == null)
                throw new ArgumentNullException("ImageFormat");

            if (jpegQuality < 10 || jpegQuality > 100)
                throw new ArgumentException("Качество изображения может быть в диапазоне 10 - 100");

            Bitmap bitmap = GetFinalBitmapForSave();

            ImageBuilder.SaveImage(bitmap, fileName, imageFormat, jpegQuality);
        }
        private Bitmap GetFinalBitmapForSave() => this.DestinationBitmap ?? this.SourceBitmap;
        public bool ResultHasEqualSize()
        {
            if (DestinationBitmap == null)
                throw new Exception("Результирующее изображение отсутствует");

            if (DestinationBitmap == SourceBitmap) return true;

            return SourceBitmap.Height == DestinationBitmap.Height && SourceBitmap.Width == DestinationBitmap.Width;
        }

        #region Static Builders
        /// <summary>
        /// Создает экземпляр ImageInfo
        /// </summary>
        /// <param name="source">"Источник может быть Uri, String, Bitmap, Image, Stream, byte[]"</param>
        /// <returns></returns>
        public static ImageInfo Build(object source)
        {
            Bitmap bitmap = ImageBuilder.LoadImage(source);
            return new ImageInfo(bitmap);
        }
        public static ImageInfo Build(string path) => Build(path as object);
        public static ImageInfo Build(Uri uri) => Build(uri as object);
        #endregion

        #region Disposing
        private bool iDisposed = false;
        public void Dispose()
        {
            if (iDisposed) return;

            try
            {
                if (sourceBitmap != null)
                {
                    if (sourceBitmap.Tag != null && sourceBitmap.Tag is BitmapTag)
                    {
                        System.IO.Stream s = ((BitmapTag)sourceBitmap.Tag).Source;
                        if (s != null) s.Dispose();
                    }
                    sourceBitmap.Dispose();
                }
            }
            finally {
                if (destinationBitmap != null) destinationBitmap.Dispose();
            }
            iDisposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
       
    }
}
