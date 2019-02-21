using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;

namespace ImageResizer
{
    /// <summary>
    /// Provides basic encoding functionality for JPEG, PNG, and GIF output. Allows adjustable JPEG compression, but doesn't implement indexed PNG files or quantized GIF files.
    /// </summary>
    internal class ImageEncoder
    {
        public ImageEncoder()
        {
        }
        public ImageEncoder(ImageFormat outputFormat)
        {
            this.OutputFormat = outputFormat;
        }
        public ImageEncoder(ImageFormat outputFormat, int jpegQuality)
        {
            this.OutputFormat = outputFormat;
            this.Quality = jpegQuality;
        }
        //public ImageEncoder(object original)
        //{
        //    ImageFormat originalFormat = GetOriginalFormat(original);

        //    if (!IsValidOutputFormat(originalFormat))
        //    {
        //        this.OutputFormat = ImageFormat.Jpeg;
        //        this.Quality = 90;
        //    }
        //    else
        //    {
        //        this.OutputFormat = originalFormat;
        //        if (ImageFormat.Jpeg.Equals(originalFormat))
        //        {
        //            this.Quality = 100;
        //        }
        //    }
        //}

        /// <summary>
        /// If you set this to anything other than 'Gif', 'Png', or 'Jpeg', it will throw an exception. Defaults to 'Jpeg'.
        /// </summary>
        public ImageFormat OutputFormat
        {
            get { return _outputFormat; }
            set
            {
                if (!IsValidOutputFormat(value)) throw new ArgumentException(value.ToString() + " неподдерживаемый формат изображения.");
                _outputFormat = value;
            }
        }
        private ImageFormat _outputFormat = ImageFormat.Jpeg;

        /// <summary>
        /// Returns true if the this encoder supports the specified image format
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public bool IsValidOutputFormat(ImageFormat f)
        {
            return (ImageFormat.Gif.Equals(f) || ImageFormat.Png.Equals(f) || ImageFormat.Jpeg.Equals(f));
        }

        /// <summary>
        /// 0..100 value. The JPEG compression quality. 90 is the best setting. Not relevant in PNG or GIF compression.
        /// </summary>
        public int Quality { get; set; } = 90;

        /// <summary>
        /// Writes the specified image to the stream using Quality and OutputFormat
        /// </summary>
        /// <param name="image"></param>
        /// <param name="s"></param>
        public void Write(Image image, System.IO.Stream s)
        {
            if (ImageFormat.Jpeg.Equals(OutputFormat)) SaveJpeg(image, s, this.Quality);
            else if (ImageFormat.Png.Equals(OutputFormat)) SavePng(image, s);
            else if (ImageFormat.Gif.Equals(OutputFormat)) SaveGif(image, s);
        }

        /// <summary>
        /// Returns true if the desired output type supports transparency.
        /// </summary>
        public bool SupportsTransparency
        {
            get
            {
                return ImageFormat.Png.Equals(OutputFormat) || ImageFormat.Gif.Equals(OutputFormat); //Does Gif transparency work?
            }
        }

        /// <summary>
        /// Returns the default mime-type for the OutputFormat
        /// </summary>
        public string MimeType
        {
            get { return ImageEncoder.GetContentTypeFromImageFormat(OutputFormat); }
        }
        /// <summary>
        /// Returns the default file extension for OutputFormat
        /// </summary>
        public string Extension
        {
            get { return ImageEncoder.GetExtensionFromImageFormat(OutputFormat); }

        }
        /// <summary>
        /// Attempts to determine the ImageFormat of the source image. First attempts to parse the path, if a string is present in original.Tag. (or if 'original' is a string)
        /// Falls back to using original.RawFormat. Returns null if both 'original' is null.
        /// RawFormat has a bad reputation, so this may return unexpected values, like MemoryBitmap or something in some situations.
        /// </summary>
        /// <param name="original">The source image that was loaded from a stream, or a string path</param>
        /// <returns></returns>
        public static ImageFormat GetOriginalFormat(object original)
        {
            if (original == null) return null;
            //Try to parse the original file extension first.
            string path = original as string;

            if (path == null && original is Image) path = ((Image)original).Tag as string;

            if (path == null && original is Image && ((Image)original).Tag is BitmapTag) path = ((BitmapTag)((Image)original).Tag).Path;

            //We have a path? Parse it!
            if (path != null)
            {
                ImageFormat f = ImageEncoder.GetImageFormatFromPhysicalPath(path);
                if (f != null) return f; //From the path
            }
            //Ok, I guess if there (a) wasn't a path, or (b), it didn't have a recognizable extension
            if (original is Image) return ((Image)original).RawFormat;
            return null;
        }

        /// <summary>
        /// Returns the ImageFormat enumeration value based on the extension in the specified physical path. Extensions can lie, just a guess.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static ImageFormat GetImageFormatFromPhysicalPath(string path)
        {
            return GetImageFormatFromExtension(System.IO.Path.GetExtension(path));
        }

        /// <summary>
        /// Returns an string instance from the specified ImageFormat. First matching entry in imageExtensions is used.
        /// Returns null if not recognized.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetExtensionFromImageFormat(ImageFormat format)
        {
            lock (_syncExts)
            {
                foreach (KeyValuePair<string, ImageFormat> p in imageExtensions)
                {
                    if (p.Value.Guid.Equals(format.Guid)) return p.Key;
                }
            }
            return null;
        }


        private static object _syncExts = new object();
        /// <summary>
        /// Returns a dict of (lowercase invariant) image extensions and ImageFormat values
        /// </summary>
        private static IDictionary<String, ImageFormat> _imageExtensions = null;
        private static IDictionary<String, ImageFormat> imageExtensions
        {
            get
            {
                lock (_syncExts)
                {
                    if (_imageExtensions == null)
                    {
                        _imageExtensions = new Dictionary<String, ImageFormat>();
                        addImageExtension("jpg", ImageFormat.Jpeg);
                        addImageExtension("jpeg", ImageFormat.Jpeg);
                        addImageExtension("jpe", ImageFormat.Jpeg);
                        addImageExtension("jif", ImageFormat.Jpeg);
                        addImageExtension("jfif", ImageFormat.Jpeg);
                        addImageExtension("jfi", ImageFormat.Jpeg);
                        addImageExtension("exif", ImageFormat.Jpeg);
                        addImageExtension("bmp", ImageFormat.Bmp);
                        addImageExtension("gif", ImageFormat.Gif);
                        addImageExtension("png", ImageFormat.Png);
                        addImageExtension("tif", ImageFormat.Tiff);
                        addImageExtension("tiff", ImageFormat.Tiff);
                        addImageExtension("tff", ImageFormat.Tiff);
                        //"bmp","gif","exif","png","tif","tiff","tff","jpg","jpeg", "jpe","jif","jfif","jfi"
                    }
                    return _imageExtensions;
                }
            }
        }

        /// <summary>
        /// Returns an ImageFormat instance from the specified file extension. Extensions lie sometimes, just a guess.
        /// returns null if not recognized.
        /// </summary>
        /// <param name="ext"></param>
        /// <returns></returns>
        public static ImageFormat GetImageFormatFromExtension(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return null;
            lock (_syncExts)
            {
                ext = ext.Trim(' ', '.').ToLowerInvariant();
                if (!imageExtensions.ContainsKey(ext)) return null;
                return imageExtensions[ext];
            }
        }
        /// <summary>
        /// NOT thread-safe! 
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="matchingFormat"></param>
        private static void addImageExtension(string extension, ImageFormat matchingFormat)
        {
            //In case first call is to this method, use the property. Will be recursive, but that's fine, since it won't be null.
            imageExtensions.Add(extension.TrimStart('.', ' ').ToLowerInvariant(), matchingFormat);
        }

        public static void AddImageExtension(string extension, ImageFormat matchingFormat)
        {
            lock (_syncExts)
            {//In case first call is to this method, use the property. Will be recursive, but that's fine, since it won't be null.
                imageExtensions.Add(extension.TrimStart('.', ' ').ToLowerInvariant(), matchingFormat);
            }
        }

        /// <summary>
        /// Supports PNG, JPEG, GIF, BMP, and TIFF. Throws a ArgumentOutOfRangeException if not 'Png', 'Jpeg', 'Gif', 'Bmp', or 'Tiff'.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string GetContentTypeFromImageFormat(ImageFormat format)
        {
            if (format == null) throw new ArgumentNullException();

            if (ImageFormat.Png.Equals(format))
                return "image/png"; //Changed from image/x-png to image/png on May 14, 2011, per http://www.w3.org/Graphics/PNG/
            else if (ImageFormat.Jpeg.Equals(format))
                return "image/jpeg";
            else if (ImageFormat.Gif.Equals(format))
                return "image/gif";
            else if (ImageFormat.Bmp.Equals(format))
                return "image/bmp";
            else if (ImageFormat.Tiff.Equals(format))
                return "image/tiff";
            else
            {
                throw new ArgumentOutOfRangeException("Unsupported format " + format.ToString());
            }

        }

        /// <summary>
        /// Returns the first ImageCodeInfo instance with the specified mime type. Returns null if there are no matches.
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public static ImageCodecInfo GetImageCodeInfo(ImageFormat format)
        {
            ImageCodecInfo[] info = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo ici in info)
                if (ici.FormatID == format.Guid) return ici;
            return null;
        }
        public static void SaveJpeg(Image image, Stream target, int quality)
        {
            #region Encoder parameter notes
            //image/jpeg
            //  The parameter list requires 172 bytes.
            //  There are 4 EncoderParameter objects in the array.
            //    Parameter[0]
            //      The category is Transformation.
            //      The data type is Long.
            //      The number of values is 5.
            //    Parameter[1]
            //      The category is Quality.
            //      The data type is LongRange.
            //      The number of values is 1.
            //    Parameter[2]
            //      The category is LuminanceTable.
            //      The data type is Short.
            //      The number of values is 0.
            //    Parameter[3]
            //      The category is ChrominanceTable.
            //      The data type is Short.
            //      The number of values is 0.


            //  http://msdn.microsoft.com/en-us/library/ms533845(VS.85).aspx
            // http://msdn.microsoft.com/en-us/library/ms533844(VS.85).aspx
            // TODO: What about ICC profiles
            #endregion

            //Validate quality
            if (quality < 0) quality = 90; //90 is a very good default to stick with.
            if (quality > 100) quality = 100;
            //Prepare parameter for encoder

            if (quality==100)
            {
                image.Save(target, ImageFormat.Jpeg);                
            }
            else
            {
                using (EncoderParameters p = new EncoderParameters(1))
                {
                    using (var ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality))
                    {
                        p.Param[0] = ep;
                        //save
                        image.Save(target, GetImageCodeInfo(ImageFormat.Jpeg), p);
                    }
                }
            }
            
        }

        /// <summary>
        /// Saves the image in PNG form. If Stream 'target' is not seekable, a temporary MemoryStream will be used to buffer the image data into the stream.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="target"></param>
        public static void SavePng(Image img, Stream target)
        {
            if (!target.CanSeek)
            {
                //Write to an intermediate, seekable memory stream (PNG compression requires it)
                using (MemoryStream ms = new MemoryStream(4096))
                {
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.WriteTo(target);
                }
            }
            else
            {
                //image/png
                //  The parameter list requires 0 bytes.
                img.Save(target, System.Drawing.Imaging.ImageFormat.Png);
            }
        }
        public static void SaveGif(Image img, Stream target)
        {
            //image/gif
            //  The parameter list requires 0 bytes.
            img.Save(target, ImageFormat.Gif);
        }
        public IEnumerable<FileSignature> GetSignatures()
        {
            //Source http://www.filesignatures.net/
            return new FileSignature[]{
                new FileSignature(new byte[] {0xFF, 0xD8, 0xFF}, "jpg", "image/jpeg"),
                new FileSignature(new byte[] {0x42, 0x4D}, "bmp", "image/x-ms-bmp"), //Can be a BMP or DIB
                new FileSignature(new byte[] {0x47,0x49,0x46, 0x38}, "gif", "image/gif"),
                new FileSignature(new byte[] {0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A}, "png","image/png"),
                new FileSignature(new byte[] {0xD7, 0xCD, 0xC6, 0x9A}, "wmf", "image/x-wmf"),
                new FileSignature(new byte[] {0x00, 0x00,0x01, 0x00}, "ico", "image/x-icon"), //Can be a printer spool or an icon
                new FileSignature(new byte[] {0x49, 0x20, 0x49}, "tif", "image/tiff"),
                new FileSignature(new byte[] {0x49, 0x49, 0x2A, 0x00}, "tif", "image/tiff"),
                new FileSignature(new byte[] {0x4D, 0x4D, 0x00, 0x2A}, "tif", "image/tiff"),
                new FileSignature(new byte[] {0x4D, 0x4D, 0x00, 0x2B}, "tif", "image/tiff")
            };
        }
    }
}
