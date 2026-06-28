namespace Cynnet.Core;

public class ServerConfig
{
    public string ServerName { get; set; } = "MyMinecraftServer";
    public string MinecraftVersion { get; set; } = "latest";
    public string ServerType { get; set; } = "Vanilla";
    public string DestinationRootFolder { get; set; } = "";
    public int RamGb { get; set; } = 4;
    public int Port { get; set; } = 25565;
    public int MaxPlayers { get; set; } = 20;
    public bool OnlineMode { get; set; } = true;
    public bool CreatePlayitHelper { get; set; } = false;
    public bool AcceptEula { get; set; } = false;

    public bool InstallGeyser { get; set; } = false;
    public bool InstallFloodgate { get; set; } = false;
    public bool InstallViaVersion { get; set; } = false;
    public bool InstallLuckPerms { get; set; } = false;
    public bool InstallEssentialsX { get; set; } = false;

    public bool HasAnyPluginSelected()
    {
        return InstallGeyser
               || InstallFloodgate
               || InstallViaVersion
               || InstallLuckPerms
               || InstallEssentialsX;
    }
}