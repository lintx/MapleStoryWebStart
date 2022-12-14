using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Start
{
    public class IdPassModel : IBaseModel
    {
        private bool _hideIdPass = true;
        public bool HideIdPass
        {
            get => _hideIdPass;
            set {
                SetProperty(ref _hideIdPass, value);
                OnPropertyChanged("DisplayId");
                OnPropertyChanged("DisplayPass");
            }
        }

        private string _id = string.Empty;
        public string Id
        {
            get => _id;
            set {
                SetProperty(ref _id, value);
                OnPropertyChanged("DisplayId");
            }
        }
        public string DisplayId
        {
            get
            {
                if (HideIdPass) return new string('*', Id.Length);
                return Id;
            }
        }

        private string _pass = string.Empty;
        public string Pass
        {
            get => _pass;
            set
            {
                SetProperty(ref _pass, value);
                OnPropertyChanged("DisplayPass");
            }
        }
        public string DisplayPass
        {
            get
            {
                if (HideIdPass) return new string('*', Pass.Length);
                return Pass;
            }
        }
    }
}
