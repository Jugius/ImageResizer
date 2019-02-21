using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ImageResizer
{
    public static class Resizer
    {
        public static Bitmap Resize(Bitmap bitmap, ResizeSettings settings)
        {
            Size newSize = CalculateNewSize(bitmap, settings);

            if (bitmap.Height == newSize.Height && bitmap.Width == newSize.Width)
                return bitmap;

            Bitmap newImage = new Bitmap(newSize.Width, newSize.Height, bitmap.PixelFormat);
            newImage.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

            using (Graphics graphicsHandle = Graphics.FromImage(newImage))
            {
                graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsHandle.SmoothingMode = SmoothingMode.HighQuality;
                graphicsHandle.CompositingQuality = CompositingQuality.HighQuality;
                graphicsHandle.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphicsHandle.CompositingMode = CompositingMode.SourceCopy;

                graphicsHandle.DrawImage(bitmap, 0, 0, newSize.Width, newSize.Height);
            }
            return newImage;
        }
        public static Size CalculateNewSize(object source, ResizeSettings settings)
        {
            if (source == null)
                throw new ArgumentNullException("sourse");

            if(source is Image || source is Bitmap)
                return CalculateNewSize(source as Image, settings);

            Bitmap bitmap = null;
            try
            {
                bitmap = ImageBuilder.LoadImage(source);
                Size newSize = CalculateNewSize(bitmap as Image, settings);
                return newSize;
            }
            finally {
                if (bitmap != null)
                    bitmap.Dispose();
            }
        }
        public static Size CalculateNewSize(Image source, ResizeSettings settings)
        {
            Size newSize = new Size();
            int originalWidth = source.Width;
            int originalHeight = source.Height;

            float resizedImageScaleFactor = 0;
            int bigSide = 0, smallSide = 0;

            switch (settings.ResizeMode)
            {
                case ResizeMode.MaxSides:

                    settings.CompareSides(ref smallSide, ref bigSide);
                    if (settings.StretchMode == StretchMode.Proportionally)
                    {
                        resizedImageScaleFactor = GetScaleFactor(originalHeight, originalWidth, smallSide, bigSide);
                        ApplyResizeRule(originalHeight, originalWidth, resizedImageScaleFactor, settings.ScaleMode, ref newSize);
                    }
                    else
                    {
                        if (originalHeight > originalWidth)
                        {
                            newSize.Height = bigSide;
                            newSize.Width = smallSide;
                        }
                        else
                        {
                            newSize.Height = smallSide;
                            newSize.Width = bigSide;
                        }
                    }
                    break;

                case ResizeMode.OneSide:

                    if (settings.Height > 0 && settings.Width > 0)
                        throw new Exception($"width: {settings.Width}, height: {settings.Height}. Только одна из сторон должна быть больше ноля");

                    if (settings.Height > 0)
                        resizedImageScaleFactor = settings.Height / Convert.ToSingle(originalHeight);
                    else if (settings.Width > 0)
                        resizedImageScaleFactor = settings.Width / Convert.ToSingle(originalWidth);
                    else
                        throw new Exception($"width: { settings.Width }, height: { settings.Height}. Одна из сторон должна быть больше ноля");

                    newSize.Width = Convert.ToInt32(originalWidth * resizedImageScaleFactor);
                    newSize.Height = Convert.ToInt32(originalHeight * resizedImageScaleFactor);
                    break;

                case ResizeMode.Rectangle:
                    if (settings.StretchMode == StretchMode.Proportionally)
                    {
                        if (originalHeight > originalWidth)
                        {
                            bigSide = settings.Height;
                            smallSide = settings.Width;
                        }
                        else
                        {
                            bigSide = settings.Width;
                            smallSide = settings.Height;
                        }
                        resizedImageScaleFactor = GetScaleFactor(originalHeight, originalWidth, smallSide, bigSide);
                        ApplyResizeRule(originalHeight, originalWidth, resizedImageScaleFactor, settings.ScaleMode, ref newSize);
                    }
                    else
                    {
                        newSize.Height = settings.Height;
                        newSize.Width = settings.Width;
                    }
                    break;

                default:
                    break;
            }

            return newSize;
        }
        private static float GetScaleFactor(int pictureHeight, int pictureWidth, int rezmaxSmS, int rezmaxBgS)
        {
            float temp = 0;

            if ((pictureHeight > pictureWidth))
            {
                temp = rezmaxBgS / Convert.ToSingle(pictureHeight);
                if (((pictureWidth * temp) > rezmaxSmS))
                {
                    temp = rezmaxSmS / Convert.ToSingle(pictureWidth);
                }
                return temp;
            }
            else
            {
                temp = rezmaxBgS / Convert.ToSingle(pictureWidth);
                if (((pictureHeight * temp) > rezmaxSmS))
                {
                    temp = rezmaxSmS / Convert.ToSingle(pictureHeight);
                }
                return temp;
            }
        }
        private static void ApplyResizeRule(int pictureHeight, int pictureWidth, float scaleFactor, ScaleMode resizingRule, ref Size newSize)
        {
            int originalWidth = pictureWidth;
            int originalHeight = pictureHeight;
            switch (resizingRule)
            {
                case ScaleMode.UpscaleOnly:
                    if (scaleFactor > 1)
                    {
                        newSize.Width = Convert.ToInt32(originalWidth * scaleFactor);
                        newSize.Height = Convert.ToInt32(originalHeight * scaleFactor);
                    }
                    else
                    {
                        newSize.Width = originalWidth;
                        newSize.Height = originalHeight;
                    }
                    break;
                case ScaleMode.DownscaleOnly:
                    if (scaleFactor < 1)
                    {
                        newSize.Width = Convert.ToInt32(originalWidth * scaleFactor);
                        newSize.Height = Convert.ToInt32(originalHeight * scaleFactor);
                    }
                    else
                    {
                        newSize.Width = originalWidth;
                        newSize.Height = originalHeight;
                    }
                    break;
                case ScaleMode.Both:
                    newSize.Width = Convert.ToInt32(originalWidth * scaleFactor);
                    newSize.Height = Convert.ToInt32(originalHeight * scaleFactor);
                    break;
                case ScaleMode.None:
                    newSize.Width = originalWidth;
                    newSize.Height = originalHeight;
                    break;
            }
        }

    }
}
