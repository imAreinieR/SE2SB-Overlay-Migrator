using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Windows;

namespace ApplicationUpdater;

public partial class UpdaterWindow: Window
{
    private readonly string                  _downloadUrl;
    private readonly string                  _targetPath;
    private readonly string?                 _checksumUrl;
    private readonly CancellationTokenSource _cts = new();

    public UpdaterWindow(string downloadUrl, string targetPath, string? checksumUrl)
    {
        _downloadUrl = downloadUrl;
        _targetPath  = targetPath;
        _checksumUrl = checksumUrl;
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
        string tempZipPath = _targetPath + ".new.zip";

        try
        {
            SetStatus("Waiting for app to close...", 0);
            await Task.Delay(1500, cancellationToken);

            SetStatus("Downloading update...", 10);

            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            client
                .DefaultRequestHeaders
                .UserAgent
                .Add(new ProductInfoHeaderValue("Application Updater", "1.0"));

            using var response = await client.GetAsync(_downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long   totalBytes = response.Content.Headers.ContentLength ?? -1L;
            byte[] buffer     = new byte[8192];
            long   received   = 0L;

            await using var stream     = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var fileStream = File.Create(tempZipPath);

            using var sha256       = SHA256.Create();
            using var cryptoStream = new CryptoStream(fileStream, sha256, CryptoStreamMode.Write);

            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await cryptoStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                received += read;

                if (totalBytes > 0)
                {
                    decimal percentDownloaded = received / totalBytes;
                    SetStatus($"Downloading... {(int) (percentDownloaded * 100)}%", 10 + (int)(percentDownloaded * 50));
                }
            }

            await cryptoStream.FlushFinalBlockAsync(cancellationToken);
            string actualHash = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
            Debug.WriteLine($"[Updater] Downloaded SHA-256: {actualHash}");

            if (_checksumUrl is not null)
            {
                SetStatus("Verifying checksum...", 80);

                string checksumContent = await client.GetStringAsync(_checksumUrl, cancellationToken);
                string expectedHash    = checksumContent.Split(' ')[0].Trim().ToLowerInvariant();

                if (actualHash != expectedHash)
                    throw new Exception("Checksum mismatch — the download may be corrupted or tampered with.");
            }
            else
            {
                Debug.WriteLine("[Updater] No checksum URL provided, skipping verification.");
            }

            SetStatus("Installing update...", 70);
            await Task.Delay(300, cancellationToken);

            await fileStream.DisposeAsync();

            string exeName    = Path.GetFileName(_targetPath);
            string extractDir = Path.Combine(Path.GetTempPath(), "SE2SB_Update");

            if (Directory.Exists(extractDir))
                Directory.Delete(extractDir, recursive: true);

            ZipFile.ExtractToDirectory(tempZipPath, extractDir);

            string extractedExe = Directory
                .GetFiles(extractDir, exeName, SearchOption.AllDirectories)
                .FirstOrDefault()
                    ?? throw new Exception($"Could not find '{exeName}' inside the update archive.");

            File.Move(extractedExe, _targetPath, true);

            SetStatus("Restarting...", 90);
            Process.Start(new ProcessStartInfo(_targetPath)
            {
                UseShellExecute = true
            });

            SetStatus("Complete!", 100);
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
            if (File.Exists(tempZipPath))
            {
                try
                {
                    File.Delete(tempZipPath);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"[Updater] Failed to delete temp zip: {exception.Message}");
                }
            }

            string extractDir = Path.Combine(Path.GetTempPath(), "SE2SB_Update");
            if (Directory.Exists(extractDir))
            {
                try
                {
                    Directory.Delete(extractDir, true);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine($"[Updater] Failed to delete extract dir: {exception.Message}");
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