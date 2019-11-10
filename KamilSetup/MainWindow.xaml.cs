using DriverInstallLib;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
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

namespace PandaAudioSetup
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Model _data;
        System.Version _currentAppZipVersion = new Version("0.0.0.0");
        const string Domain = "http://www.zgp.ink:8988";
        public MainWindow()
        {
            InitializeComponent();

                this.Topmost = true;
            this.Loaded += MainWindow_Loaded;
            this.DataContext = _data = new Model();
            if (System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\version.txt"))
            {
                var content = System.IO.File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}data\\version.txt", System.Text.Encoding.UTF8);
                _currentAppZipVersion = new Version(content);
            }

            if (System.Windows.Forms.Application.ExecutablePath.Contains("UnInstall.exe"))
            {
                this.Title = "Monster Audio";
                _data.CurrentStatus = Model.Status.UnInstall;
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Run(()=> {
                Thread.Sleep(1000);
            });
            this.Topmost = false;
        }

      
        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var frm = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (frm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _data.Folder = frm.SelectedPath + "\\ZGPAudio";
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

        private async void btnSetup_Click(object sender, RoutedEventArgs e)
        {
            _data.CurrentStatus = Model.Status.Setuping;

            try
            {
                await Task.Run(() =>
                {
                    var system32Path_32Bit = System.Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);

                    var driverFolderName = "win10";
                    if(new Version( System.Environment.OSVersion.Version.ToString()) < new Version("6.2"))
                    {
                        driverFolderName = "win7";
                    }
#if DEBUG
#else
                _data.SetupingTitle = "正在安装vc_redist.x86...";
                    System.Diagnostics.Process.Start($"{AppDomain.CurrentDomain.BaseDirectory}data\\vc_redist.x86.exe", "/quiet").WaitForExit();


#endif

                    if (_data.IsSetupDriverOnly == false)
                    {
                        _data.SetupingTitle = "正在拷贝文件...";

                        var tryfolder = $"{_data.Folder}\\{Guid.NewGuid().ToString("N")}";
                        System.IO.Directory.CreateDirectory(tryfolder);
                        if (System.IO.Directory.Exists(tryfolder) == false)
                        {
                            throw new Exception($"路径“{_data.Folder}”无法读写，可能是杀毒软件导致的，可以尝试把安装路径更改到其他盘符");
                        }
                        System.IO.Directory.Delete(tryfolder);

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
                    }

                    if (DevConHelper.ListInstalledDrivers(DriverClass.Media).Any(m => m.Name.Contains("Monster Audio")) == false)
                    {
                        _data.SetupingTitle = "正在安装虚拟声卡驱动...";

                        DevConHelper.InstallDriver($"{_data.Folder}\\driver\\{driverFolderName}\\monster.inf", "*MonsterVA");
                    }

                    if (_data.IsSetupDriverOnly == false)
                    {
                        if (_data.CurrentStatus == Model.Status.Setuping)
                        {  
                          

                            _data.SetupingTitle = "正在创建快捷方式...";
                            ShortcutCreator.CreateShortcutOnDesktop("ZGP Audio", $"{_data.Folder}\\Monster.exe", "ZGP Audio", $"{_data.Folder}\\Monster.ico");
                            ShortcutCreator.CreateProgramsShortcut("ZGP Audio", "ZGP Audio", $"{_data.Folder}\\Monster.exe", "ZGP Audio", $"{_data.Folder}\\Monster.ico");
                            ShortcutCreator.CreateProgramsShortcut("ZGP Audio", "Uninstall ZGP Audio", $"{_data.Folder}\\UnInstall.exe", "Uninstall ZGP Audio", $"{_data.Folder}\\UnInstall.exe,0");
                            createUnInstall();
                        }
                    }
                    if (_data.CurrentStatus == Model.Status.Setuping)
                    {
                        _data.CurrentStatus = Model.Status.Finished;
                    }
                });
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                _data.CurrentStatus = Model.Status.Finished;               
                this.Close();
            }
        }

        void createUnInstall()
        {
            _data.SetupingTitle = "正在创建卸载项 UnInstall...";

            if (System.IO.File.Exists($"{_data.Folder}\\UnInstall.exe"))
                System.IO.File.Delete($"{_data.Folder}\\UnInstall.exe");
            System.IO.File.Copy(System.Windows.Forms.Application.ExecutablePath, $"{_data.Folder}\\UnInstall.exe");


            _data.SetupingTitle = "正在创建注册表...";
            var root = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true);
            if (root.GetSubKeyNames().Contains("MonsterAudio") == false)
            {
                root.CreateSubKey("MonsterAudio");
            }
            root.Close();
            root = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MonsterAudio", true);
            root.SetValue("UninstallString", $"{_data.Folder}\\UnInstall.exe");
            root.SetValue("DisplayIcon", $"{_data.Folder}\\Monster.ico");
            root.SetValue("DisplayName", "ZGP Audio");
            root.SetValue("Publisher", $"ZGP");
            root.SetValue("NoModify", 1, Microsoft.Win32.RegistryValueKind.DWord);
            root.SetValue("NoRepair", 1, Microsoft.Win32.RegistryValueKind.DWord);
            root.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
            root.SetValue("InstallLocation", _data.Folder + "\\");
            root.Close();
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
            bool downloadNoAsk = true;
            //if (System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip") == false)
            //{
            //    downloadNoAsk = true;
            //}
            try
            {
                System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();

                httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/76.0.3809.132 Safari/537.36");
                var content = await httpClient.GetStringAsync($"{Domain}/main/version");
                if (_data.CurrentStatus == Model.Status.None && new Version(content) > _currentAppZipVersion)
                {
                    if (downloadNoAsk || MessageBox.Show(this, "官网已经发布新的软件版本，是否现在把安装包更新为新版本?", "", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        try
                        {
                            _data.DownloadingTitle = "正在下载安装包...";
                            _data.CurrentStatus = Model.Status.Downloading;

                            var httpWebRequest = HttpWebRequest.CreateHttp($"{Domain}/main/app");
                            httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/76.0.3809.132 Safari/537.36";
                            
                            var response = await httpWebRequest.GetResponseAsync();

                            var contentLen = (int)response.ContentLength;
                            var total = contentLen;

                            _data.DownloadingProgressTotal = total;
                            _data.DownloadingProgressValue = 0;

                    byte[] data = new byte[4096];
                            var fs = System.IO.File.Create($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip.tmp");
                            var stream = response.GetResponseStream();
                              await Task.Run(() => {
                                while (contentLen > 0)
                                {
                                    var readed = stream.Read(data, 0, Math.Min(data.Length, contentLen));
                                    fs.Write(data, 0, readed);
                                    contentLen -= readed;

                                    _data.DownloadingProgressTotal = total;
                                    _data.DownloadingProgressValue = total - contentLen;

                                    _data.DownloadingTitle = $"正在下载安装包...  {Math.Round(((double)(total - contentLen)) / (1024 * 1024), 2)}M/{Math.Round(((double)total) / (1024 * 1024), 2)}M";
                                }
                            });
                            fs.Dispose();
                            stream.Dispose();

                            if (System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip"))
                                System.IO.File.Delete($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip");

                            System.IO.File.Move($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip.tmp", $"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip");
                            _currentAppZipVersion = new Version(content);
                            System.IO.File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}data\\version.txt", _currentAppZipVersion.ToString(), System.Text.Encoding.UTF8);

                            MessageBox.Show(this, "新版本下载完毕，请继续安装！", "", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
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
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnUnInstall_Click(object sender, RoutedEventArgs e)
        {
            var root = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MonsterAudio", true);//InstallLocation
            var setupFolder = root.GetValue("InstallLocation").ToString();
            if (setupFolder.EndsWith("\\") == false)
                setupFolder += "\\";

            root.Close();

            root = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true);
            root.DeleteSubKey("MonsterAudio");
            root.Close();
           
            try
            {
                ShortcutCreator.DeleteShortcutOnDesktop("ZGP Audio", $"{_data.Folder}\\Monster.exe", "ZGP Audio", $"{_data.Folder}\\Monster.ico");
            }
            catch
            {

            }
            try
            {
                //会同时删除program文件夹
                ShortcutCreator.DeleteProgramsShortcut("ZGP Audio", "ZGP Audio", $"{_data.Folder}\\Monster.exe", "ZGP Audio", $"{_data.Folder}\\Monster.ico");
            }
            catch
            {

            }

            //卸载驱动
            if (true)
            {
                var driver = DevConHelper.ListInstalledDrivers(DriverClass.Media).FirstOrDefault(m => m.Name.Contains("Monster Audio"));
                if (driver != null)
                {
                    var path = driver.Path;
                    bool needReboot;
                    DevConHelper.RemoveDriver(path, out needReboot);
                }
            }

            try
            {
                string[] dirs = System.IO.Directory.GetDirectories(setupFolder);
                foreach (var dir in dirs)
                {
                    if (dir.ToLower().EndsWith("\\effect"))
                        continue;
                    try
                    {
                        System.IO.Directory.Delete(dir, true);
                    }
                    catch { }
                }

                string[] files = System.IO.Directory.GetFiles(setupFolder);
                foreach (var file in files)
                {
                    try
                    {
                        System.IO.File.Delete(file);
                    }
                    catch { }
                }
            }
            catch
            {

            }

            MessageBox.Show(this, "卸载完毕！");
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
            this.Folder = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\ZGPAudio";
            try
            {
                System.IO.Directory.GetFiles("d:\\","*.ppa");
                this.Folder = "D" + this.Folder.Substring(1);
            }
            catch
            {

            }
           
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
