using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace ImageResizer
{
    public class ImageSize
    {
        public Size Size { get; }
        public int ID { get; }
        public string Description { get { return this.Size.Width + "x" + this.Size.Height; } }
        internal ImageSize(int width, int height, int id)
        {
            this.Size = new Size(width: width, height: height);
            this.ID = id;
        }
        public override string ToString()
        {
            return $"width: " + this.Size.Width + ", height: " + this.Size.Height;
        }
        private static List<ImageSize> defaultSizes = new List<ImageSize>();
        public static List<ImageSize> GetDefaults()
        {
            if (defaultSizes.Count == 0)
            {
                defaultSizes.Add(new ImageSize(640, 480, 1));
                defaultSizes.Add(new ImageSize(800, 600, 2));
                defaultSizes.Add(new ImageSize(1024, 768, 3));
                defaultSizes.Add(new ImageSize(1280, 960, 4));
                defaultSizes.Add(new ImageSize(1920, 1080, 5));
            }
            return defaultSizes;
        }
        public static ImageSize GetDefaultOrCreate(Size size)
        {
            ImageSize s = GetDefaults().FirstOrDefault(a => (a.Size.Height == size.Height && a.Size.Width == size.Width));
            if (s == null)
                return new ImageSize(size.Width, size.Height, 0);
            return s;
        }
    }
}
