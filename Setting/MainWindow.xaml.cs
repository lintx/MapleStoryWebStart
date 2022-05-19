using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;
using Microsoft.Win32;

namespace Setting
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        const string RK_SW = "SOFTWARE";
        const string RK_PATH = "GAMANIA\\MapleStory";
        const string RK_KEY_PATH = "Path";
        const string RK_KEY_G_PATH = "GamePath";
        const string GAME_EXE = "MapleStory.exe";
        public MainWindow()
        {
            InitializeComponent();
            string val = GetRegistryValue(RK_KEY_G_PATH);
            if (val == String.Empty)
            {
                val = GetRegistryValue(RK_KEY_PATH);
            }
            t_path.Text = val;
        }

        private string GetRegistryValue(string path)
        {
            using (RegistryKey lm = Registry.LocalMachine)
            {
                try
                {
                    RegistryKey software = lm.OpenSubKey(RK_SW, true);
                    RegistryKey product = software.CreateSubKey(RK_PATH);
                    return product.GetValue(path).ToString();
                }
                catch (Exception)
                {

                }
            }
            return String.Empty;
        }

        private bool SetRegistryValue(string path, string value)
        {
            using (RegistryKey lm = Registry.LocalMachine)
            {
                try
                {
                    RegistryKey software = lm.OpenSubKey(RK_SW, true);
                    RegistryKey product = software.CreateSubKey(RK_PATH);
                    product.SetValue(path, value);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private bool DelRegistryValue(string path)
        {
            using (RegistryKey lm = Registry.LocalMachine)
            {
                try
                {
                    RegistryKey software = lm.OpenSubKey(RK_SW, true);
                    RegistryKey product = software.CreateSubKey(RK_PATH);
                    product.DeleteValue(path);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        private void Btn_Select_Game_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = "枫之谷游戏文件 (MapleStory.exe)|MapleStory.exe"
            };
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                this.t_path.Text = Path.GetDirectoryName(openFileDialog.FileName);
            }
        }

        private void Btn_Apply_Click(object sender, RoutedEventArgs e)
        {
            string path = this.t_path.Text;
            string exe = Path.Combine(path, GAME_EXE);
            if (!File.Exists(exe))
            {
                MessageBox.Show("游戏目录下没有检测到新枫之谷游戏文件！","错误",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }
            string cp = Environment.CurrentDirectory;
            if (!File.Exists(Path.Combine(cp, GAME_EXE)))
            {
                MessageBox.Show("当前目录下没有检测到MapleStory.exe！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            bool isOk = SetRegistryValue(RK_KEY_G_PATH, path);
            if (!isOk)
            {
                MessageBox.Show("写入注册表失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            isOk = SetRegistryValue(RK_KEY_PATH, cp);
            if (!isOk)
            {
                MessageBox.Show("写入注册表失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MessageBox.Show("设置成功！你现在可以去启动游戏了！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Btn_Delete_Click(object sender, RoutedEventArgs e)
        {
            string path = this.t_path.Text;
            string exe = Path.Combine(path, GAME_EXE);
            if (!File.Exists(exe))
            {
                MessageBox.Show("游戏目录下没有检测到新枫之谷游戏文件！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            bool isOk = DelRegistryValue(RK_KEY_G_PATH);
            if (!isOk)
            {
                MessageBox.Show("删除注册表失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            isOk = SetRegistryValue(RK_KEY_PATH, path);
            if (!isOk)
            {
                MessageBox.Show("写入注册表失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            MessageBox.Show("删除成功！你现在可以将直接启动游戏！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
