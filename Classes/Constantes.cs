using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LSL_Kinect
{
    public static class Constantes
    {

            #region Constants

            
            public static readonly double DPI = 96.0;

            
            public static readonly PixelFormat FORMAT = PixelFormats.Bgr32;

            
            public static readonly int BYTES_PER_PIXEL = (FORMAT.BitsPerPixel + 7) / 8;

            #endregion
        
    }
}
