using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HardLinker;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings = SettingsStore.Load();
    private readonly ObservableCollection<string> _files = [];
    private bool _updating;
    private bool _isInitialized;
    private static readonly Dictionary<string, (string English, string Chinese)> Text = new()
    {
        ["title"] = ("HardLinker", "HardLinker"), ["subtitle"] = ("Create Windows file hard links", "快速创建 Windows 文件硬链接"),
        ["filesTab"] = ("Files", "文件"), ["foldersTab"] = ("Folders", "文件夹"), ["settingsTab"] = ("Settings", "设置"),
        ["sourceFiles"] = ("Source files (multiple selection allowed)", "源文件（可多选）"), ["addFiles"] = ("Add files…", "添加文件…"),
        ["destination"] = ("Destination folder", "目标目录"), ["choose"] = ("Choose…", "选择…"), ["rememberFolder"] = ("Remember folder", "记住此目录"),
        ["createFiles"] = ("Create file hard links", "创建文件硬链接"),
        ["sourceFolder"] = ("Source folder (top-level files only; no recursion)", "源文件夹（只处理第一层普通文件，不递归）"),
        ["parentFolder"] = ("Destination parent folder", "目标父目录"), ["folderName"] = ("New folder name (defaults to source folder name)", "新建目录名称（默认使用源文件夹名称）"),
        ["rememberName"] = ("Remember name", "记住名称"), ["folderHint"] = ("Existing files with the same name are skipped; cross-volume files are reported as failures. Subfolders are ignored.", "已存在的同名文件会跳过；跨卷文件会报告失败。子目录不会处理。"),
        ["createFolders"] = ("Create folder hard links", "创建文件夹硬链接"), ["warning"] = ("Hard links share the same file data; changes through any link affect all links.", "硬链接共享同一份文件数据；修改任一链接会影响其他链接。"),
        ["language"] = ("Language", "界面语言"), ["pickSource"] = ("Select source files", "选择源文件"), ["pickFolder"] = ("Select a folder", "选择目录"),
        ["defaultSource"] = ("Default source folder", "默认源文件夹"), ["defaultTarget"] = ("Default destination parent folder", "默认目标父目录"), ["saveDefaults"] = ("Save default folders", "保存默认目录"),
        ["defaultsSaved"] = ("Default folders saved.", "默认目录已保存。"), ["invalidDefaultSource"] = ("The default source folder does not exist. Choose an existing folder or leave it blank.", "默认源文件夹不存在，请选择一个存在的文件夹或留空。"),
        ["invalidDefaultTarget"] = ("The default destination folder does not exist. Choose an existing folder or leave it blank.", "默认目标父目录不存在，请选择一个存在的文件夹或留空。"),
        ["selected"] = ("Selected {0} file(s).", "已选择 {0} 个文件。"), ["rememberedFolder"] = ("Frequent destination folder saved.", "常用目标目录已保存。"), ["rememberedName"] = ("Frequent folder name saved.", "常用文件夹名称已保存。"),
        ["missingTarget"] = ("Choose an existing destination folder first.", "请先选择一个存在的目标目录。"), ["missingFiles"] = ("Add at least one source file first.", "请先添加至少一个源文件。"),
        ["missingSourceFolder"] = ("Choose an existing source folder first.", "请先选择一个存在的源文件夹。"), ["missingParent"] = ("Choose an existing destination parent folder first.", "请先选择一个存在的目标父目录。"),
        ["invalidName"] = ("Enter a valid single folder name.", "请输入有效的单层文件夹名称。"), ["targetExists"] = ("The destination already exists: {0}", "目标已存在：{0}"),
        ["folderCreateFailed"] = ("Could not create the destination folder: {0}", "无法创建目标文件夹：{0}"), ["crossVolume"] = ("The source file and destination are on different volumes. Windows hard links cannot cross volumes.", "源文件与目标目录不在同一卷，Windows 硬链接不能跨卷创建。"),
        ["sourceMissing"] = ("Source file not found.", "源文件不存在。"), ["existingTarget"] = ("A destination with this name already exists; it was not overwritten.", "目标名称已存在，未覆盖。"),
        ["resultTitle"] = ("Creation results", "创建结果"), ["errorTitle"] = ("Unable to continue", "无法继续"), ["allSuccess"] = ("All links were created successfully.", "全部成功。"),
        ["resultSummary"] = ("Complete: {0} hard link(s) created; {1} failed.", "完成：成功创建 {0} 个硬链接，失败 {1} 个。"), ["destinationFolder"] = ("Destination folder: {0}", "目标文件夹：{0}"),
        ["failureItem"] = ("{0}: {1}", "{0}：{1}"), ["moreFailures"] = ("{0} more failure(s).", "另有 {0} 项失败。"),
        ["error"] = ("Error", "错误")
    };

    public MainWindow()
    {
        InitializeComponent();
        FilesList.ItemsSource = _files;
        FileTargetText.Text = RestoreFolderPath(_settings.LastFileTarget, _settings.DefaultTargetFolder);
        DefaultSourceText.Text = _settings.DefaultSourceFolder;
        DefaultTargetText.Text = _settings.DefaultTargetFolder;
        SourceFolderText.Text = RestoreFolderPath(_settings.LastSourceFolder, _settings.DefaultSourceFolder);
        FolderTargetText.Text = RestoreFolderPath(_settings.LastFolderTarget, _settings.DefaultTargetFolder);
        FolderNameText.Text = _settings.LastFolderName;
        LanguageCombo.SelectedIndex = string.Equals(_settings.Language, "zh", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        ApplyLanguage();
        RefreshChoices();
        _isInitialized = true;
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var initialSourceFolder = RestoreFolderPath(_settings.LastFileSourceFolder, _settings.DefaultSourceFolder);
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            CheckFileExists = true,
            Title = T("pickSource"),
            InitialDirectory = initialSourceFolder,
            ClientGuid = CreateFileDialogClientGuid(initialSourceFolder)
        };
        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
                if (!_files.Contains(file)) _files.Add(file);
            if (dialog.FileNames.Length > 0)
            {
                _settings.LastFileSourceFolder = Path.GetDirectoryName(dialog.FileNames[0]) ?? "";
                SettingsStore.Save(_settings);
            }
        }
        StatusText.Text = string.Format(T("selected"), _files.Count);
    }

    private void ChooseFileTarget_Click(object sender, RoutedEventArgs e)
    {
        var path = PickFolder(FileTargetText.Text);
        if (path is not null) { FileTargetText.Text = path; _settings.LastFileTarget = path; SettingsStore.Save(_settings); }
    }

    private void ChooseSourceFolder_Click(object sender, RoutedEventArgs e)
    {
        var path = PickFolder(SourceFolderText.Text);
        if (path is not null)
        {
            SourceFolderText.Text = path;
            _settings.LastSourceFolder = path;
            FolderNamesCombo.SelectedIndex = -1;
            FolderNameText.Text = new DirectoryInfo(path).Name;
            _settings.LastFolderName = FolderNameText.Text;
            SettingsStore.Save(_settings);
        }
    }

    private void ChooseFolderTarget_Click(object sender, RoutedEventArgs e)
    {
        var path = PickFolder(FolderTargetText.Text);
        if (path is not null) { FolderTargetText.Text = path; _settings.LastFolderTarget = path; SettingsStore.Save(_settings); }
    }

    private void ChooseDefaultSource_Click(object sender, RoutedEventArgs e)
    {
        var path = PickFolder(DefaultSourceText.Text);
        if (path is not null) DefaultSourceText.Text = path;
    }

    private void ChooseDefaultTarget_Click(object sender, RoutedEventArgs e)
    {
        var path = PickFolder(DefaultTargetText.Text);
        if (path is not null) DefaultTargetText.Text = path;
    }

    private void SaveDefaultFolders_Click(object sender, RoutedEventArgs e)
    {
        var source = DefaultSourceText.Text.Trim();
        var target = DefaultTargetText.Text.Trim();
        if (source.Length > 0 && !Directory.Exists(source)) { ShowError(T("invalidDefaultSource")); return; }
        if (target.Length > 0 && !Directory.Exists(target)) { ShowError(T("invalidDefaultTarget")); return; }
        _settings.DefaultSourceFolder = source;
        _settings.DefaultTargetFolder = target;
        SettingsStore.Save(_settings);
        StatusText.Text = T("defaultsSaved");
    }

    private static string RestoreFolderPath(string recentPath, string defaultPath)
    {
        if (!string.IsNullOrWhiteSpace(recentPath) && Directory.Exists(recentPath)) return recentPath;
        return !string.IsNullOrWhiteSpace(defaultPath) && Directory.Exists(defaultPath) ? defaultPath : "";
    }

    private static Guid CreateFileDialogClientGuid(string initialDirectory)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(initialDirectory)
            ? "<empty>"
            : Path.TrimEndingDirectorySeparator(Path.GetFullPath(initialDirectory)).ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"HardLinker.OpenFileDialog:{normalizedPath}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    private string? PickFolder(string initialPath)
    {
        using var dialog = new FolderBrowserDialog { Description = T("pickFolder"), UseDescriptionForTitle = true, ShowNewFolderButton = true };
        if (Directory.Exists(initialPath)) dialog.InitialDirectory = initialPath;
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

    private void SaveFileTarget_Click(object sender, RoutedEventArgs e)
    {
        var path = FileTargetText.Text.Trim();
        if (!Directory.Exists(path)) { ShowError(T("missingTarget")); return; }
        _settings.LastFileTarget = path;
        AddUnique(_settings.FileTargets, path);
        SettingsStore.Save(_settings); RefreshChoices();
        StatusText.Text = T("rememberedFolder");
    }

    private void SaveFolderName_Click(object sender, RoutedEventArgs e)
    {
        var name = FolderNameText.Text.Trim();
        if (!IsValidDirectoryName(name)) { ShowError(T("invalidName")); return; }
        _settings.LastFolderName = name;
        AddUnique(_settings.FolderNames, name);
        SettingsStore.Save(_settings); RefreshChoices();
        StatusText.Text = T("rememberedName");
    }

    private void FileTarget_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updating && FileTargetsCombo.SelectedItem is string path)
        {
            FileTargetText.Text = path;
            _settings.LastFileTarget = path;
            SettingsStore.Save(_settings);
        }
    }

    private void FolderName_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updating && FolderNamesCombo.SelectedItem is string name)
        {
            FolderNameText.Text = name;
            _settings.LastFolderName = name;
            SettingsStore.Save(_settings);
        }
    }

    private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized || _updating || LanguageCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string language) return;
        _settings.Language = language;
        SettingsStore.Save(_settings);
        ApplyLanguage();
    }

    private string T(string key) => _settings.Language == "zh" ? Text[key].Chinese : Text[key].English;

    private void ApplyLanguage()
    {
        Title = T("title");
        SubtitleText.Text = T("subtitle"); FilesTab.Header = T("filesTab"); FoldersTab.Header = T("foldersTab"); SettingsTab.Header = T("settingsTab");
        SourceFilesLabel.Text = T("sourceFiles"); AddFilesButton.Content = T("addFiles"); FileTargetLabel.Text = T("destination");
        ChooseFileTargetButton.Content = T("choose"); SaveFileTargetButton.Content = T("rememberFolder"); CreateFilesButton.Content = T("createFiles");
        SourceFolderLabel.Text = T("sourceFolder"); ChooseSourceFolderButton.Content = T("choose"); FolderTargetLabel.Text = T("parentFolder");
        ChooseFolderTargetButton.Content = T("choose"); FolderNameLabel.Text = T("folderName"); SaveFolderNameButton.Content = T("rememberName");
        FolderHintText.Text = T("folderHint"); CreateFolderLinksButton.Content = T("createFolders");
        LanguageLabel.Text = T("language");
        DefaultSourceLabel.Text = T("defaultSource"); DefaultTargetLabel.Text = T("defaultTarget");
        ChooseDefaultSourceButton.Content = T("choose"); ChooseDefaultTargetButton.Content = T("choose"); SaveDefaultsButton.Content = T("saveDefaults");
        if (StatusText.Text.Contains("share the same file data", StringComparison.OrdinalIgnoreCase) || StatusText.Text.Contains("共享同一份文件数据", StringComparison.Ordinal))
            StatusText.Text = T("warning");
    }

    private void RefreshChoices()
    {
        _updating = true;
        FileTargetsCombo.ItemsSource = null;
        FileTargetsCombo.ItemsSource = _settings.FileTargets;
        FolderNamesCombo.ItemsSource = null;
        FolderNamesCombo.ItemsSource = _settings.FolderNames;
        _updating = false;
    }

    private void CreateFiles_Click(object sender, RoutedEventArgs e)
    {
        var target = FileTargetText.Text.Trim();
        if (!Directory.Exists(target)) { ShowError(T("missingTarget")); return; }
        if (_files.Count == 0) { ShowError(T("missingFiles")); return; }
        _settings.LastFileTarget = target;
        SettingsStore.Save(_settings);

        var created = 0;
        var failures = new List<string>();
        foreach (var source in _files.ToArray())
        {
            var destination = Path.Combine(target, Path.GetFileName(source));
            try
            {
                if (!File.Exists(source)) throw new FileNotFoundException(T("sourceMissing"), source);
                EnsureSameVolume(source, destination);
                HardLinkService.Create(source, destination, T("existingTarget"));
                created++;
            }
            catch (Exception ex) { failures.Add($"{Path.GetFileName(source)}：{ex.Message}"); }
        }
        ShowResult(created, failures);
    }

    private void CreateFolderLinks_Click(object sender, RoutedEventArgs e)
    {
        var source = SourceFolderText.Text.Trim();
        var parent = FolderTargetText.Text.Trim();
        if (!Directory.Exists(source)) { ShowError(T("missingSourceFolder")); return; }
        if (!Directory.Exists(parent)) { ShowError(T("missingParent")); return; }
        var name = FolderNameText.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) name = new DirectoryInfo(source).Name;
        if (!IsValidDirectoryName(name)) { ShowError(T("invalidName")); return; }
        _settings.LastSourceFolder = source;
        _settings.LastFolderTarget = parent;
        _settings.LastFolderName = name;
        SettingsStore.Save(_settings);
        var destinationFolder = Path.Combine(parent, name);
        try
        {
            if (Directory.Exists(destinationFolder) || File.Exists(destinationFolder))
            { ShowError(string.Format(T("targetExists"), destinationFolder)); return; }
            Directory.CreateDirectory(destinationFolder);
        }
        catch (Exception ex) { ShowError(string.Format(T("folderCreateFailed"), ex.Message)); return; }

        var created = 0;
        var failures = new List<string>();
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.TopDirectoryOnly))
        {
            var destination = Path.Combine(destinationFolder, Path.GetFileName(file));
            try
            {
                EnsureSameVolume(file, destination);
                HardLinkService.Create(file, destination, T("existingTarget"));
                created++;
            }
            catch (Exception ex) { failures.Add($"{Path.GetFileName(file)}：{ex.Message}"); }
        }
        ShowResult(created, failures, destinationFolder);
    }

    private void EnsureSameVolume(string source, string destination)
    {
        var sourceRoot = Path.GetPathRoot(Path.GetFullPath(source));
        var destinationRoot = Path.GetPathRoot(Path.GetFullPath(destination));
        if (!string.Equals(sourceRoot, destinationRoot, StringComparison.OrdinalIgnoreCase))
            throw new IOException(T("crossVolume"));
    }

    private static bool IsValidDirectoryName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name != "." && name != ".." &&
        name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !name.Contains(Path.DirectorySeparatorChar) && !name.Contains(Path.AltDirectorySeparatorChar);

    private static void AddUnique(List<string> list, string value)
    {
        list.RemoveAll(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
        list.Insert(0, value);
        if (list.Count > 8) list.RemoveRange(8, list.Count - 8);
    }

    private void ShowResult(int created, List<string> failures, string? destinationFolder = null)
    {
        StatusText.Text = string.Format(T("resultSummary"), created, failures.Count);
        var details = failures.Count == 0 ? T("allSuccess") : string.Join(Environment.NewLine, failures.Take(12).Select(item => string.Format(T("failureItem"), item.Split('：', 2)[0], item.Split('：', 2).Last()))) + (failures.Count > 12 ? $"{Environment.NewLine}{string.Format(T("moreFailures"), failures.Count - 12)}" : "");
        var prefix = destinationFolder is null ? "" : $"{string.Format(T("destinationFolder"), destinationFolder)}{Environment.NewLine}{Environment.NewLine}";
        System.Windows.MessageBox.Show(this, prefix + details, T("resultTitle"), MessageBoxButton.OK, failures.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    private void ShowError(string message) => System.Windows.MessageBox.Show(this, message, T("errorTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
}

internal sealed class AppSettings
{
    public AppSettings() { }

    public List<string> FileTargets { get; set; } = [];
    public List<string> FolderNames { get; set; } = [];
    public string Language { get; set; } = "en";
    public string LastFileTarget { get; set; } = "";
    public string LastFileSourceFolder { get; set; } = "";
    public string LastSourceFolder { get; set; } = "";
    public string LastFolderTarget { get; set; } = "";
    public string LastFolderName { get; set; } = "";
    public string DefaultSourceFolder { get; set; } = "";
    public string DefaultTargetFolder { get; set; } = "";
}

internal static class SettingsStore
{
    private static readonly string FilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HardLinker", "settings.json");
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static AppSettings Load()
    {
        try { return File.Exists(FilePath) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Options) ?? new() : new(); }
        catch { return new(); }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Options));
    }
}
