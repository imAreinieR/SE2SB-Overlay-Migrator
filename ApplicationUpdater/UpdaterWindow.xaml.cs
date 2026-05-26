using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Windows;

namespace ApplicationUpdater;

public partial class UpdaterWindow: Window
{
    private readonly string                  _downloadUrl;
    private readonly string                  _targetPath;
    private readonly CancellationTokenSource _cts = new();

    public UpdaterWindow(string downloadUrl, string targetPath)
    {
        _downloadUrl = downloadUrl;
        _targetPath  = targetPath;
        InitializeComponent();
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await RunUpdateAsync(_cts.Token);
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        _cts.Cancel();
        base.OnClosing(e);
    }

    private async Task RunUpdateAsync(CancellationToken cancellationToken)
    {
        string tempPath = _targetPath + ".new.exe";

        try
        {
            SetStatus("Waiting for app to close...", 0);
            await Task.Delay(1500, cancellationToken);

            SetStatus("Downloading update...", 0);

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            client
                .DefaultRequestHeaders
                .UserAgent
                .Add(new ProductInfoHeaderValue("Application Updater", "1.0"));

            using var response = await client.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? -1L;
            byte[] buffer   = new byte[8192];
            long received   = 0L;

            await using var stream     = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = File.Create(tempPath);

            using var sha256 = SHA256.Create();
            using var cryptoStream = new CryptoStream(fileStream, sha256, CryptoStreamMode.Write);

            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await cryptoStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                received += read;

                if (totalBytes > 0)
                {
                    int pct = (int) (received * 100 / totalBytes);
                    SetStatus($"Downloading... {pct}%", pct);
                }
            }

            await cryptoStream.FlushFinalBlockAsync(cancellationToken);
            string actualHash = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
            Debug.WriteLine($"[Updater] Downloaded SHA-256: {actualHash}");

            // TODO: publish a .sha256 file alongside each release and download and compare it here before calling File.Move.

            SetStatus("Installing update...", 100);
            await Task.Delay(300, cancellationToken);

            fileStream.Close();
            File.Move(tempPath, _targetPath, overwrite: true);

            SetStatus("Restarting...", 100);
            Process.Start(new ProcessStartInfo(_targetPath)
            {
                UseShellExecute = true
            });

            Application.Current.Shutdown();
        }
        catch (OperationCanceledException)
        {
            Application.Current.Shutdown();
        }
        catch (Exception exception)
        {
            MessageBox.Show
            (
                $"Update failed:\n\n{exception.Message}\n\nPlease download the latest version manually from GitHub.",
                "Update Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            Application.Current.Shutdown();
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"[Updater] Failed to delete temp file: {exception.Message}");
                }
            }
        }
    }

    private void SetStatus(string message, int progress)
    {
        void Apply()
        {
            StatusText.Text   = message;
            ProgressBar.Value = Math.Clamp(progress, 0, 100);
        }

        if (Dispatcher.CheckAccess())
            Apply();
        else
            Dispatcher.Invoke(Apply);
    }
}