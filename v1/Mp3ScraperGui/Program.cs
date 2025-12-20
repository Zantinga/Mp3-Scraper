using HtmlAgilityPack;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mp3ScraperGui
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private readonly TextBox urlTextBox = new();
        private readonly Button downloadButton = new();
        private readonly ProgressBar progressBar = new();
        private readonly ListBox logListBox = new();

        public MainForm()
        {
            Text = "MP3 Scraper";
            Width = 600;
            Height = 400;

            urlTextBox.SetBounds(10, 10, 460, 25);
            downloadButton.SetBounds(480, 10, 90, 25);
            progressBar.SetBounds(10, 45, 560, 20);
            logListBox.SetBounds(10, 75, 560, 275);

            urlTextBox.PlaceholderText = "Enter URL";
            downloadButton.Text = "Download";

            downloadButton.Click += async (_, _) => await DownloadAsync();

            Controls.AddRange(new Control[]
            {
                urlTextBox,
                downloadButton,
                progressBar,
                logListBox
            });
        }

        private async Task DownloadAsync()
        {
            logListBox.Items.Clear();
            progressBar.Value = 0;

            if (!Uri.TryCreate(urlTextBox.Text, UriKind.Absolute, out var pageUri))
            {
                MessageBox.Show("Invalid URL");
                return;
            }

            var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "downloads");
            Directory.CreateDirectory(outputDir);

            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            logListBox.Items.Add("Downloading page...");
            var html = await http.GetStringAsync(pageUri);

            var doc = new HtmlAgilityPack.HtmlDocument();
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
                logListBox.Items.Add("No MP3 files found.");
                return;
            }

            progressBar.Maximum = mp3Links.Count;

            foreach (var mp3 in mp3Links)
            {
                try
                {
                    var fileName = Path.GetFileName(mp3.LocalPath);
                    logListBox.Items.Add($"Downloading {fileName}");

                    var data = await http.GetByteArrayAsync(mp3);
                    await File.WriteAllBytesAsync(Path.Combine(outputDir, fileName), data);

                    progressBar.Value++;
                }
                catch (Exception ex)
                {
                    logListBox.Items.Add($"Failed: {ex.Message}");
                }
            }

            logListBox.Items.Add("Done.");
        }
    }
}
