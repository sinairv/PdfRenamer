using System.ComponentModel;
using System;
namespace PdfRenamer
{
    public class PdfItemInfo: INotifyPropertyChanged
    {
        public PdfItemInfo(string origName, string sugName, bool valid)
        {
            OriginalName = origName;
            SuggestedName = sugName;
            Checked = valid;
        }

        private bool _isChecked = false;
        public bool Checked 
        { 
            get { return _isChecked; } 
            set
            {
                _isChecked = value; 
                OnPropertyChanged("Checked");
            } 
        }

        private string _originalName;
        public string OriginalName
        {
            get { return _originalName; }
            protected set 
            { 
                _originalName = value;
                OnPropertyChanged("OriginalName");
            }
        }

        private string _suggestedName;
        public string SuggestedName
        {
            get { return _suggestedName; }
            set 
            { 
                _suggestedName = value;
                OnPropertyChanged("SuggestedName");
            }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set 
            { 
                _message = value;
                OnPropertyChanged("Message");
            }
        }

        public bool IsValid { get; set; }

        public bool HasMessage
        {
            get { return !String.IsNullOrWhiteSpace(_message); }
        }


        public virtual void OnPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
