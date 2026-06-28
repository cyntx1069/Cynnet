# Cynnet

<p align="center">
  <img src="assets/app-preview.png" alt="Cynnet app preview" width="900">
</p>

<p align="center">
  <b>A clean WinUI Minecraft server maker for Windows.</b>
</p>

<p align="center">
  <img alt="Version" src="https://img.shields.io/badge/version-1.0-purple">
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows-blue">
  <img alt="Built with" src="https://img.shields.io/badge/built%20with-WinUI%203-7A3CFF">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0-512BD4">
</p>

---

## About

**Cynnet** is a Windows app that helps you create a Minecraft server without doing every setup step manually.

The app asks for the server settings, creates the folder structure, downloads the server `.jar`, generates the needed files, and gives you simple buttons to open or run the server.

Cynnet is made for people who want a faster and cleaner way to set up a Minecraft server.

---

## Features

- Create Minecraft server files automatically
- Supports **Vanilla**, **Paper**, and **Fabric**
- Minecraft version dropdown
- RAM slider
- Server name, max players, port, account mode, and EULA settings
- Java version check
- Plugin and mod sections
- Optional `playit.gg` starter file
- Clean dark WinUI interface
- Output console inside the app
- Open server folder directly from the app
- Run the server from the app

---

## Supported Server Types

| Server Type | Status |
|---|---|
| Vanilla | Supported |
| Paper | Supported |
| Fabric | Supported |
| Spigot | Coming soon |
| Bukkit | Coming soon |
| Forge | Coming soon |

---

## Requirements

- Windows 10/11
- Java 21 or newer for modern Minecraft versions
- Visual Studio 2022 if building from source
- .NET 8
- Windows App SDK / WinUI 3 project setup

`playit.gg` is optional. If you want to use it, place `playit.exe` in the generated server folder and enable the playit.gg starter option.

---

## How To Use

1. Open Cynnet.
2. Choose a server type.
3. Pick a Minecraft version.
4. Enter your server name, max players, RAM, and port.
5. Choose whether the server should use premium account mode.
6. Accept the Minecraft EULA if you want the server to start immediately.
7. Choose optional plugins, mods, or playit.gg starter.
8. Click **Create Server**.
9. Use **Open Folder** or **Run Server**.

---

## Building From Source

1. Install Visual Studio 2022.
2. Install the required workloads for WinUI / desktop development.
3. Clone this repository:

```bash
git clone https://github.com/YOUR_USERNAME/Cynnet.git
```

4. Open the solution in Visual Studio.
5. Build and run the project.

---

## Project Status

Cynnet is currently at **version 1.0**.

The first release focuses on making the basic Minecraft server creation process simple, clean, and fast.

Planned future ideas:

- Better plugin installer
- Mod installer
- More server type support
- Custom app themes
- Saved profiles
- Automatic `server.properties` editor
- Better release/build system

---

## Credits

Made by **Cynnet**.

Minecraft is owned by Mojang/Microsoft.  
Cynnet is not affiliated with Mojang, Microsoft, PaperMC, Fabric, or playit.gg.
