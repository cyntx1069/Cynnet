using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Cynnet.Core;

public class ServerCreator
{
    public async Task<string> CreateAsync(ServerConfig config, Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(config.ServerName))
        {
            throw new Exception("Server name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(config.DestinationRootFolder))
        {
            throw new Exception("Destination folder cannot be empty.");
        }

        string serverType = config.ServerType.Trim();

        if (!IsSupportedServerType(serverType))
        {
            throw new Exception($"{serverType} server creation is visible in the UI, but it is not fully supported yet. Use Paper, Vanilla, or Fabric for now.");
        }

        string safeName = MakeSafeFolderName(config.ServerName);
        string baseFolder = config.DestinationRootFolder.Trim();

        Directory.CreateDirectory(baseFolder);

        string requestedServerFolder = Path.Combine(baseFolder, safeName);
        string serverFolder = GetUniqueFolderPath(requestedServerFolder);
        string pluginsFolder = Path.Combine(serverFolder, "plugins");
        string modsFolder = Path.Combine(serverFolder, "mods");

        log("Creating folders...");

        Directory.CreateDirectory(serverFolder);
        Directory.CreateDirectory(Path.Combine(serverFolder, "backups"));
        Directory.CreateDirectory(Path.Combine(serverFolder, "logs"));

        if (serverType.Equals("Paper", StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(pluginsFolder);
        }

        if (serverType.Equals("Fabric", StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(modsFolder);
        }

        if (!string.Equals(requestedServerFolder, serverFolder, StringComparison.OrdinalIgnoreCase))
        {
            log($"Server folder already existed. Created new folder: {Path.GetFileName(serverFolder)}");
        }

        string jarPath = Path.Combine(serverFolder, "server.jar");

        log($"Creating {serverType} server...");

        if (serverType.Equals("Paper", StringComparison.OrdinalIgnoreCase))
        {
            log("Downloading Paper server.jar...");
            await PaperDownloader.DownloadPaperAsync(config.MinecraftVersion, jarPath, log);

            if (config.HasAnyPluginSelected())
            {
                log("Installing selected plugins...");
                await PluginDownloader.DownloadSelectedPluginsAsync(config, pluginsFolder, log);
            }
            else
            {
                log("No plugins selected.");
            }

            if (config.InstallGeyser && config.InstallFloodgate)
            {
                log("Creating Geyser/Floodgate note...");
                File.WriteAllText(Path.Combine(serverFolder, "GEYSER_FLOODGATE_NOTE.txt"), CreateGeyserFloodgateNote(), Encoding.UTF8);
            }
        }
        else if (serverType.Equals("Vanilla", StringComparison.OrdinalIgnoreCase))
        {
            log("Downloading Vanilla server.jar...");
            await VanillaDownloader.DownloadVanillaAsync(config.MinecraftVersion, jarPath, log);

            if (config.HasAnyPluginSelected())
            {
                log("Plugins were selected, but Vanilla servers do not support plugins. Skipping plugins.");
            }
        }
        else if (serverType.Equals("Fabric", StringComparison.OrdinalIgnoreCase))
        {
            log("Downloading Fabric server.jar...");
            await FabricDownloader.DownloadFabricAsync(config.MinecraftVersion, jarPath, log);

            if (config.HasAnyPluginSelected())
            {
                log("Plugins were selected, but Fabric servers use mods instead. Skipping plugins.");
            }
        }

        log("Creating eula.txt...");
        File.WriteAllText(
            Path.Combine(serverFolder, "eula.txt"),
            $"# Created by Cynnet{Environment.NewLine}eula={config.AcceptEula.ToString().ToLower()}"
        );

        if (!config.AcceptEula)
        {
            log("WARNING: EULA is false. The server will not start until you accept it.");
        }

        log("Creating server.properties...");
        File.WriteAllText(Path.Combine(serverFolder, "server.properties"), CreateServerProperties(config), Encoding.UTF8);

        log("Creating start.bat...");
        File.WriteAllText(Path.Combine(serverFolder, "start.bat"), CreateStartBat(config), Encoding.ASCII);

        if (config.CreatePlayitHelper)
        {
            log("Creating start_all.bat for playit.gg...");
            File.WriteAllText(Path.Combine(serverFolder, "start_all.bat"), CreatePlayitBat(), Encoding.ASCII);
        }

        log("Server files created successfully.");

        return serverFolder;
    }

    private static bool IsSupportedServerType(string serverType)
    {
        return serverType.Equals("Paper", StringComparison.OrdinalIgnoreCase)
               || serverType.Equals("Vanilla", StringComparison.OrdinalIgnoreCase)
               || serverType.Equals("Fabric", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateServerProperties(ServerConfig config)
    {
        return $"""
        #Minecraft server properties
        #Created by Cynnet
        server-port={config.Port}
        motd={config.ServerName}
        max-players={config.MaxPlayers}
        online-mode={config.OnlineMode.ToString().ToLower()}
        enable-command-block=false
        difficulty=normal
        gamemode=survival
        pvp=true
        view-distance=10
        simulation-distance=10
        spawn-protection=16
        """;
    }

    private static string CreateStartBat(ServerConfig config)
    {
        return $"""
        @echo off
        title {config.ServerName}
        echo Starting {config.ServerName}...
        echo Server type: {config.ServerType}
        echo.
        java -Xms{config.RamGb}G -Xmx{config.RamGb}G -jar server.jar --nogui
        echo.
        pause
        """;
    }

    private static string CreatePlayitBat()
    {
        return """
        @echo off
        title Cynnet - Server + playit.gg

        if exist playit.exe (
            echo Starting playit.gg...
            start "playit.gg" playit.exe
            timeout /t 3 /nobreak > nul
        ) else (
            echo playit.exe was not found in this folder.
            echo Put your playit.exe next to this file if you want auto-start.
            echo.
        )

        call start.bat
        """;
    }

    private static string CreateGeyserFloodgateNote()
    {
        return """
        Geyser + Floodgate note

        The plugins were downloaded into the plugins folder.

        First start:
        1. Start the server once.
        2. Stop the server after plugin files/configs generate.
        3. Open plugins/Geyser-Spigot/config.yml.
        4. Find auth-type.
        5. Set it to floodgate.
        6. Start the server again.

        Later Cynnet can automate this after the first server run.
        """;
    }

    private static string MakeSafeFolderName(string input)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            input = input.Replace(c, '_');
        }

        input = input.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return "MinecraftServer";
        }

        return input;
    }

    private static string GetUniqueFolderPath(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return folderPath;
        }

        string? parent = Path.GetDirectoryName(folderPath);
        string name = Path.GetFileName(folderPath);

        if (string.IsNullOrWhiteSpace(parent))
        {
            parent = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        }

        int counter = 1;

        while (true)
        {
            string newPath = Path.Combine(parent, $"{name} ({counter})");

            if (!Directory.Exists(newPath))
            {
                return newPath;
            }

            counter++;
        }
    }
}