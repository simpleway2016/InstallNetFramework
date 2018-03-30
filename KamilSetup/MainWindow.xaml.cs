using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace PandaAudioSetup
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Model _data;
        double _currentAppZipVersion = 0;
        const string Domain = "http://www.pandaaudio.cn:8988";
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = _data = new Model();
            if (System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.dat"))
            {
                var content = System.IO.File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.dat", System.Text.Encoding.UTF8);
                _currentAppZipVersion = string2Double(content);
            }
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

        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var frm = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _data.Folder = frm.SelectedPath + "\\PandaAudio";
                }
            }
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

        private void btnSetup_Click(object sender, RoutedEventArgs e)
        {
            _data.CurrentStatus = Model.Status.Setuping;

            Task.Run(() =>
            {
#if DEBUG
#else
                _data.SetupingTitle = "正在安装vc_redist.x86...";
                System.Diagnostics.Process.Start($"{AppDomain.CurrentDomain.BaseDirectory}data\\vc_redist.x86.exe", "/quiet").WaitForExit();
                _data.SetupingTitle = "正在安装虚拟声卡驱动...";
                System.Diagnostics.Process.Start($"{AppDomain.CurrentDomain.BaseDirectory}data\\DriverInstaller.exe", $"{AppDomain.CurrentDomain.BaseDirectory}data\\kamilva.inf *KamilMC").WaitForExit();
#endif
                if (_data.IsSetupDriverOnly == false)
                {
                    _data.SetupingTitle = "正在拷贝文件...";
                    //拷贝文件
                    using (ZipFile zip = new ZipFile($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip"))
                    {
                        _data.ProgressTotal = zip.Entries.Count;
                        _data.ProgressValue = 0;
                        foreach (var entry in zip.Entries)
                        {
                            if (entry.IsDirectory == false)
                            {
                                try
                                {
                                    var folder = System.IO.Path.GetDirectoryName($"{_data.Folder}\\{entry.FileName}");
                                    if (System.IO.Directory.Exists(folder) == false)
                                        System.IO.Directory.CreateDirectory(folder);
                                    using (var reader = entry.OpenReader())
                                    {
                                        byte[] content = new byte[reader.Length];
                                        reader.Read(content, 0, content.Length);

                                        var filename = $"{_data.Folder}\\{entry.FileName}";
                                        if (System.IO.File.Exists(filename))
                                            System.IO.File.Delete(filename);
                                        System.IO.File.WriteAllBytes(filename, content);
                                        System.IO.File.SetCreationTime(filename, entry.CreationTime);
                                        System.IO.File.SetLastWriteTime(filename, entry.LastModified);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Zip_ZipError(ex);
                                    break;
                                }
                            }

                            _data.ProgressValue++;
                        }
                        _data.ProgressValue = _data.ProgressTotal;
                    }

                    if (_data.CurrentStatus == Model.Status.Setuping)
                    {
                        _data.SetupingTitle = "正在创建快捷方式...";
                        ShortcutCreator.CreateShortcutOnDesktop("Panda Audio", $"{_data.Folder}\\kamil.exe", "熊猫机架", $"{_data.Folder}\\kamil.ico");
                        ShortcutCreator.CreateProgramsShortcut("熊猫机架" , "Panda Audio", $"{_data.Folder}\\kamil.exe", "熊猫机架", $"{_data.Folder}\\kamil.ico");
                    }
                }
                if (_data.CurrentStatus == Model.Status.Setuping)
                {
                    _data.CurrentStatus = Model.Status.Finished;
                }
            });
        }

        private void Zip_ZipError(Exception err)
        {
            this.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(this, err.Message, "写入文件时发生错误", MessageBoxButton.OK, MessageBoxImage.Error);
                _data.CurrentStatus = Model.Status.None;
            });
        }

        private void Zip_ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            _data.ProgressTotal = e.EntriesTotal;
            _data.ProgressValue = e.EntriesExtracted;
        }

        private void btnFinish_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CheckVersion();
        }

        async void CheckVersion()
        {
            try
            {
                System.Net.WebClient client = new System.Net.WebClient();
                client.Encoding = System.Text.Encoding.UTF8;
                var content = await client.DownloadStringTaskAsync(new Uri($"{Domain}/app.dat"));
                if ( _data.CurrentStatus == Model.Status.None && string2Double(content) > _currentAppZipVersion)
                {
                    if (MessageBox.Show(this, "官网已经发布新的软件版本，是否现在把安装包更新为新版本?", "", MessageBoxButton.YesNo , MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {                       
                        try
                        {
                            _data.DownloadingTitle = "正在下载安装包...";
                            _data.CurrentStatus = Model.Status.Downloading;
                            client.DownloadProgressChanged += (s, e) =>
                            {
                                _data.DownloadingProgressTotal = e.TotalBytesToReceive;
                                _data.DownloadingProgressValue = e.BytesReceived;

                                _data.DownloadingTitle = $"正在下载安装包...  {Math.Round(((double)e.BytesReceived) / (1024 * 1024), 2)}M/{Math.Round( ((double)e.TotalBytesToReceive) / (1024 * 1024) , 2)}M";
                            };
                            await client.DownloadFileTaskAsync($"{Domain}/app.zip", $"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip.tmp");

                            if(System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip"))
                                System.IO.File.Delete($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip");

                            System.IO.File.Move($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip.tmp", $"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip");
                            _currentAppZipVersion = string2Double(content);
                            System.IO.File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.dat",_currentAppZipVersion.ToString(), System.Text.Encoding.UTF8);

                            MessageBox.Show(this, "新版本下载完毕，请继续安装！", "", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show(this, ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        finally
                        {
                            _data.CurrentStatus = Model.Status.None;

                        }
                    }
                }
            }
            catch
            {
                
            }
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
            Downloading = 3
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
