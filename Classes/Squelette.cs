using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Microsoft.Kinect;
using System.Globalization;

namespace LSL_Kinect
{
    public class Squelette : IDisposable
    {
        private long timeStamp;
        private double x;
        private double y;
        private double z;
        private int id;
        private JointType jointTypes;
        private HandState handLeftState;
        private HandState handRightState;
        private TrackingConfidence handLeftConfidence;
        private TrackingConfidence handRightConfidence;
        private TrackingState trackingState;
        private IntPtr handle;
        private Component component = new Component();
        private bool disposed = false;
        private string abregeTrk=null;
        private string abregeHandLeft = null;
        private string abregeHandRight = null;
        private string abregeHandConfLeft = null;
        private string abregeHandConfRight = null;


        public Squelette(long _timeStamp, int _ID, double _x, double _y, double _z, JointType _jointTypes,string _abregeHandLeft,string _abregeHandConfLeft, string _abregeHandRight, string _abregeHandConfRight, string _abregeTrk)
        {
            this.timeStamp = _timeStamp;
            this.id = _ID;
            this.x = _x;
            this.y = _y;
            this.z = _z;
            this.jointTypes = _jointTypes;
            this.abregeHandLeft= _abregeHandLeft;
            this.abregeHandConfLeft = _abregeHandConfLeft;
            this.abregeHandConfRight = _abregeHandConfLeft;
            this.abregeHandRight = _abregeHandLeft;
            this.abregeTrk = _abregeTrk;

        }
        public long Timestamp { get => timeStamp; set => timeStamp = value; }
        public int ID { get => id; set => id = value; }
        public double X { get => x; set => x = value; }
        public double Y { get => y; set => y = value; }
        public double Z { get => z; set => z = value; }
        public JointType JointTypes { get => jointTypes; set => jointTypes = value; }
        public HandState HandLeftState { get => handLeftState; set => handLeftState = value; }
        public TrackingConfidence HandLeftConfidence { get => handLeftConfidence; set => handLeftConfidence = value; }
        public TrackingConfidence HandRightConfidence { get => handRightConfidence; set => handRightConfidence = value; }
        public HandState HandRightState { get => handRightState; set => handRightState = value; }
        public TrackingState TracKingState { get => trackingState; set => trackingState= value; }
        public string AbregeTrk { get => abregeTrk; set => abregeTrk= value; }
        public string AbregeHandStateLeft { get => abregeHandLeft; set => abregeHandLeft = value; }
        public string AbregeHandStateRight { get => abregeHandRight; set => abregeHandRight = value; }
        public string AbregeHandConfLeft { get => abregeHandConfLeft; set => abregeHandConfLeft = value; }
        public string AbregeHandConfRight { get => abregeHandConfRight; set => abregeHandConfRight = value; }


        public Squelette(IntPtr handle)
        {
            this.handle = handle;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    component.Dispose();
                }
                CloseHandle(handle);
                handle = IntPtr.Zero;
                disposed = true;
            }
        }

        [System.Runtime.InteropServices.DllImport("Kernel32")]
        private extern static Boolean CloseHandle(IntPtr handle);
        ~Squelette()
        {
            Dispose(false);
        }

    }
}
