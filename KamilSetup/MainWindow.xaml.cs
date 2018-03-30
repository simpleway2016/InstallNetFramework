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
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = _data = new Model();
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
                MessageBox.Show(this, "正在安装，不能关闭窗口！");
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
                            if(entry.IsDirectory == false)
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
                                        System.IO.File.SetCreationTime(filename , entry.CreationTime);
                                        System.IO.File.SetLastWriteTime(filename, entry.LastModified);
                                    }
                                }
                                catch(Exception ex)
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
                        ShortcutCreator.CreateShortcutOnDesktop("熊猫机架", $"{_data.Folder}\\kamil.exe", "Panda Audio", $"{_data.Folder}\\kamil.ico");
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
                MessageBox.Show(this, "写文件发生错误，" + err.Message);
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
            this.Close();
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
            Finished = 2
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
