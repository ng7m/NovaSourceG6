using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace NovaSourceG6Config;

public enum ApplicationTheme
{
    System,
    Light,
    Dark
}

public sealed class ThemeService
{
    private readonly string preferencePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "NovaSourceG6Config", "theme.txt");

    public ApplicationTheme Current { get; private set; } = ApplicationTheme.System;

    public void LoadAndApply()
    {
        try
        {
            if (Enum.TryParse<ApplicationTheme>(File.ReadAllText(preferencePath), true, out var saved)) Current = saved;
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        Apply();
    }

    public void Select(ApplicationTheme theme)
    {
        Current = theme;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(preferencePath)!);
            File.WriteAllText(preferencePath, theme.ToString());
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        Apply();
    }

    public void RefreshSystemTheme()
    {
        if (Current == ApplicationTheme.System) Apply();
    }

    private void Apply()
    {
        var dark = Current == ApplicationTheme.Dark || Current == ApplicationTheme.System && IsWindowsDarkMode();
        Set("AppBackgroundBrush", dark ? "#111827" : "#F4F6F8");
        Set("SurfaceBrush", dark ? "#1F2937" : "#FFFFFF");
        Set("ControlBrush", dark ? "#374151" : "#FFFFFF");
        Set("TextBrush", dark ? "#F9FAFB" : "#111827");
        Set("SecondaryTextBrush", dark ? "#D1D5DB" : "#4B5563");
        Set("BorderBrush", dark ? "#4B5563" : "#D1D5DB");
        Set("WarningBrush", dark ? "#FBBF24" : "#B45309");
        Set("HoverBrush", dark ? "#4B5563" : "#E5E7EB");
        Set("SelectionBrush", dark ? "#1D4ED8" : "#BFDBFE");
    }

    private static bool IsWindowsDarkMode()
    {
        try
        {
            var value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
            return value is int setting && setting == 0;
        }
        catch { return false; }
    }

    private static void Set(string key, string color) =>
        Application.Current.Resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
}
