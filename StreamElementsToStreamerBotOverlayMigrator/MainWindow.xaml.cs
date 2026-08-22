using Microsoft.Win32;
using StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.Managers;
using StreamElementsToStreamerBotOverlayMigrator.Services;
using StreamElementsToStreamerBotOverlayMigrator.Themes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StreamElementsToStreamerBotOverlayMigrator;

public partial class MainWindow: Window
{
    private static readonly string                       DefaultRootFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "imA-SB-Widgets");
    private        readonly ObservableCollection<Widget> _widgets;
    private                 Widget?                      _selectedWidget;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
            SyncThemeIcon();
        };

        Version? version = GetType().Assembly.GetName().Version;
        if (version != null && VersionLabel.Content is TextBlock versionText)
            versionText.Text = $"v{version.Major}.{version.Minor}.{version.Build}";

        WidgetList.ItemsSource = _widgets = new ObservableCollection<Widget>(WidgetManager.GetAll());

        if (_widgets.Any())
            WidgetList.SelectedItem = _widgets.First();
        else
            UpdateEmptyState();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        WidgetManager.CreateDatabaseBackupIfNeeded();
        base.OnClosing(e);
    }

    #region UI Elements

    private void CommitWidgetRename(TextBox textBox)
    {
        if (textBox.DataContext is not Widget widget)
            return;

        string newName = textBox.Text.Trim();

        if (string.IsNullOrEmpty(newName) || widget.Name == newName)
        {
            CancelWidgetRename(textBox);
            return;
        }

        widget.Name = newName;
        WidgetManager.Save(widget);

        if (_selectedWidget == widget)
            FolderPathBox.Text = widget.RootFolderLocation;

        SetStatus($"Renamed to '{newName}'.");

        ExitInlineEdit(textBox);
    }

    private void CancelWidgetRename(TextBox textBox)
        => ExitInlineEdit(textBox);

    private static void ExitInlineEdit(TextBox textBox)
    {
        StackPanel? nameDisplay = textBox.FindVisualSibling<StackPanel>("NameDisplayPanel");

        if (nameDisplay != null)
            nameDisplay.Visibility = Visibility.Visible;

        textBox.Visibility = Visibility.Collapsed;
    }

    #endregion UI Elements

    #region Event Handlers

    private void WidgetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WidgetList.SelectedItem is Widget widget)
            SelectWidget(widget);
    }

    private void NewWidget_Click(object sender, RoutedEventArgs e)
    {
        int maxNumber = _widgets
            .Select(widget => Regex.Match(widget.Name, @"^Widget (\d+)$", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)))
            .Where(match => match.Success)
            .Select(match => int.Parse(match.Groups[1].Value))
            .DefaultIfEmpty(0)
            .Max();

        var widget = new Widget($"Widget {maxNumber + 1}", DefaultRootFolderPath);

        _widgets.Add(widget);
        WidgetList.SelectedItem = widget;
        UpdateEmptyState();
    }

    private void RemoveWidget_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Widget widget)
            return;

        MessageBoxResult messageBoxResult = MessageBox.Show
        (
            $"Are you sure you want to delete '{widget.Name}'? This action cannot be undone.",
            "Confirm Deletion",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning
        );

        if (messageBoxResult != MessageBoxResult.OK)
            return;

        _widgets.Remove(widget);
        WidgetManager.Delete(widget);

        if (_selectedWidget == widget)
        {
            if (_widgets.Any())
            {
                WidgetList.SelectedItem = _widgets.First();
            }
            else
            {
                _selectedWidget      = null;
                FileList.ItemsSource = null;
                ClearDetail();
            }
        }

        UpdateEmptyState();
    }

    private void EditWidgetName_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement frameworkElement)
            return;

        DependencyObject? container = frameworkElement.FindAncestorWithChildren();

        if (container == null)
            return;

        StackPanel? nameDisplay = container.FindVisualChild<StackPanel>("NameDisplayPanel");
        TextBox?    nameEdit    = container.FindVisualChild<TextBox>("NameEditBox");

        if (nameDisplay == null || nameEdit == null || frameworkElement.DataContext is not Widget widget)
            return;

        nameDisplay.Visibility = Visibility.Collapsed;
        nameEdit.Visibility    = Visibility.Visible;
        nameEdit.Text          = widget.Name;

        nameEdit.SelectAll();
        nameEdit.Focus();

        e.Handled = true;
    }

    private void WidgetNameBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && sender is TextBox textBox)
            CommitWidgetRename(textBox);
        else if (e.Key == Key.Escape && sender is TextBox textBox2)
            CancelWidgetRename(textBox2);
    }

    private void WidgetNameBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox)
            CommitWidgetRename(textBox);
    }

    private void SaveWidget_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWidget is null)
            return;

        WidgetManager.Save(_selectedWidget);
        SetStatus("Widget saved.");
    }

    private void Import_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWidget is null)
            return;

        var dialog = new OpenFileDialog
        {
            Title = "Select widget files",
            Multiselect = true,
            Filter = Common.SupportedFileTypes.BuildImportFileDialogFilter()
        };

        if (dialog.ShowDialog() != true)
            return;

        AddWidgetFilesToFromPaths(_selectedWidget, dialog.FileNames);
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not WidgetFile file || _selectedWidget is null)
            return;

        _selectedWidget.Files.Remove(file);
        OnChanged();
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_selectedWidget?.HtmlFilePath))
            return;

        Clipboard.SetText(_selectedWidget.HtmlFilePath);
        SetStatus("Path copied to clipboard.");
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWidget is null)
            return;

        if (WidgetManager.GenerateExportFiles(_selectedWidget, out string errorMessage))
            SetStatus(errorMessage, success: true);
        else
            SetStatus(errorMessage, error: true);

        _selectedWidget.NotifyStatusChanges();
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        bool hasFiles = e.Data.GetDataPresent(DataFormats.FileDrop);

        e.Effects = hasFiles
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;

        if (hasFiles)
            SetDropZoneHighlight(true);
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
        => SetDropZoneHighlight(false);

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        SetDropZoneHighlight(false);

        if (_selectedWidget is null || e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
            return;

        AddWidgetFilesToFromPaths(_selectedWidget, paths);
    }

    private void SetDropZoneHighlight(bool active)
    {
        if (FilesEmptyLabel.FindName("DropZoneDash") is not System.Windows.Shapes.Rectangle dash)
            return;

        dash.Stroke = active ? AppColors.DropZoneBorderActive : AppColors.DropZoneBorderIdle;
        dash.Fill   = active ? AppColors.DropZoneFillActive   : AppColors.DropZoneFillIdle;
    }

    private void Configure_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedWidget is null)
            return;

        var configWindow = new WidgetConfigWindow(_selectedWidget)
        {
            Owner = this
        };
        configWindow.ShowDialog();
    }

    private void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        ThemeManager.Toggle();

        var newWindow = new MainWindow();
        newWindow.Show();
        Close();
    }

    private void SyncThemeIcon()
    {
        ThemeToggleIcon.Text    = ThemeManager.Current == Theme.Dark ? "☀" : "☾";
        ThemeToggleBtn.ToolTip  = ThemeManager.Current == Theme.Dark
            ? "Switch to light mode"
            : "Switch to dark mode";
    }

    private async void VersionLabel_Click(object sender, RoutedEventArgs e)
    {
        VersionLabel.IsEnabled = false;

        await UpdaterService.CheckAndUpdateToLatestAsync();

        VersionLabel.IsEnabled = true;
    }

    #endregion Event Handlers

    #region Helpers

    private void SelectWidget(Widget widget)
    {
        _selectedWidget      = widget;
        FolderPathBox.Text   = widget.FolderLocation;
        FileList.ItemsSource = widget.Files;

        if (WidgetList.SelectedItem != widget)
            WidgetList.SelectedItem = widget;

        EmptyStatePanel.Visibility    = Visibility.Collapsed;
        DetailContentPanel.Visibility = Visibility.Visible;
        SaveChangesBtn.IsEnabled      = true;

        OnChanged();
    }

    private void AddWidgetFilesToFromPaths(Widget widget, IEnumerable<string> paths)
    {
        foreach (WidgetFile widgetFile in WidgetFileImportAndExportService.FetchWidgetFiles(paths))
        {
            if (widget.Files.Any(file => file.FileName == widgetFile.FileName))
                continue;

            widget.AddWidgetFile(widgetFile);
        }

        OnChanged();
    }

    private void UpdateEmptyState()
    {
        bool noWidgets = !_widgets.Any();

        EmptyStatePanel.Visibility    = noWidgets
            ? Visibility.Visible
            : Visibility.Collapsed;
        DetailContentPanel.Visibility = noWidgets
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void ClearDetail()
    {
        FolderPathBox.Text       = string.Empty;
        GenerateBtn.IsEnabled    = false;
        SaveChangesBtn.IsEnabled = false;
        HideWarning();
        SetStatus("Select or create a widget to begin.");
        UpdateEmptyState();
    }

    private void OnChanged()
    {
        if (_selectedWidget is null)
            return;

        bool hasFiles = _selectedWidget.Files.Any();

        FilesEmptyLabel.Visibility = hasFiles
            ? Visibility.Collapsed
            : Visibility.Visible;

        FileListBorder.BorderThickness = hasFiles
            ? new Thickness(1)
            : new Thickness(0);
        FileListBorder.Background = hasFiles
            ? AppColors.FileListBgFilled
            : AppColors.FileListBgEmpty;

        ConfigureBtn.IsEnabled = _selectedWidget
            .Files
            .Any(file => file.WidgetFileType == Common.WidgetFileType.FieldJson);
        SaveChangesBtn.IsEnabled = _selectedWidget.IsDirty;

        _selectedWidget.NotifyStatusChanges();

        if (!hasFiles)
        {
            HideWarning();
            GenerateBtn.IsEnabled = false;
            SetStatus("Import your widget files to begin.");
        }
        else if (!_selectedWidget.Files.CheckIsValidFileSet(out string warning))
        {
            ShowWarning(warning);
            GenerateBtn.IsEnabled = false;
        }
        else
        {
            HideWarning();
            GenerateBtn.IsEnabled = true;
            SetStatus
            (
                _selectedWidget.IsGenerated
                    ? $"Last generated: {_selectedWidget.FolderLocation.GetLatestFileTimestamp()}"
                    : "Ready to generate."
            );
        }
    }

    private void ShowWarning(string message)
    {
        WarningText.Text         = message;
        WarningBanner.Visibility = Visibility.Visible;
    }

    private void HideWarning()
        => WarningBanner.Visibility = Visibility.Collapsed;

    private void SetStatus(string message, bool error = false, bool success = false)
    {
        StatusText.Text       = message;
        StatusText.Foreground = error
            ? AppColors.StatusError
            : success
                ? AppColors.StatusSuccess
                : AppColors.StatusDefault;
    }

    #endregion Helpers
}