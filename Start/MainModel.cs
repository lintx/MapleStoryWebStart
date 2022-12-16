using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Start
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ModelFieldCopyAttribute : Attribute
    {
    }
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
    public class MainModel : IBaseModel
    {
        private string _filename;
        public MainModel() { }
        internal MainModel(string filename)
        {
            _filename = filename;
        }

        protected virtual bool SetSettingProperty<T>(ref T member, T value, [CallerMemberName] string propertyName = null)
        {
            var result = SetProperty(ref member, value, propertyName);
            Save();
            return result;
        }

        [ModelFieldCopy]
        private bool _hideIdPass = true;
        public bool HideIdPass
        {
            get => _hideIdPass;
            set
            {
                SetSettingProperty(ref _hideIdPass, value);
                OnPropertyChanged("DisplayId");
                OnPropertyChanged("DisplayPass");
            }
        }

        private string _id = string.Empty;
        [YamlIgnore]
        public string Id
        {
            get => _id;
            set
            {
                SetProperty(ref _id, value);
                OnPropertyChanged("DisplayId");
                OnPropertyChanged("HasIdPass");
            }
        }
        [YamlIgnore]
        public string DisplayId
        {
            get
            {
                if (HideIdPass) return new string('*', Id.Length);
                return Id;
            }
        }

        private string _pass = string.Empty;
        [YamlIgnore]
        public string Pass
        {
            get => _pass;
            set
            {
                SetProperty(ref _pass, value);
                OnPropertyChanged("DisplayPass");
                OnPropertyChanged("HasIdPass");
            }
        }
        [YamlIgnore]
        public string DisplayPass
        {
            get
            {
                if (HideIdPass) return new string('*', Pass.Length);
                return Pass;
            }
        }

        [YamlIgnore]
        public bool HasIdPass
        {
            get { return !string.IsNullOrEmpty(Id) && !string.IsNullOrEmpty(Pass); }
        }

        private const string _gameExe = "MapleStory.exe";
        [YamlIgnore]
        public string GameExe
        {
            get => _gameExe;
        }
        [ModelFieldCopy]
        private string _gamePath = string.Empty;
        public string GamePath
        {
            get => _gamePath;
            set
            {
                SetSettingProperty(ref _gamePath, value);
                OnPropertyChanged("GamePathValid");
                OnPropertyChanged("DisplayGamePath");
            }
        }
        [YamlIgnore]
        public bool GamePathValid
        {
            get
            {
                if (string.IsNullOrEmpty(GamePath)) return false;
                if (GameExe != Path.GetFileName(GamePath)) return false;
                return File.Exists(GamePath);
            }
        }
        [YamlIgnore]
        public string DisplayGamePath
        {
            get
            {
                if(string.IsNullOrEmpty(GamePath)) return "没有设置游戏路径\r点击设置";
                return "当前游戏路径：\r" + GamePath + "\r点击重新设置";
            }
        }

        private const string _regKeyString = "beanfunHK";
        [YamlIgnore]
        public string RegKey
        {
            get => _regKeyString;
        }
        [YamlIgnore]
        public bool IsInstall
        {
            get
            {
                try
                {
                    var surekamKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(RegKey);
                    if (surekamKey == null) return false;
                    var shellKey = surekamKey.OpenSubKey("shell");
                    if (shellKey == null) return false;
                    var openKey = shellKey.OpenSubKey("open");
                    if (openKey == null) return false;
                    var commandKey = openKey.OpenSubKey("command");
                    if (commandKey == null) return false;
                    var val = commandKey.GetValue("").ToString();
                    var exePath = Process.GetCurrentProcess().MainModule.FileName;
                    //Console.WriteLine("val:" + val + ",exe:" + exePath);
                    return val.StartsWith(exePath);
                }
                catch
                {
                    return false;
                }
            }
            set
            {
                OnPropertyChanged("IsInstall");
                OnPropertyChanged("InstallButtonContent");
                OnPropertyChanged("InstallButtonText");
            }
        }
        private MaterialDesignThemes.Wpf.PackIcon _installedIcon = new MaterialDesignThemes.Wpf.PackIcon() { Kind = MaterialDesignThemes.Wpf.PackIconKind.ArchiveArrowUpOutline, FontSize = 24 };
        private MaterialDesignThemes.Wpf.PackIcon _notInstalledIcon = new MaterialDesignThemes.Wpf.PackIcon() { Kind = MaterialDesignThemes.Wpf.PackIconKind.ArchiveArrowDownOutline, FontSize = 24 };
        [YamlIgnore]
        public MaterialDesignThemes.Wpf.PackIcon InstallButtonContent
        {
            get
            {
                if (IsInstall)
                {
                    return _installedIcon;
                }
                else
                {
                    return _notInstalledIcon;
                }
            }
        }
        [YamlIgnore]
        public string InstallButtonText
        {
            get
            {
                if (IsInstall)
                {
                    return "已经安装，点击卸载\r卸载后在网页上点击启动游戏按钮将不启动本软件";
                }
                else
                {
                    return "尚未安装，点击安装\r安装后在网页上点击启动游戏按钮将启动本软件";
                }
            }
        }

        [ModelFieldCopy]
        private bool _autoRun = true;
        public bool AutoRun
        {
            get => _autoRun;
            set => SetSettingProperty(ref _autoRun, value);
        }

        [ModelFieldCopy]
        private bool _autoLogin = false;
        public bool AutoLogin
        {
            get => _autoLogin;
            set => SetSettingProperty(ref _autoLogin, value);
        }

        [ModelFieldCopy]
        private bool _skipPlayer = true;
        public bool SkipPlayer
        {
            get => _skipPlayer;
            set => SetSettingProperty(ref _skipPlayer, value);
        }

        [ModelFieldCopy]
        private bool _skipNgs = false;
        public bool SkipNgs
        {
            get => _skipNgs;
            set => SetSettingProperty(ref _skipNgs, value);
        }

        [ModelFieldCopy]
        private bool _preventAutoUpdate = false;
        public bool PreventAutoUpdate
        {
            get => _preventAutoUpdate;
            set => SetSettingProperty(ref _preventAutoUpdate, value);
        }

        [ModelFieldCopy]
        private bool _checkInstalled = true;
        public bool CheckInstalled
        {
            get => _checkInstalled;
            set => SetSettingProperty(ref _checkInstalled, value);
        }

        //游戏路径不对时禁止启动游戏按钮
        //开启流程继续进行梳理，先锁，锁完，先检测安装，未安装的先提示安装，提示安装取消也应进入下面的流程
        //然后先检测游戏路径，游戏路径检测完毕后
        //yesno的方法增加2个参数，分别是确定和取消的，还有final的

        internal void Load()
        {
            if (string.IsNullOrEmpty(_filename)) return;
            FileInfo file = new FileInfo(_filename);
            if (!file.Exists)
            {
                return;
            }
            var des = new DeserializerBuilder()
              .WithNamingConvention(UnderscoredNamingConvention.Instance)
              .IgnoreUnmatchedProperties()//忽略不存在的字段
              .Build();
            var yml = File.ReadAllText(file.FullName);
            MainModel temp = des.Deserialize<MainModel>(yml);
            Type type = GetType();
            FieldInfo[] fieleInfos = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            foreach(var fi in fieleInfos)
            {
                ModelFieldCopyAttribute att = (ModelFieldCopyAttribute)Attribute.GetCustomAttribute(fi, typeof(ModelFieldCopyAttribute));
                if (att == null) continue;
                fi.SetValue(this, fi.GetValue(temp));
                //Console.WriteLine("filed:" + fi.Name + ",val:" + fi.GetValue(this));
            }
#if DEBUG
            //_id = "1";
            //_pass = "2";
#endif
        }

        internal void Save()
        {
            if (string.IsNullOrEmpty(_filename)) return;
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(this);
            try
            {
                File.WriteAllText(_filename, yaml);
            }
            catch
            {
                
            }
}
    }
}
