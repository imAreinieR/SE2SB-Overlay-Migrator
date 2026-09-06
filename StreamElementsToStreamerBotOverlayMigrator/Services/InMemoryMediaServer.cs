using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace StreamElementsToStreamerBotOverlayMigrator.Services;

internal sealed class InMemoryMediaServer: IDisposable
{
    private readonly record struct Entry(byte[] Data, string ContentType);

    private readonly HttpListener                        _listener = new();
    private readonly ConcurrentDictionary<string, Entry> _files    = new();
    private readonly int                                 _port;
    private          bool                                _disposed;

    public InMemoryMediaServer()
    {
        _port = GetFreeLoopbackPort();
        _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
        _listener.Start();

        _ = Task.Run(ServeLoopAsync);
    }

    public Uri Register(byte[] data, string fileExtension)
    {
        string id = Guid.NewGuid().ToString("N");
        _files[id] = new Entry(data, GetContentType(fileExtension));

        return new Uri($"http://127.0.0.1:{_port}/{id}{fileExtension}");
    }

    public void Unregister(Uri? uri)
    {
        if (uri is null)
            return;

        string id = GetIdFromPath(uri.LocalPath);
        _files.TryRemove(id, out _);
    }

    private async Task ServeLoopAsync()
    {
        while (!_disposed && _listener.IsListening)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception) when (_disposed || !_listener.IsListening)
            {
                break;
            }
            catch
            {
                continue;
            }

            _ = Task.Run(() => HandleRequest(context));
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        try
        {
            string id = GetIdFromPath(context.Request.Url!.LocalPath);

            if (_files.TryGetValue(id, out Entry entry))
            {
                context.Response.StatusCode      = 200;
                context.Response.ContentType     = entry.ContentType;
                context.Response.ContentLength64 = entry.Data.LongLength;

                context.Response.OutputStream.Write(entry.Data, 0, entry.Data.Length);
            }
            else
            {
                context.Response.StatusCode = 404;
            }
        }
        catch
        {
            context.Response.StatusCode = 500;
        }
        finally
        {
            try
            {
                context.Response.OutputStream.Close();
            } catch
            {}
        }
    }

    private static string GetIdFromPath(string localPath)
        => Path.GetFileNameWithoutExtension(localPath.TrimStart('/'));

    private static string GetContentType(string extension)
        => extension.ToLowerInvariant() switch
        {
            ".mp4"  => "video/mp4",
            ".webm" => "video/webm",
            ".mov"  => "video/quicktime",
            ".mp3"  => "audio/mpeg",
            ".wav"  => "audio/wav",
            ".m4a"  => "audio/mp4",
            ".aac"  => "audio/aac",
            ".ogg"  => "audio/ogg",
            _       => "application/octet-stream"
        };

    private static int GetFreeLoopbackPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _files.Clear();

        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch
        {}
    }
}