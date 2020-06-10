using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL_Kinect.Classes
{
	public class DataContextIdViewModel : INotifyPropertyChanged
    {
		private string _selectedID;
		public string SelectedID
		{
			get { return _selectedID; }
			set
			{
				_selectedID = value;
				this.OnPropertyChanged("SelectedID");
			}
		}

		public ObservableCollection<BodyIdWrapper> IdList { get; set; }

		public DataContextIdViewModel()
		{
			IdList = new ObservableCollection<BodyIdWrapper>();
		}

		public void AddData(BodyIdWrapper newIdWrapper)
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
