using Microsoft.Win32;
using StreamElementsToStreamerBotMigrationTool.Data;
using StreamElementsToStreamerBotMigrationTool.Managers;
using StreamElementsToStreamerBotMigrationTool.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StreamElementsToStreamerBotMigrationTool;

public partial class MainWindow: Window
{
    private static readonly string                       DefaultDeployPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "imA-SB-Widgets");
    private        readonly ObservableCollection<Widget> _widgets;
    private                 Widget?                      _selectedWidget;

    public MainWindow()
    {
        InitializeComponent();

        WidgetList.ItemsSource = _widgets = new ObservableCollection<Widget>(WidgetManager.GetAll());
    }

    #region Event Handlers

    private void NewWidget_Click(object sender, RoutedEventArgs e)
    {
        string name = $"Widget {_widgets.Count + 1}";
        var widget = new Widget(name, DefaultDeployPath);

        _widgets.Add(widget);
        SelectWidget(widget);
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
                SelectWidget(_widgets.First());
            }
            else
            {
                _selectedWidget      = null;
                FileList.ItemsSource = null;
                ClearDetail();
            }
        }
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
            Filter      = "Widget files (*.html;*.js;*.css;*.json)|*.html;*.js;*.css;*.json|All files (*.*)|*.*"
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
        if (string.IsNullOrEmpty(DeployPathBox.Text))
            return;

        Clipboard.SetText(DeployPathBox.Text);
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

    private void EditWidgetName_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // TODO: show inline name input in sidebar row (phase 2)
    }

    #endregion Event Handlers

    #region Helpers

    private void SelectWidget(Widget widget)
    {
        _selectedWidget            = widget;
        DeployPathBox.Text         = widget.DeployedLocation;
        FileList.ItemsSource       = widget.Files;
        SaveChangesBtn.IsEnabled   = true;
        EmptyStatePanel.Visibility = Visibility.Collapsed;
        DetailPanel.Visibility     = Visibility.Visible;

        OnFilesChanged();
        SetStatus("Import your widget files.");
    }

    private void ClearDetail()
    {
        DeployPathBox.Text         = string.Empty;
        GenerateBtn.IsEnabled      = false;
        SaveChangesBtn.IsEnabled   = false;
        EmptyStatePanel.Visibility = Visibility.Visible;
        DetailPanel.Visibility     = Visibility.Collapsed;
        HideWarning();
        SetStatus("Select or create a widget to begin.");
    }

    private void OnFilesChanged()
    {
        if (_selectedWidget is null)
            return;

        bool hasFiles = _selectedWidget.Files.Count > 0;

        FilesEmptyLabel.Visibility = hasFiles
            ? Visibility.Collapsed
            : Visibility.Visible;

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