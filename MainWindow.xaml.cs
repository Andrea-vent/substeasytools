using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace PathMapper;

public partial class MainWindow
{
    public ObservableCollection<DriveMapping> Mappings { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        MappingsList.ItemsSource = Mappings;
        Mappings.CollectionChanged += (_, _) => UpdateEmptyState();
        RefreshAvailableLetters();
        UpdateEmptyState();
        Closing += OnClosing;
    }

    private void RefreshAvailableLetters()
    {
        var used = DriveInfo.GetDrives()
            .Select(d => char.ToUpperInvariant(d.Name[0]))
            .Concat(Mappings.Select(m => m.Letter[0]))
            .ToHashSet();

        var available = Enumerable.Range('A', 26)
            .Select(i => (char)i)
            .Where(c => !used.Contains(c))
            .Reverse()
            .Select(c => c + ":")
            .ToList();

        var previous = LetterCombo.SelectedItem as string;
        LetterCombo.ItemsSource = available;
        if (previous != null && available.Contains(previous))
            LetterCombo.SelectedItem = previous;
        else if (available.Count > 0)
            LetterCombo.SelectedIndex = 0;
    }

    private void UpdateEmptyState()
    {
        EmptyState.Visibility = Mappings.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog
        {
            Title = "Seleziona la cartella da mappare",
            Multiselect = false
        };
        if (dlg.ShowDialog() == true)
            PathBox.Text = dlg.FolderName;
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        var path = (PathBox.Text ?? string.Empty).Trim().TrimEnd('\\', '/');
        if (string.IsNullOrWhiteSpace(path))
        {
            ShowError("Inserisci un percorso prima di creare il collegamento.");
            return;
        }

        if (LetterCombo.SelectedItem is not string letter)
        {
            ShowError("Nessuna lettera disponibile.");
            return;
        }

        var isUnc = path.StartsWith(@"\\", StringComparison.Ordinal);
        if (!isUnc && !Directory.Exists(path))
        {
            ShowError("La cartella indicata non esiste.");
            return;
        }

        var (cmd, args) = isUnc
            ? ("net", $"use {letter} \"{path}\" /persistent:no")
            : ("cmd", $"/c subst {letter} \"{path}\"");

        var (ok, err) = Run(cmd, args);
        if (!ok)
        {
            ShowError($"Impossibile creare il collegamento.\n\n{err}");
            return;
        }

        Mappings.Add(new DriveMapping(letter, path, isUnc));
        PathBox.Text = string.Empty;
        RefreshAvailableLetters();
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not DriveMapping m) return;

        if (!RemoveMapping(m, out var err))
        {
            ShowError($"Impossibile eliminare il collegamento {m.Letter}\n\n{err}");
            return;
        }

        Mappings.Remove(m);
        RefreshAvailableLetters();
    }

    private static bool RemoveMapping(DriveMapping m, out string err)
    {
        var (cmd, args) = m.IsNetwork
            ? ("net", $"use {m.Letter} /delete /yes")
            : ("cmd", $"/c subst {m.Letter} /D");
        var (ok, e) = Run(cmd, args);
        err = e;
        return ok;
    }

    private static (bool ok, string err) Run(string cmd, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(cmd, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var p = Process.Start(psi)!;
            var stdout = p.StandardOutput.ReadToEnd();
            var stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();
            if (p.ExitCode == 0) return (true, string.Empty);
            var msg = string.IsNullOrWhiteSpace(stderr) ? stdout.Trim() : stderr.Trim();
            return (false, string.IsNullOrWhiteSpace(msg) ? $"Codice di uscita {p.ExitCode}" : msg);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (CleanupCheckbox.IsChecked != true) return;
        foreach (var m in Mappings.ToList())
            RemoveMapping(m, out _);
    }

    private void ShowError(string msg)
    {
        MessageBox.Show(this, msg, "substEasyTools", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}

public record DriveMapping(string Letter, string Path, bool IsNetwork)
{
    public string TypeLabel => IsNetwork ? "Connessione di rete (net use)" : "Sostituzione locale (subst)";
}
