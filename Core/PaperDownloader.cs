using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cynnet.Core;

public static class PaperDownloader
{
    private static readonly HttpClient Client = CreateClient();

    public static async Task DownloadPaperAsync(string versionInput, string outputJarPath, Action<string> log)
    {
        string version = versionInput.Equals("latest", StringComparison.OrdinalIgnoreCase)
            ? await GetLatestVersionAsync()
            : versionInput;

        log($"Using Minecraft version: {version}");

        string downloadUrl = await GetLatestStableDownloadUrlAsync(version);

        log("Stable Paper build found.");
        log("Downloading server.jar...");

        using HttpResponseMessage response = await Client.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();

        await using FileStream fs = File.Create(outputJarPath);
        await response.Content.CopyToAsync(fs);

        log("Paper download complete.");
    }

    public static async Task<List<string>> GetAvailableVersionsAsync(int maxCount = 60)
    {
        using HttpResponseMessage response = await Client.GetAsync("https://fill.papermc.io/v3/projects/paper");
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("versions", out JsonElement versionsElement))
        {
            return new List<string>();
        }

        var versions = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (versionsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty group in versionsElement.EnumerateObject())
            {
                if (group.Value.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (JsonElement versionElement in group.Value.EnumerateArray())
                {
                    string? version = versionElement.GetString();

                    if (string.IsNullOrWhiteSpace(version))
                    {
                        continue;
                    }

                    if (seen.Add(version))
                    {
                        versions.Add(version);
                    }

                    if (versions.Count >= maxCount)
                    {
                        return versions;
                    }
                }
            }
        }

        return versions;
    }

    private static async Task<string> GetLatestVersionAsync()
    {
        List<string> versions = await GetAvailableVersionsAsync(1);

        if (versions.Count > 0)
        {
            return versions[0];
        }

        throw new Exception("Could not find latest Paper version.");
    }

    private static async Task<string> GetLatestStableDownloadUrlAsync(string version)
    {
        string url = $"https://fill.papermc.io/v3/projects/paper/versions/{version}/builds";

        using HttpResponseMessage response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(json);

        foreach (JsonElement build in doc.RootElement.EnumerateArray())
        {
            if (!build.TryGetProperty("channel", out JsonElement channelElement))
            {
                continue;
            }

            string? channel = channelElement.GetString();

            if (!string.Equals(channel, "STABLE", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!build.TryGetProperty("downloads", out JsonElement downloads))
            {
                continue;
            }

            if (!downloads.TryGetProperty("server:default", out JsonElement serverDefault))
            {
                continue;
            }

            if (!serverDefault.TryGetProperty("url", out JsonElement urlElement))
            {
                continue;
            }

            string? downloadUrl = urlElement.GetString();

            if (!string.IsNullOrWhiteSpace(downloadUrl))
            {
                return downloadUrl;
            }
        }

        throw new Exception($"No stable Paper build found for Minecraft {version}.");
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