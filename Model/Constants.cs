using System;
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

        public static readonly float KINECT_COLOR_CAMERA_WIDTH = 1920;
        public static readonly float KINECT_COLOR_CAMERA_HEIGHT = 1080;

        //This is an estimation
        public const double WIDTH_RATIO_BETWEEN_DEPTH_AND_COLOR = 0.76;
        public static readonly int CROPPED_CAMERA_WIDTH =
            Convert.ToInt32(WIDTH_RATIO_BETWEEN_DEPTH_AND_COLOR * KINECT_COLOR_CAMERA_WIDTH);

        public static readonly int PIXEL_FORMAT_OFFSET_BETWEEN_DEPTH_AND_COLOR =
            Convert.ToInt32((1 - WIDTH_RATIO_BETWEEN_DEPTH_AND_COLOR) / 2 * KINECT_COLOR_CAMERA_WIDTH);

        //Rought of estimation of an offset that aim to compensate the differents angless/positions/centers betweeen the two cameras
        public static readonly int PIXEL_POSITION_OFFSET_BETWEEN_DEPTH_AND_COLOR =
            Convert.ToInt32(0.02 * KINECT_COLOR_CAMERA_WIDTH);
        //Total offset between the two camera
        public static readonly int PIXEL_TOTAL_OFFSET_BETWEEN_DEPTH_AND_COLOR =
            PIXEL_FORMAT_OFFSET_BETWEEN_DEPTH_AND_COLOR + PIXEL_POSITION_OFFSET_BETWEEN_DEPTH_AND_COLOR;

        #endregion Constants
    }
}