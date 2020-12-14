using DriverInstallLib;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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

namespace PandaAudioSetup
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Model _data;
        Version _currentAppZipVersion = new Version("0.0.0.0");
        string ServerUrl = "http://jacktan.cn:8900";
        public MainWindow()
        {
            InitializeComponent();
           
          if(  IntPtr.Size == 8)
            {
                ServerUrl = "http://jacktan.cn:8988";
            }
           
            this.Topmost = true;
            this.Loaded += MainWindow_Loaded;
            this.DataContext = _data = new Model();
            if (System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.txt") && System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip"))
            {
                var content = System.IO.File.ReadAllText($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.txt", System.Text.Encoding.UTF8);
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
                    _data.Folder = frm.SelectedPath + "\\MonsterAudio";
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
                    Patch.Fix();

                    var systemPath_64Bit = System.Environment.GetFolderPath(Environment.SpecialFolder.System);

                    var driverFolderName = "win10";
                    if(System.Environment.OSVersion.Version < new Version("6.2"))
                    {
                        driverFolderName = "win7";
                    }
#if DEBUG
#else
                _data.SetupingTitle = "正在安装vc_redist.x64...";
                    System.Diagnostics.Process.Start($"{AppDomain.CurrentDomain.BaseDirectory}data\\vc_redist.x64.exe", "/quiet").WaitForExit();
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

                        //var stream = Application.GetResourceStream(new Uri("pack://application:,,,/images/logo.ico"));


                        //    byte[] bs = new byte[stream.Stream.Length];
                        //stream.Stream.Position = 0;
                        //stream.Stream.Read(bs, 0, bs.Length);

                        //System.IO.File.WriteAllBytes($"{_data.Folder}\\Monster.ico", bs);

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

                   

                    if (_data.IsSetupDriverOnly == false)
                    {
                        if (_data.CurrentStatus == Model.Status.Setuping)
                        {  
                          

                            _data.SetupingTitle = "正在创建快捷方式...";
                            ShortcutCreator.CreateShortcutOnDesktop("Monster Audio", $"{_data.Folder}\\Monster.exe", "Monster Audio", $"{_data.Folder}\\Monster.ico");
                            ShortcutCreator.CreateProgramsShortcut("Monster Audio", "Monster Audio", $"{_data.Folder}\\Monster.exe", "Monster Audio", $"{_data.Folder}\\Monster.ico");
                            ShortcutCreator.CreateProgramsShortcut("Monster Audio", "Uninstall Monster Audio", $"{_data.Folder}\\UnInstall.exe", "Uninstall Monster Audio", $"{_data.Folder}\\UnInstall.exe,0");
                            createUnInstall();
                        }
                    }

                    if (DevConHelper.ListInstalledDrivers(DriverClass.Media).Any(m => m.Name.Contains("Monster Audio")) == false)
                    {
                        _data.SetupingTitle = "即将安装虚拟声卡驱动...";

                        this.Dispatcher.Invoke(()=> {
                            MessageBox.Show(this, "即将安装虚拟声卡驱动，由于部分用户反应此步骤会重启windows，所以这里特意提醒，请先保存手头上的工作。\r\n如果系统重启，表示驱动安装失败，需要重新执行一遍安装程序。\r\n\r\n点击【确定】按钮继续安装");
                        });

                        _data.SetupingTitle = "正在安装虚拟声卡驱动...";
                        DevConHelper.InstallDriver($"{_data.Folder}\\driver\\{driverFolderName}\\monster.inf", "*MonsterVA");                        
                    }

                    //如果是win7
                    if (System.Environment.OSVersion.Version < new Version("6.2"))
                    {
                        try
                        {
                            var renderkey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render", false);
                            var folders = renderkey.GetSubKeyNames();
                            renderkey.Dispose();

                            foreach (var foldername in folders)
                            {
                                using (renderkey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\{foldername}\Properties", false))
                                {
                                    var val = renderkey.GetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2");
                                    if (val != null && val.ToString().Contains("Monster Mic"))
                                    {
                                        //renderkey.SetValue("{a45c254e-df1c-4efd-8020-67d146a850e0},2", "Monster Play");
                                        var content = $@"Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render\{foldername}\Properties]
""{{a45c254e-df1c-4efd-8020-67d146a850e0}},2""=""Monster Play""
";
                                        System.IO.File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}reg.reg", content, System.Text.Encoding.Unicode);
                                        Process.Start("regedit", string.Format(" /s \"{0}\"", $"{AppDomain.CurrentDomain.BaseDirectory}reg.reg")).WaitForExit();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("发生错误，此错误可忽略，已经成功安装！\r\n" + ex.Message);
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
            root.SetValue("DisplayName", "Monster Audio");
            root.SetValue("Publisher", $"Monster");
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
            if(_data.CurrentStatus != Model.Status.UnInstall)
                CheckVersion();
        }
        static string VersionFileUrl;
        static string AppFileUrl;
        static string VersionHistoryUrl;
        async void CheckVersion()
        {
            _data.CanInstall = false;

            bool downloadNoAsk = false;
            if (System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip") == false)
            {
                downloadNoAsk = true;
            }
            try
            {
                System.Net.WebClient client = new System.Net.WebClient();
                client.Headers.Add("Cache-Control", "no-cache");
                client.Headers.Add("Pragma", "no-cache");
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/76.0.3809.132 Safari/537.36");
                client.Encoding = System.Text.Encoding.UTF8;

                var urlsContent = await client.DownloadStringTaskAsync(new Uri($"{ServerUrl}/urls.json"));
                var dict = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(urlsContent);
                if (dict.ContainsKey("VersionFileUrl"))
                    VersionFileUrl = dict["VersionFileUrl"];

                if (dict.ContainsKey("AppFileUrl"))
                    AppFileUrl = dict["AppFileUrl"];

                if (dict.ContainsKey("VersionHistoryUrl"))
                    VersionHistoryUrl = dict["VersionHistoryUrl"];

                var versionFileContent = await client.DownloadStringTaskAsync(new Uri(VersionFileUrl));
                var serverVersion = new Version(Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(versionFileContent)["version"]);
                if (_data.CurrentStatus == Model.Status.None && serverVersion > _currentAppZipVersion)
                {
                    if (downloadNoAsk)
                    {
                        try
                        {
                            _data.DownloadingTitle = "正在下载安装包...";
                            _data.CurrentStatus = Model.Status.Downloading;

                            client = new System.Net.WebClient();
                            client.Headers.Add("Cache-Control", "no-cache");
                            client.Headers.Add("Pragma", "no-cache");
                            client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/76.0.3809.132 Safari/537.36");
                            client.Encoding = System.Text.Encoding.UTF8;

                            client.DownloadProgressChanged += (s, e) =>
                            {
                                _data.DownloadingProgressTotal = e.TotalBytesToReceive;
                                _data.DownloadingProgressValue = e.BytesReceived;

                                _data.DownloadingTitle = $"正在下载安装包...  {Math.Round(((double)e.BytesReceived) / (1024 * 1024), 2)}M/{Math.Round(((double)e.TotalBytesToReceive) / (1024 * 1024), 2)}M";
                            };
                            await client.DownloadFileTaskAsync(AppFileUrl, $"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip.tmp");

                            if (System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip"))
                                System.IO.File.Delete($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip");

                            System.IO.File.Move($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip.tmp", $"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip");
                            _currentAppZipVersion = serverVersion;
                            System.IO.File.WriteAllText($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.txt", _currentAppZipVersion.ToString(), System.Text.Encoding.UTF8);


                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
                            Process.GetCurrentProcess().Kill();
                        }
                        finally
                        {
                            _data.CurrentStatus = Model.Status.None;

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
               
            }
            finally
            {
                if (System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.txt") && System.IO.File.Exists($"{AppDomain.CurrentDomain.BaseDirectory}data\\app.zip"))
                {
                    _data.CanInstall = true;
                }
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
                ShortcutCreator.DeleteShortcutOnDesktop("Monster Audio", $"{_data.Folder}\\Monster.exe", "Monster Audio", $"{_data.Folder}\\Monster.ico");
            }
            catch
            {

            }
            try
            {
                //会同时删除program文件夹
                ShortcutCreator.DeleteProgramsShortcut("Monster Audio", "Monster Audio", $"{_data.Folder}\\Monster.exe", "Monster Audio", $"{_data.Folder}\\Monster.ico");
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

        public string ProductName
        {
            get
            {
                if(IntPtr.Size == 8)
                return "Monster Audio Installer（64bit）";
                else
                    return "Monster Audio Installer（32bit）";
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


        private bool _CanInstall;
        public bool CanInstall
        {
            get => _CanInstall;
            set
            {
                if (_CanInstall != value)
                {
                    _CanInstall = value;
                    this.OnChange("CanInstall");
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
            if (IntPtr.Size == 8)
            {
                this.Folder = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + "\\MonsterAudio";
            }
            else
            {
                this.Folder = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "\\MonsterAudio";
            }

            try
            {
                var filename = "d:\\" + Guid.NewGuid();
                System.IO.File.Create(filename).Close();
                System.IO.File.Delete(filename);
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
