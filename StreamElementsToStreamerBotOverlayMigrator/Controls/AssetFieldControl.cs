using StreamElementsToStreamerBotOverlayMigrator.Common;
using StreamElementsToStreamerBotOverlayMigrator.Data;
using StreamElementsToStreamerBotOverlayMigrator.Services;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StreamElementsToStreamerBotOverlayMigrator.Controls;

public class AssetFieldControl: StackPanel, IDisposable
{
    private readonly Image?                _preview;
    private readonly MediaElement?         _player;
    private readonly Button?               _playPauseButton;
    private readonly Button?               _stopButton;
    private readonly StackPanel?           _transportPanel;
    private readonly bool                  _isVideo;
    private readonly InMemoryMediaServer   _mediaServer = MediaServerHost.Instance;

    private          SoundPlayer?          _wavPlayer;
    private          MemoryStream?         _wavStream;
    private          Uri?                  _activeMediaUri;
    private          bool                  _isPlaying;

    public           StyledDropdown        Dropdown  { get; }
    public           Button                SetButton { get; }

    public event     EventHandler<string>? PlaybackError;

    public AssetFieldControl(StyledDropdown dropdown, Button setButton, WidgetFileType assetWidgetFileType)
    {
        Dropdown  = dropdown;
        SetButton = setButton;

        Children.Add(dropdown);

        if (assetWidgetFileType == WidgetFileType.ImageAsset)
        {
            _preview = new Image
            {
                Height              = 64,
                MaxWidth            = 120,
                Margin              = new Thickness(0, 8, 0, 0),
                Stretch             = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Left,
                Visibility          = Visibility.Collapsed,
                SnapsToDevicePixels = true
            };

            Children.Add(_preview);
        }
        else if (assetWidgetFileType == WidgetFileType.AudioAsset || assetWidgetFileType == WidgetFileType.VideoAsset)
        {
            _isVideo = assetWidgetFileType == WidgetFileType.VideoAsset;

            _player = new MediaElement
            {
                LoadedBehavior   = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Margin           = new Thickness(0, 8, 0, 0),
                Stretch          = Stretch.Uniform,
                ScrubbingEnabled = true,
                Volume           = 1.0,
                IsMuted          = false
            };

            if (_isVideo)
            {
                _player.Height              = 120;
                _player.MaxWidth            = 220;
                _player.HorizontalAlignment = HorizontalAlignment.Left;
                _player.Visibility          = Visibility.Collapsed;
            }
            else
            {
                _player.Width            = 1;
                _player.Height           = 1;
                _player.Opacity          = 0;
                _player.Visibility       = Visibility.Visible;
                _player.IsHitTestVisible = false;
            }

            _player.MediaEnded  += (_, _) => StopPlayback();
            _player.MediaFailed += (_, e) => HandlePlaybackFailure(e.ErrorException?.Message);

            Children.Add(_player);

            _transportPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin      = new Thickness(0, 8, 0, 0),
                Visibility  = Visibility.Collapsed
            };

            _playPauseButton = new Button
            {
                Content = "▶ Play",
                Width   = 80
            };

            _stopButton = new Button
            {
                Content   = "■ Stop",
                Width     = 70,
                Margin    = new Thickness(6, 0, 0, 0),
                IsEnabled = false
            };

            _playPauseButton.Click += (_, _) => TogglePlayback();
            _stopButton.Click      += (_, _) => StopPlayback();

            _transportPanel.Children.Add(_playPauseButton);
            _transportPanel.Children.Add(_stopButton);

            Children.Add(_transportPanel);
        }

        setButton.Margin              = new Thickness(0, 8, 0, 0);
        setButton.HorizontalAlignment = HorizontalAlignment.Left;

        Children.Add(setButton);
    }

    public void LoadAsset(WidgetFile? widgetFile)
    {
        StopPlayback();
        ReleaseWavPlayer();
        ReleaseActiveMediaUri();

        if (_preview is not null)
            LoadImagePreview(widgetFile);

        if (_player is not null)
            LoadMediaAsset(widgetFile);
    }

    private void LoadImagePreview(WidgetFile? widgetFile)
    {
        if (widgetFile is null || string.IsNullOrEmpty(widgetFile.Content))
        {
            _preview!.Source    = null;
            _preview.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            byte[] imageBytes = Convert.FromBase64String(widgetFile.Content);

            var bitmap = new BitmapImage();
            using (var memoryStream = new MemoryStream(imageBytes))
            {
                bitmap.BeginInit();
                bitmap.CacheOption  = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = memoryStream;
                bitmap.EndInit();
            }
            bitmap.Freeze();

            _preview!.Source    = bitmap;
            _preview.Visibility = Visibility.Visible;
        }
        catch
        {
            _preview!.Source     = null;
            _preview.Visibility = Visibility.Collapsed;
        }
    }

    private void LoadMediaAsset(WidgetFile? widgetFile)
    {
        if (widgetFile is null || string.IsNullOrEmpty(widgetFile.Content))
        {
            if (_isVideo)
                _player!.Visibility = Visibility.Collapsed;

            if (_transportPanel is not null)
                _transportPanel.Visibility = Visibility.Collapsed;

            return;
        }

        string extension    = Path.GetExtension(widgetFile.FileName);
        bool   useWavPlayer = !_isVideo && extension.Equals(".wav", StringComparison.OrdinalIgnoreCase);

        try
        {
            byte[] fileBytes = Convert.FromBase64String(widgetFile.Content);

            if (useWavPlayer)
            {
                var memoryStream = new MemoryStream(fileBytes);
                var soundPlayer  = new SoundPlayer(memoryStream);
                soundPlayer.Load();

                _wavStream = memoryStream;
                _wavPlayer = soundPlayer;
            }
            else
            {
                Uri mediaUri = _mediaServer.Register(fileBytes, extension);
                _activeMediaUri = mediaUri;
                _player!.Source = mediaUri;
            }

            if (_isVideo)
                _player!.Visibility = Visibility.Visible;

            if (_transportPanel is not null)
                _transportPanel.Visibility = Visibility.Visible;
        }
        catch (Exception exception)
        {
            HandlePlaybackFailure(exception.Message);

            if (_isVideo)
                _player!.Visibility = Visibility.Collapsed;

            if (_transportPanel is not null)
                _transportPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void TogglePlayback()
    {
        if (_wavPlayer is not null)
        {
            if (_isPlaying)
                return;

            _wavPlayer.Play();
            _isPlaying = true;

            if (_playPauseButton is not null)
            {
                _playPauseButton.Content   = "▶ Playing…";
                _playPauseButton.IsEnabled = false;
            }

            if (_stopButton is not null)
                _stopButton.IsEnabled = true;

            return;
        }

        if (_player is null || _player.Source is null || _playPauseButton is null)
            return;

        if (_isPlaying)
        {
            _player.Pause();
            _playPauseButton.Content = "▶ Play";
            _isPlaying               = false;
        }
        else
        {
            _player.Play();
            _playPauseButton.Content = "⏸ Pause";
            _isPlaying               = true;

            if (_stopButton is not null)
                _stopButton.IsEnabled = true;
        }
    }

    private void StopPlayback()
    {
        if (_wavPlayer is not null)
        {
            _wavPlayer.Stop();
            _isPlaying = false;

            if (_playPauseButton is not null)
            {
                _playPauseButton.Content   = "▶ Play";
                _playPauseButton.IsEnabled = true;
            }

            if (_stopButton is not null)
                _stopButton.IsEnabled = false;

            return;
        }

        if (_player is null)
            return;

        _player.Stop();
        _isPlaying = false;

        if (_playPauseButton is not null)
            _playPauseButton.Content = "▶ Play";

        if (_stopButton is not null)
            _stopButton.IsEnabled = false;
    }

    private void HandlePlaybackFailure(string? message)
    {
        StopPlayback();
        PlaybackError?.Invoke(this, message ?? "unknown media error");
    }

    private void ReleaseWavPlayer()
    {
        if (_wavPlayer is null)
            return;

        SoundPlayer   wavPlayer = _wavPlayer;
        MemoryStream? wavStream = _wavStream;
        _wavPlayer = null;
        _wavStream = null;

        try
        {
            wavPlayer.Stop();
            wavPlayer.Dispose();
            wavStream?.Dispose();
        }
        catch
        {}
    }

    private void ReleaseActiveMediaUri()
    {
        if (_player is not null)
            _player.Source = null;

        if (_activeMediaUri is null)
            return;

        Uri mediaUri = _activeMediaUri;
        _activeMediaUri = null;

        _mediaServer.Unregister(mediaUri);
    }

    public void Dispose()
    {
        StopPlayback();
        ReleaseWavPlayer();
        ReleaseActiveMediaUri();
        GC.SuppressFinalize(this);
    }
}