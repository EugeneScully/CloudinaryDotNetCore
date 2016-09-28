namespace CloudinaryDotNet
{
    /// <summary>
    /// Copy of basic rectangle from System.Drawing
    /// </summary>
    /// <remarks>System.Drawing is not available in .net core.</remarks>
    public struct Rectangle
    {
        public Rectangle(
            int x,
            int y,
            int width,
            int height
        )
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
