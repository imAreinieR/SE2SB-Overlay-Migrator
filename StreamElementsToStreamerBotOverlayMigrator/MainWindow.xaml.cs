using Microsoft.Win32;
using StreamElementsToStreamerBotOverlayMigrator.Common.ExtensionMethods;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.Managers;
using StreamElementsToStreamerBotOverlayMigrator.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StreamElementsToStreamerBotOverlayMigrator;

public partial class MainWindow: Window
{
    private static readonly string                       DefaultDeployPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "imA-SB-Widgets");
    private        readonly ObservableCollection<Widget> _widgets;
    private                 Widget?                      _selectedWidget;

    public MainWindow()
    {
        InitializeComponent();

        WidgetList.ItemsSource = _widgets = new ObservableCollection<Widget>(WidgetManager.GetAll());

        if (_widgets.Any())
            WidgetList.SelectedItem = _widgets.First();
        else
            UpdateEmptyState();
    }

    #region Event Handlers

    private void WidgetList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WidgetList.SelectedItem is Widget widget)
            SelectWidget(widget);
    }

    private void NewWidget_Click(object sender, RoutedEventArgs e)
    {
        string name = $"Widget {_widgets.Count + 1}";
        var widget = new Widget(name, DefaultDeployPath);

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

    private void CommitWidgetRename(TextBox textBox)
    {
        string newName = textBox.Text.Trim();

        if (string.IsNullOrEmpty(newName))
        {
            CancelWidgetRename(textBox);
            return;
        }

        if (textBox.DataContext is Widget widget)
        {
            widget.Name = newName;
            WidgetManager.Save(widget);

            if (_selectedWidget == widget)
                DeployPathBox.Text = widget.FolderLocation;

            SetStatus($"Renamed to '{newName}'.");
        }

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
            Title       = "Select widget files",
            Multiselect = true,
            Filter      = "Widget files (*.html;*.js;*.css;*.json;*.zip)|*.html;*.js;*.css;*.json;*.zip|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
            return;

        foreach (WidgetFile widgetFile in WidgetFileImportAndExportService.FetchWidgetFiles(dialog.FileNames))
        {
            if (_selectedWidget.Files.Any(file => file.FileName == widgetFile.FileName))
                continue;

            _selectedWidget.Files.Add(widgetFile);
        }

        OnFilesChanged();
    }

    private void RemoveFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not WidgetFile file || _selectedWidget is null)
            return;

        _selectedWidget.Files.Remove(file);
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

        foreach (WidgetFile widgetFile in WidgetFileImportAndExportService.FetchWidgetFiles(paths))
        {
            if (_selectedWidget.Files.Any(file => file.FileName == widgetFile.FileName))
                continue;

            _selectedWidget.Files.Add(widgetFile);
        }

        OnFilesChanged();
    }

    private void SetDropZoneHighlight(bool active)
    {
        if (FilesEmptyLabel.FindName("DropZoneDash") is not System.Windows.Shapes.Rectangle dash)
            return;

        dash.Stroke = new SolidColorBrush
        (
            active
                ? Color.FromRgb(0x4F, 0x7E, 0xFF)
                : Color.FromRgb(0x3D, 0x4A, 0x6E)
        );
        dash.Fill = new SolidColorBrush
        (
            active
                ? Color.FromArgb(0x1A, 0x4F, 0x7E, 0xFF)
                : Colors.Transparent
        );
    }

    #endregion Event Handlers

    #region Helpers

    private void SelectWidget(Widget widget)
    {
        _selectedWidget      = widget;
        DeployPathBox.Text   = widget.DeployedLocation;
        FileList.ItemsSource = widget.Files;

        if (WidgetList.SelectedItem != widget)
            WidgetList.SelectedItem = widget;

        EmptyStatePanel.Visibility    = Visibility.Collapsed;
        DetailContentPanel.Visibility = Visibility.Visible;
        SaveChangesBtn.IsEnabled      = true;

        OnFilesChanged();
        SetStatus("Import your widget files.");
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
        DeployPathBox.Text       = string.Empty;
        GenerateBtn.IsEnabled    = false;
        SaveChangesBtn.IsEnabled = false;
        HideWarning();
        SetStatus("Select or create a widget to begin.");
        UpdateEmptyState();
    }

    private void OnFilesChanged()
    {
        if (_selectedWidget is null)
            return;

        bool hasFiles = _selectedWidget.Files.Count > 0;

        FilesEmptyLabel.Visibility = hasFiles
            ? Visibility.Collapsed
            : Visibility.Visible;

        FileListBorder.BorderThickness = hasFiles
            ? new Thickness(1)
            : new Thickness(0);
        FileListBorder.Background = new SolidColorBrush
        (
            hasFiles
                ? Color.FromRgb(0x1A, 0x1E, 0x2B)
                : Colors.Transparent
        );

        if (!hasFiles)
        {
            HideWarning();
            GenerateBtn.IsEnabled = false;
            SetStatus("Import your widget files to begin.");
            return;
        }

        if (!_selectedWidget.Files.CheckIsValidFileSet(out string warning))
        {
            ShowWarning(warning);
            GenerateBtn.IsEnabled = false;
        }
        else
        {
            HideWarning();
            GenerateBtn.IsEnabled = true;
            SetStatus("Ready to generate.");
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
        StatusText.Foreground = new SolidColorBrush
        (
            error
                ? Color.FromRgb(0xFF, 0x6B, 0x6B)
                : success
                    ? Color.FromRgb(0x3E, 0xCF, 0x8E)
                    : Color.FromRgb(0x58, 0x60, 0x80)
        );
    }

    #endregion Helpers
}