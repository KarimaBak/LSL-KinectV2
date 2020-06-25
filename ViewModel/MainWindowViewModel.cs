using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Documents;
using System.Windows.Input;

namespace LSL_Kinect.Classes
{
	public class MainWindowViewModel : INotifyPropertyChanged
    {
		public ObservableCollection<Sequence> SequenceList { get; set; }

		public ObservableCollection<BodyIdWrapper> IdList { get; set; }

		private string csvPath;
		public string CsvPath
		{
			set
			{
				csvPath = value;
				OnPropertyChanged("CsvPath");
			}
			get
			{
				return csvPath;
			}
		}

		public MainWindowViewModel()
		{
			IdList = new ObservableCollection<BodyIdWrapper>();
			SequenceList = new ObservableCollection<Sequence>();
		}

		public void AddAllSequences(SequenceList newSequences)
		{
            foreach (Sequence sequence in newSequences.listSequence)
            {
				SequenceList.Add(sequence);
			}
		}

			public void AddBodyID(BodyIdWrapper newIdWrapper)
        {
			IdList.Add(newIdWrapper);
		}


		#region INotifyPropertyChanged Members

		private void NotifyPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion

	}
}
