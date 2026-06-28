using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cynnet.Core;

public static class FabricDownloader
{
    private static readonly HttpClient Client = CreateClient();

    public static async Task DownloadFabricAsync(string versionInput, string outputJarPath, Action<string> log)
    {
        string minecraftVersion = versionInput.Equals("latest", StringComparison.OrdinalIgnoreCase)
            ? await VanillaDownloaderLatestVersionHelper.GetLatestReleaseAsync()
            : versionInput;

        log($"Using Minecraft version: {minecraftVersion}");

        string loaderVersion = await GetLatestLoaderVersionAsync(minecraftVersion);
        string installerVersion = await GetLatestInstallerVersionAsync();

        log($"Using Fabric loader: {loaderVersion}");
        log($"Using Fabric installer: {installerVersion}");

        string downloadUrl =
            $"https://meta.fabricmc.net/v2/versions/loader/{minecraftVersion}/{loaderVersion}/{installerVersion}/server/jar";

        using HttpResponseMessage response = await Client.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();

        await using FileStream fs = File.Create(outputJarPath);
        await response.Content.CopyToAsync(fs);

        log("Fabric download complete.");
    }

    private static async Task<string> GetLatestLoaderVersionAsync(string minecraftVersion)
    {
        string url = $"https://meta.fabricmc.net/v2/versions/loader/{minecraftVersion}";

        using HttpResponseMessage response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(json);

        JsonElement[] loaders = doc.RootElement.EnumerateArray().ToArray();

        foreach (JsonElement loaderEntry in loaders)
        {
            JsonElement loader = loaderEntry.GetProperty("loader");

            bool stable = loader.GetProperty("stable").GetBoolean();

            if (stable)
            {
                return loader.GetProperty("version").GetString()
                       ?? throw new Exception("Could not read Fabric loader version.");
            }
        }

        if (loaders.Length > 0)
        {
            return loaders[0]
                .GetProperty("loader")
                .GetProperty("version")
                .GetString() ?? throw new Exception("Could not read Fabric loader version.");
        }

        throw new Exception($"Could not find Fabric loader for Minecraft {minecraftVersion}.");
    }

    private static async Task<string> GetLatestInstallerVersionAsync()
    {
        using HttpResponseMessage response = await Client.GetAsync("https://meta.fabricmc.net/v2/versions/installer");
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(json);

        JsonElement[] installers = doc.RootElement.EnumerateArray().ToArray();

        foreach (JsonElement installer in installers)
        {
            bool stable = installer.GetProperty("stable").GetBoolean();

            if (stable)
            {
                return installer.GetProperty("version").GetString()
                       ?? throw new Exception("Could not read Fabric installer version.");
            }
        }

        if (installers.Length > 0)
        {
            return installers[0].GetProperty("version").GetString()
                   ?? throw new Exception("Could not read Fabric installer version.");
        }

        throw new Exception("Could not find Fabric installer.");
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

internal static class VanillaDownloaderLatestVersionHelper
{
    public static async Task<string> GetLatestReleaseAsync()
    {
        using HttpClient client = new HttpClient();

        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("CynnetServerMaker", "0.7")
        );

        using HttpResponseMessage response = await client.GetAsync("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();

        using JsonDocument doc = JsonDocument.Parse(json);

        return doc.RootElement
            .GetProperty("latest")
            .GetProperty("release")
            .GetString() ?? throw new Exception("Could not find latest Minecraft release.");
    }
}