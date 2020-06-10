using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System.Runtime.InteropServices;
using System.Windows;

namespace LSL_Kinect
{
    public static class BitmapGeneration
    {
        #region Members
        static WriteableBitmap _bitmap = null;
        static int _width;
        static int _height;
        static byte[] _pixels = null;
        #endregion

        #region Public methods

        public static BitmapSource ToBitmap(this ColorFrame frame)
        {
            if (_bitmap == null)
            {
                _width = frame.FrameDescription.Width;
                _height = frame.FrameDescription.Height;
                _pixels = new byte[_width * _height * Constantes.BYTES_PER_PIXEL];
                _bitmap = new WriteableBitmap(_width, _height, 
                    Constantes.DPI, Constantes.DPI, Constantes.FORMAT, null);
            }

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(_pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(_pixels, ColorImageFormat.Bgra);
            }

            _bitmap.Lock();

            Marshal.Copy(_pixels, 0, _bitmap.BackBuffer, _pixels.Length);
            _bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));

            _bitmap.Unlock();

            return _bitmap;
        }

        #endregion





    }
}
