using System;


namespace LSL_Kinect.Classes
{
	public class BodyIdWrapper
	{
		private const ulong KINECT_MINIMAL_ID = 72057594037900000;

		public BodyIdWrapper(ulong _kinectID)
		{
			kinectID = _kinectID;
			shortIDString = (_kinectID - KINECT_MINIMAL_ID).ToString();
		}

		public ulong kinectID { get; set; }
		public string shortIDString { get; set; }
	}
}