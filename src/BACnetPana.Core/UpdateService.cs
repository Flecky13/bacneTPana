using System.Diagnostics;
using System.Text.Json;

namespace bacneTPana.Core
{
    /// <summary>
    /// Service fÃ¼r Update-ÃœberprÃ¼fung gegen GitHub Releases
    /// </summary>
    public class UpdateService
    {
        private const string GitHubApiLatestUrl = "https://api.github.com/repos/Flecky13/bacneTPana/releases/latest";
        private const string GitHubApiAllReleasesUrl = "https://api.github.com/repos/Flecky13/bacneTPana/releases";
        private const string GitHubRepoUrl = "https://github.com/Flecky13/bacneTPana/releases";
        private static string CurrentVersion = GetCurrentVersion();
        private const string AppName = "bacneTPana";
        private const string Author = "Flecky13";

        /// <summary>
        /// Liest die aktuelle Version aus der Assembly
        /// </summary>
        private static string GetCurrentVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
                }
            }
            catch { }

            // Fallback
            return "1.0.0.0";
        }

        public class VersionInfo
        {
            public string? CurrentVersion { get; set; }
            public string? LatestVersion { get; set; }
            public string? DownloadUrl { get; set; }
            public string? ReleaseNotes { get; set; }
            public bool UpdateAvailable { get; set; }
            public string? Author { get; set; }
            public DateTime? ReleaseDate { get; set; }
        }

        /// <summary>
        /// PrÃ¼ft die neueste verfÃ¼gbare Version auf GitHub
        /// </summary>
        public async Task<VersionInfo> CheckForUpdatesAsync()
        {
            var versionInfo = new VersionInfo
            {
                CurrentVersion = CurrentVersion,
                Author = Author,
                UpdateAvailable = false
            };

            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                httpClient.DefaultRequestHeaders.Add("User-Agent", $"{AppName}/{CurrentVersion}");

                // Versuche zuerst latest endpoint
                var json = await TryGetReleaseJson(httpClient, GitHubApiLatestUrl);

                // Falls leer (z.B. nur Drafts/Pre-releases), versuche alle releases
                if (string.IsNullOrEmpty(json))
                {
                    json = await TryGetReleaseJsonFromAll(httpClient, GitHubApiAllReleasesUrl);
                }

                if (string.IsNullOrEmpty(json))
                {
                    return versionInfo;
                }

                // Parse JSON
                ParseReleaseInfo(json, versionInfo);
            }
            catch
            {
            }

            return versionInfo;
        }

        /// <summary>
        /// Versucht, den /latest Endpoint zu lesen
        /// </summary>
        private async Task<string> TryGetReleaseJson(HttpClient httpClient, string url)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return json;
                }
                else
                {
                }
            }
            catch
            {
            }

            return string.Empty;
        }

        /// <summary>
        /// Versucht, aus dem /releases Array die neueste Release zu finden
        /// </summary>
        private async Task<string> TryGetReleaseJsonFromAll(HttpClient httpClient, string url)
        {
            try
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                var json = await response.Content.ReadAsStringAsync();

                using var jsonDoc = JsonDocument.Parse(json);
                var root = jsonDoc.RootElement;

                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    // Finde die neueste nicht-Draft Release
                    foreach (var release in root.EnumerateArray())
                    {
                        bool isDraft = false;
                        if (release.TryGetProperty("draft", out var draftProp))
                        {
                            isDraft = draftProp.GetBoolean();
                        }

                        if (!isDraft && release.TryGetProperty("tag_name", out _))
                        {
                            // Gib diese Release als JSON zurÃ¼ck
                            return release.GetRawText();
                        }
                    }
                }

            }
            catch
            {
            }

            return string.Empty;
        }

        /// <summary>
        /// Parst die Release-Informationen aus dem JSON
        /// </summary>
        private void ParseReleaseInfo(string json, VersionInfo versionInfo)
        {
            using var jsonDoc = JsonDocument.Parse(json);
            var root = jsonDoc.RootElement;

            // Extrahiere Version aus dem Tag-Namen (z.B. "v1.4.0" oder "V1.3.1.0" -> "1.4.0" oder "1.3.1.0")
            if (root.TryGetProperty("tag_name", out var tagElement))
            {
                var tagName = tagElement.GetString();
                // Entferne sowohl kleine als auch groÃŸe 'v' am Anfang
                versionInfo.LatestVersion = tagName?.TrimStart('v', 'V') ?? CurrentVersion;

                // PrÃ¼fe ob Update verfÃ¼gbar ist
                var comparison = CompareVersions(versionInfo.LatestVersion, CurrentVersion);

                if (comparison > 0)
                {
                    versionInfo.UpdateAvailable = true;
                }
            }
            else
            {
            }

            // Extrahiere Download-URL
            if (root.TryGetProperty("assets", out var assetsElement) && assetsElement.ValueKind == JsonValueKind.Array)
            {
                var assetCount = 0;
                foreach (var asset in assetsElement.EnumerateArray())
                {
                    assetCount++;
                    if (asset.TryGetProperty("browser_download_url", out var urlElement))
                    {
                        var url = urlElement.GetString();
                        // Suche nach .exe oder .zip Datei
                        if (url?.Contains(".exe") == true || url?.Contains(".zip") == true)
                        {
                            versionInfo.DownloadUrl = url;
                            break;
                        }
                    }
                }
            }

            // Extrahiere Release Notes
            if (root.TryGetProperty("body", out var bodyElement))
            {
                versionInfo.ReleaseNotes = bodyElement.GetString() ?? string.Empty;
            }

            // Extrahiere Release Date
            if (root.TryGetProperty("published_at", out var dateElement))
            {
                if (DateTime.TryParse(dateElement.GetString(), out var releaseDate))
                {
                    versionInfo.ReleaseDate = releaseDate;
                }
            }

        }

        /// <summary>
        /// Downloadet die neueste Version
        /// </summary>
        public async Task<bool> DownloadUpdateAsync(string downloadUrl, string savePath)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", $"{AppName}/{CurrentVersion}");

                var response = await httpClient.GetAsync(downloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var content = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(savePath, content);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ã–ffnet die GitHub Release-Seite im Browser
        /// </summary>
        public void OpenGitHubReleasePage()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = GitHubRepoUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }

        /// <summary>
        /// Vergleicht zwei Versionsnummern
        /// </summary>
        /// <returns>-1 wenn v1 < v2, 0 wenn gleich, 1 wenn v1 > v2</returns>
        private int CompareVersions(string v1, string v2)
        {
            try
            {
                var version1 = new Version(v1);
                var version2 = new Version(v2);
                var result = version1.CompareTo(version2);
                return result;
            }
            catch (FormatException)
            {
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Gibt Informationen Ã¼ber die Anwendung zurÃ¼ck
        /// </summary>
        public VersionInfo GetApplicationInfo()
        {
            return new VersionInfo
            {
                CurrentVersion = CurrentVersion,
                Author = Author,
                UpdateAvailable = false,
                LatestVersion = CurrentVersion
            };
        }
    }
}
