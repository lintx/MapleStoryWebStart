using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Start
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private GameManager gameManager = null;
        private IdPassModel IdPass = new IdPassModel();
        public MainWindow()
        {
            InitializeComponent();
            Environment.CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            //DataContext = this;
            idPassPanel.DataContext = IdPass;
            InitMutex();
            LoadSetting();
            Loaded += (s, e) =>
            {
                //GamePathValid();
                //窗体布局属性等加载完毕，但是还没有呈现
            };
            ContentRendered += (s, e) =>
            {
                //窗体内容已经呈现完毕
                SetInstallButtonContent();
                //Thread.Sleep(2000);
                //IdPass.Id = "1111";
                if (Setting.CheckInstalled && !Installed())
                {
                    yesNoDialogYesBtnClick = (s1, p) => {
                        Install();
                        CheckRunArgs();
                    };
                    ShowYesNodDialog("检测到程序尚未安装，是否立即安装？");
                    return;
                }
                CheckRunArgs();
            };
        }

        #region 游戏启动相关
        [DllImport("LRInject.dll", EntryPoint = "LRInject", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint LRInject(string application, string workpath, string commandline, string dllpath, uint CodePage, bool HookIME);
        private class BeanfunArgs
        {
            public string SN = string.Empty;
            public string Cmd = string.Empty;
            public string WebToken = string.Empty;
            public string SecretCode = string.Empty;
            public string Data = string.Empty;

            public string ppppp = string.Empty;
            public string ServiceCode = string.Empty;
            public string ServiceRegion = string.Empty;
            public string ServiceAccount = string.Empty;
            public string CreateTime = string.Empty;    //帐号建立时间
            public string BeanfunUrl = string.Empty;
            public string WebStartPatch = string.Empty;
        }
        private BeanfunArgs beanfunArgs = null;
        private string[] argDelimiter = new string[1]{ "&&&&" };
        private char[] keyValDelimiter = new char[1] { '=' };
        private string[] keys = { "bac987d65e432f10", "3bc4d5e6f2a79108", "cdbeaf9012456378", "4e6fb81a3c5d7092", "bdef1246789ac530", "5f82cb4093e71d6a", "df1468ace0357b92", "b50c61a4f93e82d7" };
        private const string getOtpUrl = "{0}beanfun_block/generic_handlers/get_webstart_otp.ashx?SN={1}&WebToken={2}&SecretCode={3}&ppppp={4}&ServiceCode={5}&ServiceRegion={6}&ServiceAccount={7}&CreateTime={8}&d={9}";
        private const string adapterUrl = "{0}generic_handlers/adapter.ashx?cmd=06002&sn={1}&result={2}&d={3}";
        //private List<string> keys = new List<string>();

        private void CheckRunArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length >= 2)
            {
                EncodeRunArgs(args[1]);
            }
        }

        private void EncodeRunArgs(string cmd)
        {
            //Console.WriteLine("run command:"+cmd);
            try
            {
                var argCls = new BeanfunArgs();
                var argType = argCls.GetType();
                string[] args = cmd.Split(argDelimiter, StringSplitOptions.RemoveEmptyEntries);
                args.ToList().ForEach(arg => {
                    string[] kv = arg.Split(keyValDelimiter,2);
                    if (kv == null || kv.Length < 2) return;
                    var kf = argType.GetField(kv[0]);
                    if (kf == null) return;
                    kf.SetValue(argCls, kv[1]);
                });
                //argType.GetFields().ToList().ForEach(arg => {
                //    Console.WriteLine("key:" + arg.Name + ",val:" + arg.GetValue(argCls));
                //});
                if (argCls.Data == string.Empty) return;
                int num = Convert.ToInt32(argCls.Data.Substring(0, 1), 16);
                int keyIdx = num % 4;
                //Console.WriteLine("keyIdx:" + keyIdx);
                string str = argCls.Data.Substring(1);
                //Console.WriteLine("str:" + str);
                if(keyIdx >= keys.Length)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => Alert("错误", "解密密钥获取失败！")));
                    return;
                }
                var key = keys[keyIdx];
                var result = string.Empty;
                var ks = key.Length;
                for (int i = 0, size = str.Length; i < size; i++)
                {
                    var c = str[i];
                    for (int l = 0; l < ks; l++)
                    {
                        if (key[l] == c)
                        {
                            result += l.ToString("x");//将位置转换为16进制后拼接
                            break;
                        }
                    }
                }
                //Console.WriteLine("result:" + result);
                string key1 = result.Substring(num + 1, 8);
                result = result.Remove(num + 1, 8);
                //Console.WriteLine("key1:" + key1 + ",text:" + result);
                result = EncodeString(result, key1);
                //Console.WriteLine("encodeing:" + result);

                var arr1 = result.Split(';');
                if (arr1.Length < 1) return;
                arr1[0].Split(argDelimiter, StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(arg => {
                    string[] kv = arg.Split(keyValDelimiter, 2);
                    if (kv == null || kv.Length < 2) return;
                    var kf = argType.GetField(kv[0]);
                    if (kf == null) return;
                    kf.SetValue(argCls, kv[1]);
                });
                if(argCls.ServiceCode != "610074" || argCls.ServiceRegion != "T9")
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => Alert("错误", "插件当前只支持新枫之谷港区帐号启动游戏！")));
                    return;
                }
                IdPass.Id = argCls.ServiceAccount;
                beanfunArgs = argCls;
                GetOtp();
                //argType.GetFields().ToList().ForEach(arg =>
                //{
                //    Console.WriteLine("key:" + arg.Name + ",val:" + arg.GetValue(argCls));
                //});
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => Alert("错误", ex.Message)));
            }
        }

        private void GetOtp(bool notice = true)
        {
            if (beanfunArgs == null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => Alert("错误", "请先从网页上点击启动游戏！")));
                return;
            }
            if (notice)
            {
                //空请求告诉服务器启动成功
                string adapter = string.Format(adapterUrl, beanfunArgs.BeanfunUrl, beanfunArgs.SN, "1", Environment.TickCount);
                GetHttp(adapter);
            }
            string otpUrl = string.Format(getOtpUrl, beanfunArgs.BeanfunUrl, beanfunArgs.SN, beanfunArgs.WebToken, beanfunArgs.SecretCode, beanfunArgs.ppppp, beanfunArgs.ServiceCode, beanfunArgs.ServiceRegion, beanfunArgs.ServiceAccount, beanfunArgs.CreateTime, Environment.TickCount);
            //Console.WriteLine(otpUrl);
            //请求数据，如果需要，还需要先请求空请求
            string httpResult = GetHttp(otpUrl);
            //Console.WriteLine(httpResult);
            var arr = httpResult.Split(';');
            if (arr.Length < 2)
            {
                Alert("错误", "获取密码出错！\r" + httpResult);
                return;
            }
            if (arr[0] != "1")
            {
                Alert("错误", "获取密码出错！\r" + arr[1]);
                return;
            }
            string key = arr[1].Substring(0, 8);
            IdPass.Pass = EncodeString(arr[1].Remove(0, 8), key);
            //Console.WriteLine("result:" + IdPass.Pass);
            if (Setting.AutoRun) RunMapleStory();
        }

        private string GetHttp(string url)
        {
            string responseContent = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            using(var wr = req.GetResponse())
            using(var stream = wr.GetResponseStream())
            using(var reader = new StreamReader(stream, Encoding.UTF8))
            {
                responseContent = reader.ReadToEnd();
            }
            return responseContent;
        }

        private string EncodeString(string encData, string key)
        {
            DESCryptoServiceProvider descryptoServiceProvider = new DESCryptoServiceProvider();
            descryptoServiceProvider.Key = Encoding.UTF8.GetBytes(key);
            descryptoServiceProvider.Mode = CipherMode.ECB;
            descryptoServiceProvider.Padding = PaddingMode.None;
            ICryptoTransform cryptoTransform = descryptoServiceProvider.CreateDecryptor();
            byte[] array = HexString2Bytes(encData);//重命名：hex,两两一组转字节数组
            descryptoServiceProvider.Clear();
            return Encoding.UTF8.GetString(cryptoTransform.TransformFinalBlock(array, 0, array.Length)).TrimEnd(new char[1]);
        }

        private byte[] HexString2Bytes(string str)
        {
            byte[] array = Enumerable.Range(0, str.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(str.Substring(x, 2), 16))
                .ToArray<byte>();
            return array;
        }

        private void RunMapleStory()
        {
            if (!GamePathValid())
            {
                yesNoDialogYesBtnClick = (s, p) => {
                    SelectGamePathTextBox_PreviewMouseLeftButtonUp(null, null);
                    SaveSetting();
                    RunMapleStory();
                };
                ShowYesNodDialog("游戏目录无效！是否选择游戏目录？");
                return;
            }

            List<Process> processList = new List<Process>();
            string gameProcessName = Path.GetFileNameWithoutExtension(gameExe);
            foreach (Process process in Process.GetProcessesByName(gameProcessName))
            {
                try
                {
                    if (process.MainModule.FileName == Setting.GamePath) processList.Add(process);
                }
                catch { }
            }

            if (processList.Count > 0)
            {
                yesNoDialogYesBtnClick = (s, p) => {
                    foreach(Process process in processList)
                    {
                        try
                        {
                            process.Kill();
                        }
                        catch { }
                    }
                    Thread.Sleep(100);
                    RunMapleStory();
                };
                ShowYesNodDialog("检测到游戏正在运行，是否结束游戏后重新运行？");
                return;
            }
            new Thread(new ThreadStart(() => {
                string path = Setting.GamePath;
                string commandLine = Setting.AutoLogin ? $"{gameExe} tw.login.maplestory.beanfun.com 8484 BeanFun {IdPass.Id} {IdPass.Pass}" : gameExe;
                string dllPath = $"{Environment.CurrentDirectory}\\LRHookx64.dll";
                //MessageBox.Show(path + "," + commandLine + "," + dllPath);
                uint pid = LRInject(path, Path.GetDirectoryName(path), commandLine, dllPath, (uint)System.Globalization.CultureInfo.GetCultureInfo("zh-HK").TextInfo.ANSICodePage, Environment.OSVersion.Version >= new Version(6, 2));
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    //这里要放到主线程执行，否则会造成计时器退出
                    gameManager = new GameManager(Setting, pid);
                }));
            })).Start();
        }
        private void CopyIdButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(IdPass.Id);
        }

        private void CopyPassButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(IdPass.Pass);
        }
        private void InputIdPassButton_Click(object sender, RoutedEventArgs e)
        {
            if(!gameManager.InputIdPass(IdPass.Id, IdPass.Pass))
            {
                Alert("错误", "请检查游戏是否运行！");
            }
        }
        private void RefreshPassButton_Click(object sender, RoutedEventArgs e)
        {
            GetOtp(false);
        }

        private void RunGameButton_Click(object sender, RoutedEventArgs e)
        {
            RunMapleStory();
        }
        #endregion

        #region 进程通信
        private const string AppMutexName = "MapleStoryLinTxWebStartMutex";
        private const string AppNamedPipeName = "MapleStoryLinTxWebStartNamedPipe";
        private const string AppRunCommand = "run";
        private Mutex mutex = null;

        private void InitMutex()
        {
            if (CreateMutex())
            {
                ListenerPipeMessage();
            }
            else
            {
                string[] args = Environment.GetCommandLineArgs();
                //已经有其他实例在运行，判断有无启动参数，有就传参数，无就传"run"
                if (args.Length <= 1)
                {
                    SendPipeMessage(AppRunCommand);
                }
                else
                {
                    SendPipeMessage(args[1]);
                }
                Application.Current.Shutdown();
            }
        }

        private void ListenerPipeMessage()
        {
            try
            {
                //建立异步管道服务端
                var server = new NamedPipeServerStream(AppNamedPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                //异步等待连接
                server.BeginWaitForConnection(o => {
                    //先获取实例
                    var pipe = (NamedPipeServerStream)o.AsyncState;
                    //然后结束等待监听
                    pipe.EndWaitForConnection(o);
                    //获取reader
                    var sr = new StreamReader(pipe);
                    //直接读取数据
                    string line = sr.ReadLine();
                    Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        Activate();
                        if(line != null && line != "" && line != AppRunCommand)
                        {
                            EncodeRunArgs(line);
                        }
                    }));
                    //如果需要读取多条数据，可以用类似下面的方法
                    //string line;
                    //while (pipe.IsConnected)
                    //{
                    //    while ((line = sr.ReadLine()) != null)
                    //    {
                    //        Console.WriteLine(line);
                    //    }
                    //}
                    //视为通讯结束
                    pipe.Disconnect();
                    pipe.Dispose();
                    //重新开启一个服务端
                    ListenerPipeMessage();
                }, server);
            }catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => Alert("错误", ex.Message)));
            }
        }

        private void SendPipeMessage(string message)
        {
            try
            {
                using (var client = new NamedPipeClientStream(AppNamedPipeName))
                {
                    client.Connect();
                    using(var sw = new StreamWriter(client) { AutoFlush = true})
                    {
                        sw.WriteLine(message);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private bool CreateMutex()
        {
            bool ok = false;
            mutex = new Mutex(false, AppMutexName, out ok);
            return ok;
        }
        #endregion

        #region 设置
        public SettingModel Setting = new SettingModel();
        private const string settingFile = "./setting.yml";
        private const string gameExe = "MapleStory.exe";
        private void LoadSetting()
        {
            FileInfo file = new FileInfo(settingFile);
            if (!file.Exists)
            {
                settingDialog.DataContext = Setting;
                return;
            }
            var des = new DeserializerBuilder()
              .WithNamingConvention(UnderscoredNamingConvention.Instance)
              .IgnoreUnmatchedProperties()//忽略不存在的字段
              .Build();
            var yml = File.ReadAllText(file.FullName);
            Setting = des.Deserialize<SettingModel>(yml);
            settingDialog.DataContext = Setting;
            IdPass.HideIdPass = Setting.HideIdPass;
        }

        private bool SaveSetting()
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(Setting);
            try
            {
                File.WriteAllText(settingFile, yaml);
            }
            catch (Exception ex)
            {
                SettingAlert("保存失败", ex.Message);
                return false;
            }
            IdPass.HideIdPass = Setting.HideIdPass;
            return true;
        }
        private void SaveSettingButton_Click(object sender, RoutedEventArgs e)
        {
            if (!GamePathValid())
            {
                SettingAlert("错误", "游戏目录无效！");
                return;
            }
            if (SaveSetting())
            {
                DialogHost.Close("RootDialog");
            }
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            DialogHost.Show(settingDialog, "RootDialog");
        }
        private void SelectGamePathTextBox_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Setting.GamePath = string.Empty;
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = $"新枫之谷游戏文件 ({gameExe})|{gameExe}",
                Title = "选择新枫之谷游戏目录",
                //FileName = Setting.GamePath,
                //InitialDirectory = Path.GetDirectoryName(Setting.GamePath),
            };
            if (!string.IsNullOrEmpty(Setting.GamePath))
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(Setting.GamePath);
            }
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                Setting.GamePath = openFileDialog.FileName;
            }
        }
        private bool GamePathValid()
        {
            if (string.IsNullOrEmpty(Setting.GamePath)) return false;
            if (gameExe != Path.GetFileName(Setting.GamePath)) return false;
            return File.Exists(Setting.GamePath);
        }
        #endregion

        #region 提示框
        private RoutedEventHandler yesNoDialogYesBtnClick = (sender, args) => { };
        private void yesNoDialogYesBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogHost.Close("RootDialog");
            yesNoDialogYesBtnClick(sender, e);
        }

        private void ShowYesNodDialog(string message)
        {
            ShowYesNodDialog("提示", message);
        }
        private void ShowYesNodDialog(string title, string message)
        {
            yesNoDialogTitleTxt.Text = "提示";
            yesNoDialogTxt.Text = message;
            DialogHost.Show(yesNoDialog, "RootDialog");
        }

        private void Alert(string title, string content)
        {
            yesDialogTitleTxt.Text = title;
            yesDialogTxt.Text = content;
            DialogHost.Show(yesDialog, "RootDialog");
        }

        private void SettingAlert(string title, string content)
        {
            yesDialogTitleTxt.Text = title;
            yesDialogTxt.Text = content;
            DialogHost.Show(yesDialog, "settingDialogHost");
        }
        #endregion

        #region 安装和卸载
        private const string RegKeyString = "beanfunHK";

        private bool Installed()
        {
            try
            {
                var surekamKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(RegKeyString);
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

        private void SetInstallButtonContent()
        {
            if (Installed())
            {
                InstallButton.Content = "卸载";
            }
            else
            {
                InstallButton.Content = "安装";
            }
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (Installed())
            {
                Uninstall();
            }
            else
            {
                Install();
            }
            SetInstallButtonContent();
        }

        private void Install()
        {
            Uninstall();
            try
            {
                //注册的协议头，即在地址栏中的路径 如QQ的：tencent://xxxxx/xxx 我注册的是jun 在地址栏中输入：jun:// 就能打开本程序
                var exePath = Process.GetCurrentProcess().MainModule.FileName;
                var surekamKey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(RegKeyString);
                surekamKey.SetValue("", "URL: beanfunhk Protocol");
                surekamKey.SetValue("URL Protocol", "");
                var defaultIconKey = surekamKey.CreateSubKey("DefaultIcon");
                defaultIconKey.SetValue("", exePath);
                //以下这些参数都是固定的，不需要更改，直接复制过去 
                var shellKey = surekamKey.CreateSubKey("shell");
                var openKey = shellKey.CreateSubKey("open");
                var commandKey = openKey.CreateSubKey("command");
                //这里可执行文件取当前程序全路径，可根据需要修改
                commandKey.SetValue("", exePath + " \"%1\"");
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => Alert("错误", ex.Message)));
            }
        }

        private void Uninstall()
        {
            try
            {
                if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(RegKeyString) != null) Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(RegKeyString);
            }
            catch (Exception ex)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, (Action)(() => Alert("错误", ex.Message)));
            }
        }

        #endregion
    }
}
