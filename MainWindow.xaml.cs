using Cynnet.Core;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using WinRT.Interop;

namespace Cynnet;

public sealed partial class MainWindow : Window
{
    private string? _lastCreatedServerFolder;
    private JavaCheckResult? _lastJavaCheck;

    public MainWindow()
    {
        InitializeComponent();

        CenterWindow();

        _ = SetAppIconAndLogoAsync();

        SetupTabs();
        SetupServerTypes();
        SetupFallbackMinecraftVersions();
        SetupDefaultDestinationFolder();

        ShowPage("ServerCreation");

        AppendLog($"Welcome to {GetAppBrandName()}! Current version is {GetAppVersion()}");

        if (AutoCheckJavaCheckBox.IsChecked == true)
        {
            _ = CheckJavaAsync();
        }
        else
        {
            JavaStatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 156, 163, 175));
        }

        _ = LoadMinecraftVersionsAsync();
    }

    private void CenterWindow()
    {
        int windowWidth = 1424;
        int windowHeight = 820;

        IntPtr hwnd = WindowNative.GetWindowHandle(this);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);

        DisplayArea displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);
        RectInt32 workArea = displayArea.WorkArea;

        int x = workArea.X + (workArea.Width - windowWidth) / 2;
        int y = workArea.Y + (workArea.Height - windowHeight) / 2;

        AppWindow.MoveAndResize(new RectInt32(x, y, windowWidth, windowHeight));
    }

    private async Task SetAppIconAndLogoAsync()
    {
        try
        {
            string assetsFolder = Path.Combine(AppContext.BaseDirectory, "Assets");
            string iconPath = Path.Combine(assetsFolder, "Cynnet_AppIcon.ico");
            string pngPath = Path.Combine(assetsFolder, "Cynnet_AppIcon.png");

            if (File.Exists(iconPath))
            {
                AppWindow.SetIcon(iconPath);
            }

            if (File.Exists(pngPath))
            {
                StorageFile logoFile = await StorageFile.GetFileFromPathAsync(pngPath);

                using var stream = await logoFile.OpenAsync(FileAccessMode.Read);

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);

                AppLogoImage.Source = bitmap;
                AppLogoImage.Visibility = Visibility.Visible;
                AppLogoFallbackText.Visibility = Visibility.Collapsed;
            }
            else
            {
                AppLogoImage.Visibility = Visibility.Collapsed;
                AppLogoFallbackText.Visibility = Visibility.Visible;
            }
        }
        catch
        {
            AppLogoImage.Visibility = Visibility.Collapsed;
            AppLogoFallbackText.Visibility = Visibility.Visible;
        }
    }

    private string GetAppBrandName()
    {
        string brandName = AppBrandNameText.Text?.Trim() ?? "Cynnet";

        if (string.IsNullOrWhiteSpace(brandName))
        {
            return "Cynnet";
        }

        return brandName;
    }

    private string GetAppVersion()
    {
        string versionText = AppVersionText.Text?.Trim() ?? "Version unknown";

        if (versionText.StartsWith("Version ", StringComparison.OrdinalIgnoreCase))
        {
            versionText = versionText["Version ".Length..].Trim();
        }

        if (string.IsNullOrWhiteSpace(versionText))
        {
            return "unknown";
        }

        return versionText;
    }

    private void SetupTabs()
    {
        SetSelectedTab(ServerCreationTabButton);
    }

    private void SetupServerTypes()
    {
        ServerTypeComboBox.Items.Clear();

        ServerTypeComboBox.Items.Add("Vanilla");
        ServerTypeComboBox.Items.Add("Paper");
        ServerTypeComboBox.Items.Add("Spigot (Coming soon)");
        ServerTypeComboBox.Items.Add("Bukkit (Coming soon)");
        ServerTypeComboBox.Items.Add("Forge (Coming soon)");
        ServerTypeComboBox.Items.Add("Fabric");

        ServerTypeComboBox.SelectedIndex = 0;
        UpdateAddOnAvailability();
    }

    private void SetupDefaultDestinationFolder()
    {
        DestinationFolderBox.Text = @"C:\Users\User\Desktop\Minecraft Server";
    }

    private void TabButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        if (button.Tag is not string pageName)
        {
            return;
        }

        ShowPage(pageName);
        SetSelectedTab(button);
    }

    private void SetSelectedTab(Button selectedButton)
    {
        Button[] buttons =
        {
            ServerCreationTabButton,
            AddOnsTabButton,
            OtherTabButton,
            OutputTabButton,
            SettingsTabButton
        };

        foreach (Button button in buttons)
        {
            bool isSelected = button == selectedButton;

            button.Background = new SolidColorBrush(isSelected
                ? Color.FromArgb(255, 27, 27, 36)
                : Color.FromArgb(0, 0, 0, 0));

            button.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            button.BorderThickness = new Thickness(0);

            button.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        }
    }

    private void ShowPage(string pageName)
    {
        ServerCreationPage.Visibility = pageName == "ServerCreation" ? Visibility.Visible : Visibility.Collapsed;
        AddOnsPage.Visibility = pageName == "AddOns" ? Visibility.Visible : Visibility.Collapsed;
        OtherPage.Visibility = pageName == "Other" ? Visibility.Visible : Visibility.Collapsed;
        OutputPage.Visibility = pageName == "Output" ? Visibility.Visible : Visibility.Collapsed;
        SettingsPage.Visibility = pageName == "Settings" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetupFallbackMinecraftVersions()
    {
        VersionComboBox.Items.Clear();

        VersionComboBox.Items.Add("latest");
        VersionComboBox.Items.Add("1.21.10");
        VersionComboBox.Items.Add("1.21.9");
        VersionComboBox.Items.Add("1.21.8");
        VersionComboBox.Items.Add("1.21.7");
        VersionComboBox.Items.Add("1.21.6");
        VersionComboBox.Items.Add("1.21.5");
        VersionComboBox.Items.Add("1.21.4");
        VersionComboBox.Items.Add("1.20.6");
        VersionComboBox.Items.Add("1.20.4");
        VersionComboBox.Items.Add("1.20.1");

        VersionComboBox.SelectedIndex = 0;
    }

    private async Task LoadMinecraftVersionsAsync()
    {
        try
        {
            string selectedBeforeLoad = GetSelectedMinecraftVersion();

            var versions = await PaperDownloader.GetAvailableVersionsAsync(60);

            if (versions.Count == 0)
            {
                AppendLog("WARNING: Could not load versions. Using fallback list.");
                return;
            }

            VersionComboBox.Items.Clear();
            VersionComboBox.Items.Add("latest");

            foreach (string version in versions)
            {
                VersionComboBox.Items.Add(version);
            }

            int selectedIndex = 0;

            for (int i = 0; i < VersionComboBox.Items.Count; i++)
            {
                if (string.Equals(VersionComboBox.Items[i]?.ToString(), selectedBeforeLoad, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIndex = i;
                    break;
                }
            }

            VersionComboBox.SelectedIndex = selectedIndex;

            AppendLog($"Loaded {versions.Count} versions.");
        }
        catch (Exception ex)
        {
            AppendLog($"WARNING: Could not load versions. Using fallback list. {ex.Message}");
        }
    }

    private string GetSelectedMinecraftVersion()
    {
        return VersionComboBox.SelectedItem?.ToString()?.Trim() ?? "latest";
    }

    private string GetSelectedServerType()
    {
        string selected = ServerTypeComboBox.SelectedItem?.ToString()?.Trim() ?? "Vanilla";

        if (selected.Contains("Spigot", StringComparison.OrdinalIgnoreCase))
        {
            return "Spigot";
        }

        if (selected.Contains("Bukkit", StringComparison.OrdinalIgnoreCase))
        {
            return "Bukkit";
        }

        if (selected.Contains("Forge", StringComparison.OrdinalIgnoreCase))
        {
            return "Forge";
        }

        return selected;
    }

    private void ServerTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateAddOnAvailability();
    }

    private void UpdateAddOnAvailability()
    {
        string serverType = GetSelectedServerType();

        bool isPluginServer =
            serverType.Equals("Paper", StringComparison.OrdinalIgnoreCase)
            || serverType.Equals("Spigot", StringComparison.OrdinalIgnoreCase)
            || serverType.Equals("Bukkit", StringComparison.OrdinalIgnoreCase);

        bool isModServer =
            serverType.Equals("Fabric", StringComparison.OrdinalIgnoreCase)
            || serverType.Equals("Forge", StringComparison.OrdinalIgnoreCase);

        bool isVanilla =
            serverType.Equals("Vanilla", StringComparison.OrdinalIgnoreCase);

        SetPluginControlsEnabled(isPluginServer);
        SetModControlsEnabled(isModServer);

        if (isPluginServer)
        {
            PluginStatusText.Text = "Available for this plugin-type server.";
            ModStatusText.Text = "Unavailable because this server type uses plugins, not mods.";
        }
        else if (isModServer)
        {
            PluginStatusText.Text = "Unavailable because this server type uses mods, not plugins.";
            ModStatusText.Text = "Available for this mod-type server.";
        }
        else if (isVanilla)
        {
            PluginStatusText.Text = "Unavailable because Vanilla servers do not use plugins.";
            ModStatusText.Text = "Unavailable because Vanilla servers do not use mods.";
        }
    }

    private void SetPluginControlsEnabled(bool enabled)
    {
        GeyserCheckBox.IsEnabled = enabled;
        FloodgateCheckBox.IsEnabled = enabled;
        ViaVersionCheckBox.IsEnabled = enabled;
        LuckPermsCheckBox.IsEnabled = enabled;
        EssentialsXCheckBox.IsEnabled = enabled;

        if (!enabled)
        {
            GeyserCheckBox.IsChecked = false;
            FloodgateCheckBox.IsChecked = false;
            ViaVersionCheckBox.IsChecked = false;
            LuckPermsCheckBox.IsChecked = false;
            EssentialsXCheckBox.IsChecked = false;
        }
    }

    private void SetModControlsEnabled(bool enabled)
    {
        LithiumCheckBox.IsEnabled = enabled;
        FerriteCoreCheckBox.IsEnabled = enabled;
        SparkCheckBox.IsEnabled = enabled;
        SimpleVoiceChatCheckBox.IsEnabled = enabled;
        WorldEditModCheckBox.IsEnabled = enabled;

        if (!enabled)
        {
            LithiumCheckBox.IsChecked = false;
            FerriteCoreCheckBox.IsChecked = false;
            SparkCheckBox.IsChecked = false;
            SimpleVoiceChatCheckBox.IsChecked = false;
            WorldEditModCheckBox.IsChecked = false;
        }
    }

    private void RamSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (RamLabel != null)
        {
            RamLabel.Text = $"RAM: {(int)e.NewValue} GB";
        }
    }

    private async void CheckJavaButton_Click(object sender, RoutedEventArgs e)
    {
        await CheckJavaAsync();
    }

    private async Task CheckJavaAsync()
    {
        CheckJavaButton.IsEnabled = false;
        JavaStatusText.Text = "Checking Java...";
        JavaStatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 156, 163, 175));

        try
        {
            _lastJavaCheck = await JavaChecker.CheckAsync();

            if (!_lastJavaCheck.IsInstalled)
            {
                JavaStatusText.Text = "Java was not found. The server files can be created, but the server cannot start until Java is installed.";
                JavaStatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 248, 113, 113));
                AppendLog("Java check: Java was not found.");
                return;
            }

            if (_lastJavaCheck.MajorVersion > 0 && _lastJavaCheck.MajorVersion < 21)
            {
                JavaStatusText.Text = $"Java version: {_lastJavaCheck.MajorVersion}";
                JavaStatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 251, 191, 36));
                AppendLog($"Java version: {_lastJavaCheck.MajorVersion} - may be too old.");
                return;
            }

            JavaStatusText.Text = $"Java version: {_lastJavaCheck.MajorVersion}";
            JavaStatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 34, 197, 94));
            AppendLog($"Java version: {_lastJavaCheck.MajorVersion}");
        }
        catch (Exception ex)
        {
            JavaStatusText.Text = "Java check failed.";
            JavaStatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 248, 113, 113));
            AppendLog($"Java check error: {ex.Message}");
        }
        finally
        {
            CheckJavaButton.IsEnabled = true;
        }
    }

    private async void BrowseDestinationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            FolderPicker picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");

            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                DestinationFolderBox.Text = folder.Path;
                AppendLog($"Selected destination folder: {folder.Path}");
            }
        }
        catch (Exception ex)
        {
            AppendLog($"Folder picker error: {ex.Message}");
            await ShowMessageAsync("Folder picker error", ex.Message);
        }
    }

    private async void CreateServerButton_Click(object sender, RoutedEventArgs e)
    {
        CreateServerButton.IsEnabled = false;
        OpenFolderButton.IsEnabled = false;
        RunServerButton.IsEnabled = false;
        RunAllButton.IsEnabled = false;

        LogBox.Text = "";
        ShowPage("Output");
        SetSelectedTab(OutputTabButton);

        try
        {
            if (!TryBuildServerConfig(out ServerConfig config, out string errorMessage))
            {
                AppendLog("ERROR:");
                AppendLog(errorMessage);

                await ShowMessageAsync("Invalid settings", errorMessage);
                return;
            }

            if (_lastJavaCheck == null)
            {
                AppendLog("Checking Java before creating server...");
                _lastJavaCheck = await JavaChecker.CheckAsync();
            }

            if (_lastJavaCheck.IsInstalled)
            {
                AppendLog($"Java detected: {_lastJavaCheck.DisplayVersion}");
            }
            else
            {
                AppendLog("WARNING: Java was not found. Server files will still be created, but start.bat will not work until Java is installed.");
            }

            if (!config.AcceptEula)
            {
                AppendLog("WARNING: EULA is not accepted. The server will be created, but Minecraft will not start until eula=true.");
            }

            var creator = new ServerCreator();
            string folder = await creator.CreateAsync(config, AppendLog);

            _lastCreatedServerFolder = folder;

            OpenFolderButton.IsEnabled = true;
            RunServerButton.IsEnabled = true;
            RunAllButton.IsEnabled = File.Exists(Path.Combine(folder, "start_all.bat"));

            AppendLog("");
            AppendLog("DONE: Server created at:");
            AppendLog(folder);

            await ShowMessageAsync("Server created", $"Your server was created here:\n{folder}");
        }
        catch (Exception ex)
        {
            AppendLog("");
            AppendLog("ERROR:");
            AppendLog(ex.Message);

            await ShowMessageAsync("Error", ex.Message);
        }
        finally
        {
            CreateServerButton.IsEnabled = true;
        }
    }

    private bool TryBuildServerConfig(out ServerConfig config, out string errorMessage)
    {
        config = new ServerConfig();
        errorMessage = "";

        string serverName = ServerNameBox.Text.Trim();
        string minecraftVersion = GetSelectedMinecraftVersion();
        string serverType = GetSelectedServerType();
        string destinationFolder = DestinationFolderBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(serverName))
        {
            errorMessage = "Server name cannot be empty.";
            return false;
        }

        if (serverName.Length > 60)
        {
            errorMessage = "Server name is too long. Use 60 characters or less.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(minecraftVersion))
        {
            errorMessage = "Minecraft version cannot be empty.";
            return false;
        }

        if (!minecraftVersion.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            bool looksLikeVersion = Regex.IsMatch(minecraftVersion, @"^[0-9]+(\.[0-9]+){1,3}$");

            if (!looksLikeVersion)
            {
                errorMessage = "Minecraft version must be latest or a normal version number like 1.21.10.";
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(destinationFolder))
        {
            errorMessage = "Destination folder cannot be empty.";
            return false;
        }

        if (!int.TryParse(PortBox.Text, out int port))
        {
            errorMessage = "Port must be a number.";
            return false;
        }

        if (port < 1 || port > 65535)
        {
            errorMessage = "Port must be between 1 and 65535.";
            return false;
        }

        if (!int.TryParse(MaxPlayersBox.Text, out int maxPlayers))
        {
            errorMessage = "Max players must be a number.";
            return false;
        }

        if (maxPlayers < 1)
        {
            errorMessage = "Max players must be at least 1.";
            return false;
        }

        if (maxPlayers > 1000)
        {
            errorMessage = "Max players is too high. Use 1000 or less.";
            return false;
        }

        int ramGb = (int)RamSlider.Value;

        config = new ServerConfig
        {
            ServerName = serverName,
            MinecraftVersion = minecraftVersion,
            ServerType = serverType,
            DestinationRootFolder = destinationFolder,
            RamGb = ramGb,
            Port = port,
            MaxPlayers = maxPlayers,
            OnlineMode = OnlineModeSwitch.IsOn,
            CreatePlayitHelper = PlayitCheckBox.IsChecked == true,
            AcceptEula = EulaSwitch.IsOn,

            InstallGeyser = GeyserCheckBox.IsChecked == true,
            InstallFloodgate = FloodgateCheckBox.IsChecked == true,
            InstallViaVersion = ViaVersionCheckBox.IsChecked == true,
            InstallLuckPerms = LuckPermsCheckBox.IsChecked == true,
            InstallEssentialsX = EssentialsXCheckBox.IsChecked == true
        };

        return true;
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_lastCreatedServerFolder) || !Directory.Exists(_lastCreatedServerFolder))
        {
            AppendLog("ERROR: No server folder found.");
            return;
        }

        AppendLog("Opening server folder...");

        Process.Start(new ProcessStartInfo
        {
            FileName = _lastCreatedServerFolder,
            UseShellExecute = true
        });
    }

    private async void RunServerButton_Click(object sender, RoutedEventArgs e)
    {
        await RunBatFileAsync("start.bat");
    }

    private async void RunAllButton_Click(object sender, RoutedEventArgs e)
    {
        await RunBatFileAsync("start_all.bat");
    }

    private async Task RunBatFileAsync(string batName)
    {
        if (string.IsNullOrWhiteSpace(_lastCreatedServerFolder) || !Directory.Exists(_lastCreatedServerFolder))
        {
            AppendLog("ERROR: No server folder found.");
            return;
        }

        _lastJavaCheck = await JavaChecker.CheckAsync();

        if (!_lastJavaCheck.IsInstalled)
        {
            AppendLog("ERROR: Java was not found. Install Java first, then try again.");

            JavaStatusText.Text = "Java was not found. Install Java first, then try again.";
            JavaStatusText.Foreground = new SolidColorBrush(Color.FromArgb(255, 248, 113, 113));

            await ShowMessageAsync("Java missing", "Java was not found. Install Java first, then try again.");
            return;
        }

        string batPath = Path.Combine(_lastCreatedServerFolder, batName);

        if (!File.Exists(batPath))
        {
            AppendLog($"ERROR: {batName} was not found.");
            return;
        }

        AppendLog($"Starting {batName}...");

        Process.Start(new ProcessStartInfo
        {
            FileName = batPath,
            WorkingDirectory = _lastCreatedServerFolder,
            UseShellExecute = true
        });
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        LogBox.Text = "";
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private void AppendLog(string text)
    {
        LogBox.Text += $"[{DateTime.Now:HH:mm:ss}] {text}\r\n";

        try
        {
            LogBox.Select(LogBox.Text.Length, 0);
        }
        catch
        {
            // Auto-scroll is not critical.
        }
    }
}