using GameLauncher.Properties;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Media;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Text;


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
                        PlayButton.Content = "JOUER";
                        PlayButton.Visibility = Visibility.Visible;
                        InstallButton.Visibility = Visibility.Collapsed;
                        break;
                    case LauncherStatus.failed:
                        PlayButton.Content = "ECHEC";
                        InstallButton.Content = "ECHEC";
                        break;
                    case LauncherStatus.downloadingGame:
                        PlayButton.Content = "TELECHARGEMENT";
                        InstallButton.Content = "TELECHARGEMENT";
                        break;
                    case LauncherStatus.downloadingUpdate:
                        PlayButton.Content = "MISE A JOUR";
                        InstallButton.Content = "MISE A JOUR";
                        break;
                    default:
                        break;
                }
            }
        }

        private ProgressBar downloadProgressBar;
        private TextBlock downloadProgressText;
        private HttpClient httpClient;

        private long totalDownloadedBytes = 0L;
        private long totalBytes = 0L;

        public MainWindow()
        {
            InitializeComponent();

            rootPath = Properties.Settings.Default.PathFolder;
            versionFile = System.IO.Path.Combine(rootPath, "Version.txt");
            gameZip = System.IO.Path.Combine(rootPath, "Build.zip");
            gameExe = System.IO.Path.Combine(rootPath, "Build", "Duel de R�gne.exe");

            downloadProgressBar = DownloadProgressBar;
            downloadProgressText = DownloadProgressText;
            httpClient = new HttpClient();

            SoundPlayer soundPlayer = new SoundPlayer("C:\\Users\\wagas\\Desktop\\LGL-Game-Launcher\\GameLauncher\\GameLauncher\\Sons\\Loop.wav");
            soundPlayer.PlayLooping();
        }

        private async Task CheckForUpdates()
        {
            if (File.Exists(versionFile))
            {
                // Vérifie si une version locale existe et lit la version à partir du fichier "Version.txt"
                Version localVersion = new Version(File.ReadAllText(versionFile));
                VersionText.Text = "Version " + localVersion.ToString();

                try
                {
                    // Obtient la version en ligne à partir de dropbox
                    Version onlineVersion = new Version(await httpClient.GetStringAsync("https://www.dropbox.com/s/e6642vrr19p0ht1/Version.txt?dl=1"));

                    if (onlineVersion.IsDifferentThan(localVersion))
                    {
                        // Si une mise à jour est disponible, installe les nouveaux fichiers du jeu
                        await InstallGameFilesAsync(true, onlineVersion);
                    }
                    else
                    {
                        // Sinon, le lanceur est prêt à jouer
                        Status = LauncherStatus.ready;
                    }
                }
                catch (Exception ex)
                {
                    Status = LauncherStatus.failed;
                    MessageBox.Show($"Error checking for game updates: {ex}");
                    totalDownloadedBytes = 0;
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
                    // Si c'est une mise à jour, on change l'état du lanceur en "downloadingUpdate"
                    Status = LauncherStatus.downloadingUpdate;
                }
                else
                {
                    // Si c'est une installation, on change l'état du lanceur en "downloadingGame" et on récupére la version en ligne
                    Status = LauncherStatus.downloadingGame;
                    onlineVersion = new Version(await httpClient.GetStringAsync("https://www.dropbox.com/s/e6642vrr19p0ht1/Version.txt?dl=1"));
                }

                var progress = new Progress<int>(ReportProgress);
                var downloadTask = DownloadFileAsync(new Uri("https://www.dropbox.com/s/043eoko3pir262m/Build.zip?dl=1"), gameZip, onlineVersion, progress);

                // Afficher la barre de progression
                downloadProgressBar.Visibility = Visibility.Visible;
                downloadProgressText.Visibility = Visibility.Visible;

                await downloadTask;

                // Rendre la barre de téléchargement invisible une fois terminé
                downloadProgressBar.Visibility = Visibility.Collapsed;
                downloadProgressText.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Status = LauncherStatus.failed;
                MessageBox.Show($"Error installing game files: {ex}");
                totalDownloadedBytes = 0;

            }
        }

        private async Task DownloadFileAsync(Uri uri, string filePath, Version onlineVersion, IProgress<int> progress)
        {
            // Envoie une requête HTTP GET pour récupérer le fichier à partir de l'URL spécifié
            using (var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode(); // Vérifie que la réponse est réussie (code de statut HTTP 2xx)

                totalBytes = response.Content.Headers.ContentLength ?? -1L; // Obtient la taille totale du fichier à télécharger (si disponible)
                var downloadedBytes = 0L; // Initialise le nombre de bytes téléchargés

                // Ouverture d'un flux pour lire le contenu de la réponse
                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192]; // Tampon de lecture pour stocker les données téléchargées
                    int bytesRead;

                    // Lecture et écriture par morceaux jusqu'à ce que tout le fichier soit téléchargé
                    while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        downloadedBytes += bytesRead;
                        totalDownloadedBytes += bytesRead; // Met à jour le nombre total de bytes téléchargés

                        var progressPercentage = (int)(downloadedBytes * 100 / totalBytes);
                        progress?.Report(progressPercentage);
                    }
                }

                // Extraction de l'archive ZIP (si le fichier téléchargé est un fichier ZIP)
                ZipFile.ExtractToDirectory(filePath, rootPath, true);
                File.Delete(filePath); // Supprime le fichier ZIP après extraction

                File.WriteAllText(versionFile, onlineVersion.ToString()); // Écrit la version en ligne dans le fichier de version local

                VersionText.Text = onlineVersion.ToString(); // Met à jour le texte de la version affiché dans l'interface utilisateur
                Status = LauncherStatus.ready;
            }
        }

        private void ReportProgress(int progressPercentage)
        {

            downloadProgressBar.Value = progressPercentage;
            downloadProgressText.Text = $"{progressPercentage}% ({totalDownloadedBytes / 1048576} Mo / {totalBytes / 1048576}Mo)"; // Affiche la quantité de Mo téléchargée
        }

        private async void Window_ContentRendered(object sender, EventArgs e)
        {
            // Vérifie si le fichier du jeu existe
            if (File.Exists(gameExe))
            {
                PlayButton.Visibility = Visibility.Visible;
                InstallButton.Visibility = Visibility.Collapsed;

                // On vérifie que le jeu est à jour
                await CheckForUpdates();
            }
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {

            if (File.Exists(gameExe) && Status == LauncherStatus.ready)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(gameExe);
                startInfo.WorkingDirectory = System.IO.Path.Combine(rootPath, "Build");
                Process.Start(startInfo);

                Close();
            }
            else if (Status == LauncherStatus.failed)
            {
                await CheckForUpdates();
            }
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog();
            dialog.IsFolderPicker = true;

            if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                // Obtient le dossier sélectionné par l'utilisateur
                string installationFolder = dialog.FileName;

                // Définit le dossier d'installation du jeu sur le dossier sélectionné par l'utilisateur
                Properties.Settings.Default.PathFolder = installationFolder;

                // Stocke la valeur dans les paramètres de l'application
                Properties.Settings.Default.Save();

                versionFile = System.IO.Path.Combine(installationFolder, "Version.txt");
                gameZip = System.IO.Path.Combine(installationFolder, "Build.zip");
                gameExe = System.IO.Path.Combine(installationFolder, "Build", "Duel de R�gne.exe");

                // Appele la méthode CheckForUpdates pour lancer le téléchargement
                await CheckForUpdates();
            }
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {

            // Ouvre le lien dans le navigateur par défaut
            Process.Start(new ProcessStartInfo("https://discord.com/") { UseShellExecute = true });
        }

        private void WebsiteButton_Click(object sender, RoutedEventArgs e)
        {

            // Ouvre le lien dans le navigateur par défaut
            Process.Start(new ProcessStartInfo("https://card-game-website.vercel.app/") { UseShellExecute = true });
        }

        private void LogOutButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Si on maintiens le clique gauche sur la fenêtre
            if (e.ChangedButton == MouseButton.Left)
            {
                // Alors on peut la déplacer
                this.DragMove();
            }
        }

        bool isMaximized = false;

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (isMaximized)
                {
                    this.WindowState = WindowState.Normal;
                    this.Width = 1080;
                    this.Height = 680;

                    isMaximized = false;
                } else {
                    this.WindowState = WindowState.Maximized;

                    isMaximized = true;
                }
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
            // Constructeur prenant une chaîne de version au format "major.minor.subMinor"
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
            // Vérifie si cette version est différente de la version spécifiée
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