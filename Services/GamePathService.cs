using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MdModManager.Services;

public interface IGamePathService
{
    string? DetectGamePath();
    bool IsValidGamePath(string path);
}

public class GamePathService : IGamePathService
{
    private const string GameFolderName = "Muse Dash";
    private const string ExeName = "MuseDash.exe";
    private const string AssemblyName = "GameAssembly.dll";

    public string? DetectGamePath()
    {
        // 1. Check common default paths
        var commonPaths = new[]
        {
            $@"C:\Program Files (x86)\Steam\steamapps\common\{GameFolderName}",
            $@"C:\Program Files\Steam\steamapps\common\{GameFolderName}"
        };

        foreach (var path in commonPaths)
        {
            if (IsValidGamePath(path))
            {
                return path;
            }
        }

        // 2. Check Steam Registry
        var steamPath = GetSteamPathFromRegistry();
        if (string.IsNullOrEmpty(steamPath))
        {
            return null;
        }

        var defaultLibraryPath = Path.Combine(steamPath, "steamapps", "common", GameFolderName);
        if (IsValidGamePath(defaultLibraryPath))
        {
            return defaultLibraryPath;
        }

        // 3. Parse libraryfolders.vdf
        var vdfPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdfPath))
        {
            var libraryPaths = ParseLibraryFolders(vdfPath);
            foreach (var libPath in libraryPaths)
            {
                var gamePath = Path.Combine(libPath, "steamapps", "common", GameFolderName);
                if (IsValidGamePath(gamePath))
                {
                    return gamePath;
                }
            }
        }

        return null;
    }

    public bool IsValidGamePath(string path)
    {
        if (string.IsNullOrEmpty(path)|| !Directory.Exists(path))
        {
            return false;
        }

        var exePath = Path.Combine(path, ExeName);
        var assemblyPath = Path.Combine(path, AssemblyName);

        return File.Exists(exePath) && File.Exists(assemblyPath);
    }

    private string? GetSteamPathFromRegistry()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            return key?.GetValue("InstallPath")?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private List<string> ParseLibraryFolders(string vdfPath)
    {
        var paths = new List<string>();
        try
        {
            var lines = File.ReadAllLines(vdfPath);
            var pathRegex = new Regex(@"""path""\s+""([^""]+)""");

            foreach (var line in lines)
            {
                var match = pathRegex.Match(line);
                if (match.Success)
                {
                    // VDF paths have double backslashes which need to be unescaped
                    var directoryPath = match.Groups[1].Value.Replace(@"\\", @"\");
                    paths.Add(directoryPath);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing VDF: {ex.Message}");
        }

        return paths;
    }
}
