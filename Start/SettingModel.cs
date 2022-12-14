using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Start
{
    public abstract class IBaseModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets property if it does not equal existing value. Notifies listeners if change occurs.
        /// </summary>
        /// <typeparam name="T">Type of property.</typeparam>
        /// <param name="member">The property's backing field.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">Name of the property used to notify listeners.  This
        /// value is optional and can be provided automatically when invoked from compilers
        /// that support <see cref="CallerMemberNameAttribute"/>.</param>
        protected virtual bool SetProperty<T>(ref T member, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(member, value))
            {
                return false;
            }

            member = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">Name of the property, used to notify listeners.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public class SettingModel : IBaseModel
    {
        private string _gamePath = string.Empty;
        public string GamePath
        {
            get => _gamePath;
            set => SetProperty(ref _gamePath, value);
        }

        private bool _autoRun = true;
        public bool AutoRun
        {
            get => _autoRun;
            set => SetProperty(ref _autoRun, value);
        }

        private bool _hideIdPass = true;
        public bool HideIdPass
        {
            get => _hideIdPass;
            set => SetProperty(ref _hideIdPass, value);
        }

        private bool _autoLogin = false;
        public bool AutoLogin
        {
            get => _autoLogin;
            set => SetProperty(ref _autoLogin, value);
        }

        private bool _skipPlayer = true;
        public bool SkipPlayer
        {
            get => _skipPlayer;
            set => SetProperty(ref _skipPlayer, value);
        }

        private bool _skipNgs = false;
        public bool SkipNgs
        {
            get => _skipNgs;
            set => SetProperty(ref _skipNgs, value);
        }

        private bool _preventAutoUpdate = false;
        public bool PreventAutoUpdate
        {
            get => _preventAutoUpdate;
            set => SetProperty(ref _preventAutoUpdate, value);
        }

        private bool _checkInstalled = true;
        public bool CheckInstalled
        {
            get => _checkInstalled;
            set => SetProperty(ref _checkInstalled, value);
        }
    }
}
