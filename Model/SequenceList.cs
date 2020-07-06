using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
namespace LSL_Kinect
{
	public enum MarkerType
    {
		[XmlEnum(Name = "Start")]
		Start,
		[XmlEnum(Name = "Stop")]
		Stop,
		[XmlEnum(Name = "Message")]
		Message,
    }

	[XmlType("marker")]
	public class Marker
	{
		[XmlAttribute("affectCSV")]
		public bool affectCSV { get; set; }
		[XmlElement(ElementName = "content")]
		public string Content { get; set; }
		[XmlElement(ElementName = "type")]
		public MarkerType Type { get; set; }

        public override string ToString()
        {
			return "Content : " + Content + "\n" +
				"Type : " + Type.ToString() + "\n";
        }

        public Marker(string content, MarkerType type, bool _affectCSV=false)
        {
            Content = content;
            Type = type;
			affectCSV = _affectCSV;
		}

        public Marker()
        {
			affectCSV = false;
        }
    }

	[XmlType("sequence")]
	public class Sequence
	{
		private int step = 0;
		public Marker PreviousStep
		{ 
			get 
			{ return (step > 0) ? Markers[step - 1] : null ; }
			set { }
		}
		public Marker NextStep
		{
			get
			{ return Markers[step]; }
			set { }
		}

		[XmlElement(ElementName = "name")]
		public string Name { get; set; }
		[XmlElement(ElementName = "marker")]
		public List<Marker> Markers { get; set; }

		public override string ToString()
		{
			string description = "";
			description += "Sequence Name : " + Name + "\n" +
				"Markers : \n";

			foreach (Marker marker in Markers)
            {
				description +=  marker.ToString();
            }
			return description;
		}

		public bool isOnLastStep() { return (step == Markers.Count - 1); }

		public Marker DoNextStep()
        {
			Marker nextMarker = Markers[step];
			step++;
			if(step >= Markers.Count)
            {
				step = 0;
            }
			return nextMarker;
        } 
	}
		
	[XmlRoot(ElementName = "sequenceList")]
	public class SequenceList
	{
		static readonly string XML_SEQUENCES_FILE = Directory.GetCurrentDirectory() + "/SequenceConfig.xml";

		[XmlElement(ElementName = "sequence")]
		public List<Sequence> listSequence { get; set; }
		public override string ToString()
		{
			string description = "";
			description += "Sequence List : \n";

			foreach (Sequence sequence in listSequence)
			{
				description += sequence.ToString();
			}
			return description;
		}

		public static SequenceList Deserialize()
		{
			return Deserialize(XML_SEQUENCES_FILE);
		}


		public static SequenceList Deserialize(string filename)
		{
			// Create an instance of the XmlSerializer.
			XmlSerializer serializer = new XmlSerializer(typeof(SequenceList));

			// Declare an object variable of the type to be deserialized.
			SequenceList sequences = null;

            try
            {
				using (Stream reader = new FileStream(filename, FileMode.Open))
				{
					// Call the Deserialize method to restore the object's state.
					sequences = (SequenceList)serializer.Deserialize(reader);
				}
            }
            catch (FileNotFoundException)
			{
				Debug.WriteLine("Sequence configuration file not found");
            }

			return sequences;
		}

	}

}
