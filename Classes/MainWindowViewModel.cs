﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace LSL_Kinect.Classes
{
	public class MainWindowViewModel : INotifyPropertyChanged
    {
		public ObservableCollection<BodyIdWrapper> IdList { get; set; }

		public MainWindowViewModel()
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