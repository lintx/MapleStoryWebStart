using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Start
{
    internal class GameManager
    {
        private uint pid;
        private bool joinEd = false;
        private bool exitEd = false;
        private Process process;
        private MainModel setting;
        private DispatcherTimer timer = new DispatcherTimer();
        internal GameManager(MainModel setting, uint pid)
        {
            this.setting = setting;
            this.pid = pid;
            process = Process.GetProcessById((int)pid);
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        internal void Dispose()
        {
            timer.Tick -= timer_Tick;
            timer.Stop();
            process = null;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (ExitEd)
            {
                return;
            }
            process.Refresh();
            waitTitleClass();
            waitProcess();
        }

        private bool checkProcess()
        {
            if (exitEd) return exitEd;
            if (process.HasExited)
            {
                exitEd = true;
                joinEd = false;
                timer.Stop();
            }
            return exitEd;
        }

        private void waitTitleClass()
        {
            try
            {
                const uint WM_CLOSE = 0x10;
                const int nChars = 256;
                StringBuilder sb = new StringBuilder(nChars);
                if(GetClassName(process.MainWindowHandle, sb, nChars) > 0)
                {
                    string cls = sb.ToString();
                    if(cls == "StartUpDlgClass" && setting.SkipPlayer)
                    {
                        PostMessage(process.MainWindowHandle, WM_CLOSE, 0, 0);
                    }
                    else if(cls == "MapleStoryClassTW")
                    {
                        //已经进入游戏
                        joinEd = true;
                        timer.Stop();
                    }
                }
            }
            catch { }
        }

        private void waitProcess()
        {
            try
            {
                foreach(Process _p in process.GetChildProcesses())
                {
                    if(_p.ProcessName == "BlackXchg.aes" && setting.SkipNgs)
                    {
                        _p.Kill();
                        break;
                    }
                    if(_p.ProcessName == "Patcher" && setting.PreventAutoUpdate)
                    {
                        //该方法尚未验证，需要验证升级窗口的父ID是否是游戏本地，和打开升级窗口后父ID是否已退出
                        _p.Kill();
                        break;
                    }
                }
            }
            catch { }
        }

        internal bool JoinEd
        {
            get { return joinEd; }
        }

        internal bool ExitEd
        {
            get { return checkProcess(); }
        }

        internal bool InputIdPass(string id, string pass)
        {
            if (ExitEd || !JoinEd) return false;
            const int WM_KEYDOWN = 0X100;
            const byte VK_BACK = 0x0008;
            const byte VK_TAB = 0x0009;
            const byte VK_ENTER = 0x000D;
            const byte VK_END = 0x0023;

            //先将游戏窗口激活
            SetForegroundWindow(process.MainWindowHandle);
            //等待100毫秒
            Thread.Sleep(100);

            //将光标移动到帐号输入框末尾
            PostKey(process.MainWindowHandle, WM_KEYDOWN, VK_END);
            //清空帐号输入框内容
            for (int i = 0; i < 64; i++)
            {
                PostKey(process.MainWindowHandle, WM_KEYDOWN, VK_BACK);
            }
            //输入帐号
            PostString(process.MainWindowHandle, id);
            //切换到密码输入框
            PostKey(process.MainWindowHandle, WM_KEYDOWN, VK_TAB);
            //将光标移动到帐号输入框末尾
            PostKey(process.MainWindowHandle, WM_KEYDOWN, VK_END);
            //清空密码输入框内容
            for (int i = 0; i < 20; i++)
            {
                PostKey(process.MainWindowHandle, WM_KEYDOWN, VK_BACK);
            }
            //输入密码
            PostString(process.MainWindowHandle, pass);
            //按回车键进行登录
            PostKey(process.MainWindowHandle, WM_KEYDOWN, VK_ENTER);
            return true;
        }

        //获取激活的窗口句柄
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        //将指定窗口设置为激活状态
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        //根据窗口句柄获取窗口类名
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        //根据窗口类名和窗口名查找窗口句柄
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        //根据窗口句柄查找窗口的进程ID
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern byte MapVirtualKey(byte wCode, int wMap);

        [DllImport("user32.dll")]
        private static extern int PostMessage(IntPtr hWnd, uint wMsg, int wParam, int lParam);

        private static void PostString(IntPtr hwnd, string input)
        {
            const int WM_CHAR = 0x102;
            byte[] chars = ASCIIEncoding.ASCII.GetBytes(input);
            foreach (byte ch in chars)
            {
                PostMessage(hwnd, WM_CHAR, ch, 0);
            }
        }

        private static void PostKey(IntPtr hWnd, uint wMsg, byte wParam)
        {
            PostMessage(hWnd, wMsg, wParam, MapVirtualKey(wParam, 0) << 16 + 1);
        }
    }


    public static class ProcessExtensions
    {
        private static string FindIndexedProcessName(int pid)
        {
            var processName = Process.GetProcessById(pid).ProcessName;
            var processesByName = Process.GetProcessesByName(processName);
            string processIndexdName = null;

            for (var index = 0; index < processesByName.Length; index++)
            {
                processIndexdName = index == 0 ? processName : processName + "#" + index;
                var processId = new PerformanceCounter("Process", "ID Process", processIndexdName);
                if ((int)processId.NextValue() == pid)
                {
                    Console.WriteLine(processIndexdName + "," + pid);
                    return processIndexdName;
                }
            }

            return processIndexdName;
        }

        private static Process FindPidFromIndexedProcessName(string indexedProcessName)
        {
            var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);

            return Process.GetProcessById((int)parentId.NextValue());
        }

        //使用性能分析器获取进程父进程
        public static Process Parent(this Process process)
        {
            return FindPidFromIndexedProcessName(FindIndexedProcessName(process.Id));
        }

        //获取进程的子进程列表
        public static IList<Process> GetChildProcesses(this Process process)
        => new ManagementObjectSearcher(
                $"Select * From Win32_Process Where ParentProcessID={process.Id}")
                .Get()
                .Cast<ManagementObject>()
                .Select(mo =>
                    Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])))
                .ToList();
    }
}
