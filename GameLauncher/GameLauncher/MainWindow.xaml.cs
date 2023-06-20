using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GameLauncher
{
    enum LauncherStatus
    {
        ready,
        failed,
        downloadingGame,
        downloadingUpdate
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string rootPath;
        private string versionFile;
        private string gameZip;
        private string gameExe;

        private LauncherStatus _status;

        internal LauncherStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                switch (_status)
                {
                    case LauncherStatus.ready:
                        PlayButton.Content = "Play";
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "Update Failed - Retry";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "Downloading Game";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "Downloading Update";
                        break;
                    default:
                        break;
                }
            }
        }

        private ProgressBar downloadProgressBar;
        private HttpClient httpClient;

        public MainWindow()
        {
            InitializeComponent();

            rootPath = Directory.GetCurrentDirectory();
            versionFile = Path.Combine(rootPath, "Version.txt");
            gameZip = Path.Combine(rootPath, "Build.zip");
            gameExe = Path.Combine(rootPath, "Build", "Card game.exe");

            downloadProgressBar = DownloadProgressBar;
            httpClient = new HttpClient();
        }

        private async Task CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = localVersion.ToString();

                try
                {
                    Version onlineVersion = new Version(await httpClient.GetStringAsync("https://www.dropbox.com/s/e6642vrr19p0ht1/Version.txt?dl=1"));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        await InstallGameFilesAsync(true, onlineVersion);
                    }
                    else
                    {
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                }
            }
            else
            {
                await InstallGameFilesAsync(false, Version.zero);
            }
        }

        private async Task InstallGameFilesAsync(bool isUpdate, Version onlineVersion)
        {
            try
            {
                if (isUpdate)
                {
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    Status = LauncherStatus.downloadingGame;
                    onlineVersion = new Version(await httpClient.GetStringAsync("https://www.dropbox.com/s/e6642vrr19p0ht1/Version.txt?dl=1"));
                }

                var progress = new Progress<int>(ReportProgress);
                var downloadTask = DownloadFileAsync(new Uri("https://www.dropbox.com/s/043eoko3pir262m/Build.zip?dl=1"), gameZip, onlineVersion, progress);

                // Afficher la barre de progression
                downloadProgressBar.Visibility = Visibility.Visible;

                await downloadTask;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
            }
        }

        private async Task DownloadFileAsync(Uri uri, string filePath, Version onlineVersion, IProgress<int> progress)
        {
            using (var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                var downloadedBytes = 0L;

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;

                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;

                        // Calculer le pourcentage de progression
                        var progressPercentage = (int)(downloadedBytes * 100 / totalBytes);

                        // Signaler la progression
                        progress?.Report(progressPercentage);
                    }
                }

                // Extraire l'archive zip
                ZipFile.ExtractToDirectory(filePath, rootPath, true);
                File.Delete(filePath);

                File.WriteAllText(versionFile, onlineVersion.ToString());

                VersionText.Text = onlineVersion.ToString();
                Status = LauncherStatus.ready;
            }
        }

        private void ReportProgress(int progressPercentage)
        {
            // Mettre à jour la valeur de la barre de progression en fonction du pourcentage de progression
            downloadProgressBar.Value = progressPercentage;
        }

        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            await CheckForUpdates();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = Path.Combine(rootPath, "Build");
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                CheckForUpdates();
            }
        }
    }

    struct Version
    {
        internal static Version zero = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor)
        {
            major = _major;
            minor = _minor;
            subMinor = _subMinor;
        }
        internal Version(string _version)
        {
            string[] versionStrings = _version.Split('.');
            if (versionStrings.Length != 3)
            {
                major = 0;
                minor = 0;
                subMinor = 0;
                return;
            }

            major = short.Parse(versionStrings[0]);
            minor = short.Parse(versionStrings[1]);
            subMinor = short.Parse(versionStrings[2]);
        }

        internal bool IsDifferentThan(Version _otherVersion)
        {
            if (major != _otherVersion.major)
            {
                return true;
            }
            else
            {
                if (minor != _otherVersion.minor)
                {
                    return true;
                }
                else
                {
                    if (subMinor != _otherVersion.subMinor)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
    }
}
