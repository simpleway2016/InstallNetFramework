using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AsioDriverSetup
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Model _data;
        public MainWindow()
        {
            InitializeComponent();

            this.Topmost = true;
            this.Loaded += MainWindow_Loaded;
            this.DataContext = _data = new Model();

           
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(() => {
                Thread.Sleep(1000);
            });
            this.Topmost = false;
        }

        double string2Double(string content)
        {
            if (string.IsNullOrEmpty(content))
                return 0;

            var index = content.IndexOf(".");
            if (index < 0)
                return Convert.ToDouble(content);
            content = content.Replace(".", "");
            return Convert.ToDouble(content.Insert(index, "."));
        }


        protected override void OnClosing(CancelEventArgs e)
        {
            if (_data.CurrentStatus == Model.Status.Setuping)
            {
                MessageBox.Show(this, "正在安装，无法关闭窗口！");
                e.Cancel = true;
                return;

            }
            else if (_data.CurrentStatus == Model.Status.Downloading)
            {
                MessageBox.Show(this, "正在下载新版本，无法关闭窗口！");
                e.Cancel = true;
                return;
            }
            base.OnClosing(e);
        }

        private async void btnSetup_Click(object sender, RoutedEventArgs e)
        {
            _data.CurrentStatus = Model.Status.Setuping;

            try
            {
                await Task.Run(() =>
                {
                   

                    var driverFolderName = "win10";
                    if (string2Double(System.Environment.OSVersion.Version.ToString()) < 6.2)
                    {
                        driverFolderName = "win7";
                    }


                    //USB\VID_04B4&PID_1004&REV_0000
                    _data.SetupingTitle = "正在安装usb驱动...";
                    System.Diagnostics.Process.Start($"{AppDomain.CurrentDomain.BaseDirectory}data\\DriverInstaller.exe", $"\"{AppDomain.CurrentDomain.BaseDirectory}data\\{driverFolderName}\\cyusb3.inf\" \"USB\\VID_04B4&PID_1004\"").WaitForExit();

                    if (System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\cy22asio.dll"))
                    {
                        _data.SetupingTitle = "正在注册32位ASIO...";
                        var system32Path_32Bit = System.Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
                        System.IO.File.Copy($"{AppDomain.CurrentDomain.BaseDirectory}data\\cy22asio.dll", $"{system32Path_32Bit}\\cy22asio.dll", true);
                        System.Diagnostics.Process.Start($"{system32Path_32Bit}\\regsvr32.exe", $"/s \"{system32Path_32Bit}\\cy22asio.dll\"");
                    }
                    if (System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\cy22asio64.dll"))
                    {
                        _data.SetupingTitle = "正在注册64位ASIO...";
                        var systemPath = System.Environment.GetFolderPath(Environment.SpecialFolder.System);
                        System.IO.File.Copy($"{AppDomain.CurrentDomain.BaseDirectory}data\\cy22asio64.dll", $"{systemPath}\\cy22asio64.dll", true);
                        System.Diagnostics.Process.Start($"{systemPath}\\regsvr32.exe", $"/s \"{systemPath}\\cy22asio64.dll\"");
                    }
                   

                    if (_data.CurrentStatus == Model.Status.Setuping)
                    {
                        _data.CurrentStatus = Model.Status.Finished;
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                _data.CurrentStatus = Model.Status.Finished;
                this.Close();
            }
        }


        private void btnFinish_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

       
       
    }

    class Model : INotifyPropertyChanged
    {
        public class PannelVisibility
        {
            Model _data;
            public PannelVisibility(Model m)
            {
                _data = m;
            }
            public Visibility this[string name]
            {
                get
                {
                    if (_data.CurrentStatus.ToString() == name)
                        return System.Windows.Visibility.Visible;
                    else
                        return System.Windows.Visibility.Hidden;
                }
            }
        }
        public enum Status
        {
            None = 0,
            Setuping = 1,
            Finished = 2,
            Downloading = 3,
            UnInstall = 4
        }

        string _Folder;
        public string Folder
        {
            get => _Folder;
            set
            {
                if (value.Contains("\\\\"))
                {
                    value = value.Replace("\\\\", "\\");
                }
                if (value != _Folder)
                {
                    _Folder = value;
                    this.OnChange("Folder");
                }
            }
        }
        string _SetupingTitle;
        public string SetupingTitle
        {
            get => _SetupingTitle;
            set
            {
                if (value != _SetupingTitle)
                {
                    _SetupingTitle = value;
                    this.OnChange("SetupingTitle");
                }
            }
        }
        string _DownloadingTitle;
        public string DownloadingTitle
        {
            get => _DownloadingTitle;
            set
            {
                if (value != _DownloadingTitle)
                {
                    _DownloadingTitle = value;
                    this.OnChange("DownloadingTitle");
                }
            }
        }
        Status _CurrentStatus = Status.None;
        public Status CurrentStatus
        {
            get => _CurrentStatus;
            set
            {
                if (value != _CurrentStatus)
                {
                    _CurrentStatus = value;
                    this.OnChange("Visibility");
                    this.OnChange("CurrentStatus");
                }
            }
        }

        bool _IsSetupDriverOnly = false;
        public bool IsSetupDriverOnly
        {
            get => _IsSetupDriverOnly;
            set
            {
                if (value != _IsSetupDriverOnly)
                {
                    _IsSetupDriverOnly = value;
                    this.OnChange("IsSetupDriverOnly");
                }
            }
        }

        int _ProgressTotal = 0;
        public int ProgressTotal
        {
            get => _ProgressTotal;
            set
            {
                if (value < _ProgressValue)
                {
                    return;
                }
                if (value != _ProgressTotal)
                {
                    _ProgressTotal = value;
                    this.OnChange("ProgressTotal");
                }

            }
        }
        int _ProgressValue = 0;
        public int ProgressValue
        {
            get => _ProgressValue;
            set
            {
                if (value > _ProgressTotal)
                {
                    value = _ProgressTotal;
                }
                if (value != _ProgressValue)
                {
                    _ProgressValue = value;
                    this.OnChange("ProgressValue");
                }

            }
        }
        long _DownloadingProgressTotal = 0;
        public long DownloadingProgressTotal
        {
            get => _DownloadingProgressTotal;
            set
            {
                if (value < _DownloadingProgressValue)
                {
                    return;
                }
                if (value != _DownloadingProgressTotal)
                {
                    _DownloadingProgressTotal = value;
                    this.OnChange("DownloadingProgressTotal");
                }

            }
        }
        long _DownloadingProgressValue = 0;
        public long DownloadingProgressValue
        {
            get => _DownloadingProgressValue;
            set
            {
                if (value > _DownloadingProgressTotal)
                {
                    value = _DownloadingProgressTotal;
                }
                if (value != _DownloadingProgressValue)
                {
                    _DownloadingProgressValue = value;
                    this.OnChange("DownloadingProgressValue");
                }

            }
        }
        public PannelVisibility Visibility
        {
            get;
            private set;
        }

        public Model()
        {
            SetupingTitle = "正在安装...";
            this.Folder = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\PandaAudio";
            Visibility = new PannelVisibility(this);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnChange(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
