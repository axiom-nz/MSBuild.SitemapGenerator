using System.Xml.Linq;
using Microsoft.Build.Framework;

namespace MSBuild.SitemapGenerator;

public class GenerateSitemap : Microsoft.Build.Utilities.Task
{
    [Required]
    public string PublishDir { get; set; } = null!;

    [Required]
    public string BaseUrl { get; set; } = null!;

    public ITaskItem[]? Rules { get; set; }

    public override bool Execute()
    {
        try
        {
            PublishDir = PublishDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (!Directory.Exists(PublishDir))
            {
                Log.LogError($"Publish directory does not exist: {PublishDir}");
                return false;
            }

            Log.LogMessage(MessageImportance.High, $"Generating sitemap V3 at {DateTime.UtcNow:u} for: {PublishDir}");
            Log.LogMessage(MessageImportance.High, $"BaseUrl: {BaseUrl}");
            Log.LogMessage(MessageImportance.High, $"Rules count: {Rules?.Length ?? 0}");

            string[] htmlFiles = Directory.GetFiles(PublishDir, "*.html", SearchOption.AllDirectories);
            List<string> urls = [];
            Log.LogMessage(MessageImportance.High, $"Found {htmlFiles.Length} HTML files.");

            List<SitemapRuleRule> rules = Rules?.Select(r => new SitemapRuleRule
            {
                Prefix = r.ItemSpec.ToLowerInvariant(),
                Priority = r.GetMetadata("Priority"),
                ChangeFreq = r.GetMetadata("ChangeFreq")
            }).OrderByDescending(r => r.Prefix.Length).ToList() ?? [];

            foreach (string file in htmlFiles)
            {
                string relativePath = file.Substring(PublishDir.Length).TrimStart(Path.DirectorySeparatorChar);
                string urlPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');

                if (urlPath.EndsWith("index.html", StringComparison.OrdinalIgnoreCase))
                    urlPath = urlPath.Substring(0, urlPath.Length - "index.html".Length);
                else if (urlPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    urlPath = urlPath.Substring(0, urlPath.Length - ".html".Length);

                if (!urlPath.StartsWith("/"))
                    urlPath = "/" + urlPath;

                string fullUrl = $"{BaseUrl.TrimEnd('/')}{urlPath}";
                if (!urls.Contains(fullUrl))
                    urls.Add(fullUrl);
            }

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            XDocument sitemap = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(ns + "urlset",
                    urls.Select(url => {
                        string path = new Uri(url).AbsolutePath.ToLowerInvariant();
                        SitemapRuleRule? rule = rules.FirstOrDefault(r => path.StartsWith(r.Prefix));
                        
                        return new XElement(ns + "url",
                            new XElement(ns + "loc", url),
                            new XElement(ns + "lastmod", DateTime.UtcNow.ToString("yyyy-MM-dd")),
                            new XElement(ns + "changefreq", rule?.ChangeFreq ?? "daily"),
                            new XElement(ns + "priority", rule?.Priority ?? GetDefaultPriority(path))
                        );
                    })
                )
            );

            string outputPath = Path.Combine(PublishDir, "sitemap.xml");
            sitemap.Save(outputPath);

            Log.LogMessage(MessageImportance.High, $"Successfully generated sitemap with {urls.Count} URLs at {outputPath}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex);
            return false;
        }
    }

    private string GetDefaultPriority(string path)
    {
        return path is "/" or "/index" ? "1.0" : "0.5";
    }

    private class SitemapRuleRule
    {
        public string Prefix { get; set; } = null!;
        public string? Priority { get; set; }
        public string? ChangeFreq { get; set; }
    }
}
