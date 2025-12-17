using HtmlAgilityPack;
using System.Net;

if (args.Length == 0)
{
    Console.WriteLine("Usage: Mp3Scraper <url>");
    return;
}

var pageUrl = args[0];
var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "downloads");
Directory.CreateDirectory(outputDir);

using var http = new HttpClient();
http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

Console.WriteLine($"Downloading page: {pageUrl}");
var html = await http.GetStringAsync(pageUrl);

var doc = new HtmlDocument();
doc.LoadHtml(html);

var mp3Links = doc.DocumentNode
    .SelectNodes("//*[@src or @href]")
    ?.Select(n => n.GetAttributeValue("src", null) ?? n.GetAttributeValue("href", null))
    .Where(u => !string.IsNullOrWhiteSpace(u))
    .Select(u => new Uri(new Uri(pageUrl), u))
    .Where(u =>
        u.Host.Contains("ipaudio6.com", StringComparison.OrdinalIgnoreCase) &&
        u.AbsolutePath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
    .Distinct()
    .ToList();

if (mp3Links == null || mp3Links.Count == 0)
{
    Console.WriteLine("No matching MP3 files found.");
    return;
}

Console.WriteLine($"Found {mp3Links.Count} MP3 files.");

foreach (var mp3 in mp3Links)
{
    try
    {
        var fileName = Path.GetFileName(mp3.LocalPath);
        var filePath = Path.Combine(outputDir, fileName);

        Console.WriteLine($"Downloading {fileName}");
        var data = await http.GetByteArrayAsync(mp3);
        await File.WriteAllBytesAsync(filePath, data);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to download {mp3}: {ex.Message}");
    }
}

Console.WriteLine("Done.");
