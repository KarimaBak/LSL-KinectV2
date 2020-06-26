using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LSL_Kinect.Classes
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Sequence> SequenceList { get; set; }

        private Sequence currentSequence;

        public Sequence CurrentSequence
        {
            set
            {
                currentSequence = value;
                OnPropertyChanged("CurrentSequence");
                OnPropertyChanged("NextStep");
                OnPropertyChanged("PreviousStep");
            }
            get
            {
                return currentSequence;
            }
        }

        public void ActualizeStep()
        {
            OnPropertyChanged("NextStep");
            OnPropertyChanged("PreviousStep");
        }

        public string NextStep
        {
            set
            { }
            get
            {
                return (CurrentSequence != null && CurrentSequence.NextStep != null) ? CurrentSequence.NextStep.Content : string.Empty;
            }
        }

        public string PreviousStep
        {
            set {}
            get
            {
                return (CurrentSequence != null && CurrentSequence.PreviousStep != null) ? CurrentSequence.PreviousStep.Content : string.Empty;
            }
        }

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

        #endregion INotifyPropertyChanged Members
    }
}