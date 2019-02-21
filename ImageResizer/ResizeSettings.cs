using System.Drawing;

namespace ImageResizer
{
    public class ResizeSettings
    {         
        public ScaleMode ScaleMode { get; set; } = ScaleMode.DownscaleOnly;
        public StretchMode StretchMode { get; set; } = StretchMode.Proportionally;
        public ResizeMode ResizeMode { get; set; } = ResizeMode.MaxSides;
        public int Width { get; set; } = 0;
        public int Height { get; set; } = 0;
        public ResizeSettings() { }
        public ResizeSettings(Size size) : this(size.Width, size.Height) { }
        public ResizeSettings(ImageSize imageSize) : this(imageSize.Size) { }
        public ResizeSettings(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }
        public void CompareSides(ref int smallSide, ref int bigSide)
        {
            if (this.Height > this.Width)
            {
                smallSide = this.Width;
                bigSide = this.Height;
            }
            else
            {
                smallSide = this.Height;
                bigSide = this.Width;
            }
        }        
    }
}
