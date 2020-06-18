using System.Windows.Media;

namespace LSL_Kinect
{
    public static class Constants
    {
        #region Constants
        //TODO Move to ressources file or smth
        public static readonly double DPI = 96.0;
        public static readonly PixelFormat FORMAT = PixelFormats.Bgr32;
        public static readonly int BYTES_PER_PIXEL = (FORMAT.BitsPerPixel + 7) / 8;

        #endregion Constants
    }
}