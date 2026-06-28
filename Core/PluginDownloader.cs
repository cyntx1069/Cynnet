using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cynnet.Core;

public static class PluginDownloader
{
    private static readonly HttpClient Client = CreateClient();

    public static async Task DownloadSelectedPluginsAsync(ServerConfig config, string pluginsFolder, Action<string> log)
    {
        Directory.CreateDirectory(pluginsFolder);

        if (config.InstallGeyser)
        {
            await TryDownloadDirectAsync(
                "Geyser",
                "https://download.geysermc.org/v2/projects/geyser/versions/latest/builds/latest/downloads/spigot",
                Path.Combine(pluginsFolder, "Geyser-Spigot.jar"),
                log
            );
        }

        if (config.InstallFloodgate)
        {
            await TryDownloadDirectAsync(
                "Floodgate",
                "https://download.geysermc.org/v2/projects/floodgate/versions/latest/builds/latest/downloads/spigot",
                Path.Combine(pluginsFolder, "Floodgate-Spigot.jar"),
                log
            );
        }

        if (config.InstallViaVersion)
        {
            await TryDownloadModrinthPluginAsync(
                displayName: "ViaVersion",
                slug: "viaversion",
                fileName: "ViaVersion.jar",
                minecraftVersion: config.MinecraftVersion,
                pluginsFolder: pluginsFolder,
                log: log
            );
        }

        if (config.InstallLuckPerms)
        {
            await TryDownloadModrinthPluginAsync(
                displayName: "LuckPerms",
                slug: "luckperms",
                fileName: "LuckPerms.jar",
                minecraftVersion: config.MinecraftVersion,
                pluginsFolder: pluginsFolder,
                log: log
            );
        }

        if (config.InstallEssentialsX)
        {
            await TryDownloadModrinthPluginAsync(
                displayName: "EssentialsX",
                slug: "essentialsx",
                fileName: "EssentialsX.jar",
                minecraftVersion: config.MinecraftVersion,
                pluginsFolder: pluginsFolder,
                log: log
            );
        }

        log("Plugin installer finished.");
    }

    private static async Task TryDownloadDirectAsync(string displayName, string url, string outputPath, Action<string> log)
    {
        try
        {
            log($"Downloading {displayName}...");

            using HttpResponseMessage response = await Client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            await using FileStream fs = File.Create(outputPath);
            await response.Content.CopyToAsync(fs);

            log($"Installed {displayName}.");
        }
        catch (Exception ex)
        {
            log($"WARNING: Failed to download {displayName}: {ex.Message}");
        }
    }

    private static async Task TryDownloadModrinthPluginAsync(
        string displayName,
        string slug,
        string fileName,
        string minecraftVersion,
        string pluginsFolder,
        Action<string> log)
    {
        try
        {
            log($"Finding {displayName} on Modrinth...");

            string? downloadUrl = await GetModrinthDownloadUrlAsync(slug, minecraftVersion, log);

            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                log($"WARNING: Could not find a download for {displayName}.");
                return;
            }

            string outputPath = Path.Combine(pluginsFolder, fileName);

            log($"Downloading {displayName}...");

            using HttpResponseMessage response = await Client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();

            await using FileStream fs = File.Create(outputPath);
            await response.Content.CopyToAsync(fs);

            log($"Installed {displayName}.");
        }
        catch (Exception ex)
        {
            log($"WARNING: Failed to download {displayName}: {ex.Message}");
        }
    }

    private static async Task<string?> GetModrinthDownloadUrlAsync(string slug, string minecraftVersion, Action<string> log)
    {
        bool exactVersionWanted = !minecraftVersion.Equals("latest", StringComparison.OrdinalIgnoreCase);

        if (exactVersionWanted)
        {
            string exactUrl = BuildModrinthVersionsUrl(slug, minecraftVersion, includeGameVersion: true);
            string? exactResult = await TryGetDownloadUrlFromModrinthAsync(exactUrl);

            if (!string.IsNullOrWhiteSpace(exactResult))
            {
                return exactResult;
            }

            log($"No exact plugin match for Minecraft {minecraftVersion}. Trying latest compatible plugin release...");
        }

        string fallbackUrl = BuildModrinthVersionsUrl(slug, minecraftVersion, includeGameVersion: false);
        return await TryGetDownloadUrlFromModrinthAsync(fallbackUrl);
    }

    private static string BuildModrinthVersionsUrl(string slug, string minecraftVersion, bool includeGameVersion)
    {
        string loadersJson = Uri.EscapeDataString("[\"paper\",\"spigot\",\"bukkit\",\"purpur\"]");

        var url = new StringBuilder();
        url.Append("https://api.modrinth.com/v2/project/");
        url.Append(Uri.EscapeDataString(slug));
        url.Append("/version?loaders=");
        url.Append(loadersJson);

        if (includeGameVersion)
        {
            string gameVersionsJson = Uri.EscapeDataString($"[\"{minecraftVersion}\"]");
            url.Append("&game_versions=");
            url.Append(gameVersionsJson);
        }

        return url.ToString();
    }

    private static async Task<string?> TryGetDownloadUrlFromModrinthAsync(string url)
    {
        using HttpResponseMessage response = await Client.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        string json = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(json);

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        JsonElement[] versions = doc.RootElement.EnumerateArray().ToArray();

        string? releaseFile = FindDownloadUrl(versions, releaseOnly: true);

        if (!string.IsNullOrWhiteSpace(releaseFile))
        {
            return releaseFile;
        }

        return FindDownloadUrl(versions, releaseOnly: false);
    }

    private static string? FindDownloadUrl(JsonElement[] versions, bool releaseOnly)
    {
        foreach (JsonElement version in versions)
        {
            if (releaseOnly)
            {
                if (!version.TryGetProperty("version_type", out JsonElement versionTypeElement))
                {
                    continue;
                }

                string? versionType = versionTypeElement.GetString();

                if (!string.Equals(versionType, "release", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            if (!version.TryGetProperty("files", out JsonElement filesElement))
            {
                continue;
            }

            JsonElement? primaryFile = null;
            JsonElement? firstFile = null;

            foreach (JsonElement file in filesElement.EnumerateArray())
            {
                firstFile ??= file;

                if (file.TryGetProperty("primary", out JsonElement primaryElement) && primaryElement.GetBoolean())
                {
                    primaryFile = file;
                    break;
                }
            }

            JsonElement selectedFile = primaryFile ?? firstFile ?? default;

            if (selectedFile.ValueKind == JsonValueKind.Undefined)
            {
                continue;
            }

            if (selectedFile.TryGetProperty("url", out JsonElement urlElement))
            {
                string? downloadUrl = urlElement.GetString();

                if (!string.IsNullOrWhiteSpace(downloadUrl))
                {
                    return downloadUrl;
                }
            }
        }

        return null;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("CynnetServerMaker", "0.6")
        );

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("(contact: change-this-email@example.com)")
        );

        return client;
    }
}