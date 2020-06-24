using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using System.Runtime.InteropServices;
using System.Windows;
using System.Diagnostics;
using System;

namespace LSL_Kinect
{
    public static class BitmapGeneration
    {
        #region Members
        static WriteableBitmap _bitmap = null;
        static int _width;
        static int _height;
        static byte[] _pixels = null;
        static readonly Duration waitingDuration = new Duration(new System.TimeSpan(0));
        #endregion

        #region Public methods

        public static BitmapSource ToBitmap(this ColorFrame frame)
        {
            if (_bitmap == null)
            {
                _width = frame.FrameDescription.Width;
                _height = frame.FrameDescription.Height;
                _pixels = new byte[_width * _height * Constants.BYTES_PER_PIXEL];
                _bitmap = new WriteableBitmap(_width, _height,
                    Constants.DPI, Constants.DPI, Constants.FORMAT, null);
            }


            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(_pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(_pixels, ColorImageFormat.Bgra);
            }

            try
            {
                if (_bitmap.TryLock(waitingDuration))
                {
                    Marshal.Copy(_pixels, 0, _bitmap.BackBuffer, _pixels.Length);
                    _bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));

                    _bitmap.Unlock();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Frame loss","Warning");
            }
            return _bitmap;
        }


        #endregion





    }
}
