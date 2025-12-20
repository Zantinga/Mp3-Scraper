using HtmlAgilityPack;
using Microsoft.Win32;
using System.IO;
using System.Net.Http;
using System.Windows;


namespace Mp3ScraperWpf
{
    public partial class MainWindow : Window
    {
        private DateTime _startTime;
        private CancellationTokenSource? _cts;
        private bool _isDownloading;
        private long _totalBytesDownloaded;


        public MainWindow()
        {
            InitializeComponent();
            FolderBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads");
        }


        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
                FolderBox.Text = dialog.FolderName;
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isDownloading)
            {
                _cts?.Cancel();
                return;
            }

            await StartDownloadAsync();
        }



        private async Task StartDownloadAsync()
        {
            LogBox.Items.Clear();
            Progress.Value = 0;

            if (!Uri.TryCreate(UrlBox.Text, UriKind.Absolute, out var pageUri))
            {
                MessageBox.Show("Invalid URL");
                return;
            }

            Directory.CreateDirectory(FolderBox.Text);

            _isDownloading = true;
            ActionButton.Content = "Cancel";
            ActionButton.Background = (System.Windows.Media.Brush)
                FindResource("AccentBrush");

            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _totalBytesDownloaded = 0;
            _startTime = DateTime.Now;

            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                StatusText.Text = "Downloading page...";
                var html = await http.GetStringAsync(pageUri);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var mp3Links = doc.DocumentNode
                    .SelectNodes("//*[@src or @href]")
                    ?.Select(n => n.GetAttributeValue("src", null) ?? n.GetAttributeValue("href", null))
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Select(u => new Uri(pageUri, u))
                    .Where(u => u.Host.Contains("ipaudio6.com", StringComparison.OrdinalIgnoreCase)
                             && u.AbsolutePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .ToList();

                if (mp3Links == null || mp3Links.Count == 0)
                {
                    StatusText.Text = "No MP3s found.";
                    return;
                }

                Progress.Maximum = mp3Links.Count;

                for (int i = 0; i < mp3Links.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    var mp3 = mp3Links[i];
                    var fileName = Path.GetFileName(mp3.LocalPath);

                    StatusText.Text = $"Downloading {fileName}";

                    var data = await http.GetByteArrayAsync(mp3, token);
                    await File.WriteAllBytesAsync(
                        Path.Combine(FolderBox.Text, fileName), data);

                    _totalBytesDownloaded += data.Length;

                    LogBox.Items.Add($"Completed {fileName}");

                    Progress.Value = i + 1;
                    UpdateEta(i + 1, mp3Links.Count);
                }


                StatusText.Text = "Done.";
            }
            catch (OperationCanceledException)
            {
                StatusText.Text = "Download cancelled.";
                LogBox.Items.Add("Cancelled by user.");
            }
            finally
            {
                _isDownloading = false;
                ActionButton.Content = "Download";
                _cts?.Dispose();
                _cts = null;
            }
        }



        private void UpdateEta(int completed, int total)
        {
            var elapsed = DateTime.Now - _startTime;
            var avg = elapsed.TotalSeconds / completed;
            var remaining = TimeSpan.FromSeconds(avg * (total - completed));

            string eta = remaining.ToString(@"mm\:ss");
            StatusText.Text = $"{completed}/{total} downloaded — ETA {eta}";
        }
    }
}