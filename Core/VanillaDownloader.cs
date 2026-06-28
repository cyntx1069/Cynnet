using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cynnet.Core;

public static class VanillaDownloader
{
    private static readonly HttpClient Client = CreateClient();

    public static async Task DownloadVanillaAsync(string versionInput, string outputJarPath, Action<string> log)
    {
        using HttpResponseMessage manifestResponse = await Client.GetAsync("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");
        manifestResponse.EnsureSuccessStatusCode();

        string manifestJson = await manifestResponse.Content.ReadAsStringAsync();

        using JsonDocument manifestDoc = JsonDocument.Parse(manifestJson);

        string version = versionInput.Equals("latest", StringComparison.OrdinalIgnoreCase)
            ? GetLatestReleaseVersion(manifestDoc)
            : versionInput;

        log($"Using Minecraft version: {version}");

        string versionJsonUrl = GetVersionJsonUrl(manifestDoc, version);

        using HttpResponseMessage versionResponse = await Client.GetAsync(versionJsonUrl);
        versionResponse.EnsureSuccessStatusCode();

        string versionJson = await versionResponse.Content.ReadAsStringAsync();

        using JsonDocument versionDoc = JsonDocument.Parse(versionJson);

        string serverDownloadUrl = versionDoc.RootElement
            .GetProperty("downloads")
            .GetProperty("server")
            .GetProperty("url")
            .GetString() ?? throw new Exception("Could not find Vanilla server download URL.");

        using HttpResponseMessage serverResponse = await Client.GetAsync(serverDownloadUrl);
        serverResponse.EnsureSuccessStatusCode();

        await using FileStream fs = File.Create(outputJarPath);
        await serverResponse.Content.CopyToAsync(fs);

        log("Vanilla download complete.");
    }

    private static string GetLatestReleaseVersion(JsonDocument manifestDoc)
    {
        return manifestDoc.RootElement
            .GetProperty("latest")
            .GetProperty("release")
            .GetString() ?? throw new Exception("Could not find latest Vanilla release.");
    }

    private static string GetVersionJsonUrl(JsonDocument manifestDoc, string version)
    {
        foreach (JsonElement versionElement in manifestDoc.RootElement.GetProperty("versions").EnumerateArray())
        {
            string? id = versionElement.GetProperty("id").GetString();

            if (!string.Equals(id, version, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? url = versionElement.GetProperty("url").GetString();

            if (!string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }

        throw new Exception($"Could not find Vanilla version {version}.");
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient();

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("CynnetServerMaker", "0.7")
        );

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("(contact: change-this-email@example.com)")
        );

        return client;
    }
}