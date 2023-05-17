using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PathHelper = System.IO.Path;

namespace PuppeteerSharp.Helpers
{
    /// <summary>
    /// Represents a directory that is deleted on disposal.
    /// </summary>
    internal sealed class TempDirectory : IDisposable
    {
        private int _disposed;

        public TempDirectory()
            : this(PathHelper.Combine(PathHelper.GetTempPath(), PathHelper.GetRandomFileName()))
        {
        }

        public TempDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Path must be specified", nameof(path));
            }

            Directory.CreateDirectory(path);
            Path = path;
        }

        ~TempDirectory()
        {
            DisposeCore();
        }

        public string Path { get; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            DisposeCore();
        }

        public override string ToString() => Path;

        private static async Task DeleteAsync(string path)
        {
            const int minDelayInMsec = 200;
            const int maxDelayInMsec = 8000;

            int retryDelay = minDelayInMsec;
            while (true)
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                try
                {
                    Directory.Delete(path, true);
                    return;
                }
                catch
                {
                    await Task.Delay(retryDelay).ConfigureAwait(false);
                    if (retryDelay < maxDelayInMsec)
                    {
                        retryDelay = Math.Min(2 * retryDelay, maxDelayInMsec);
                    }
                }
            }
        }

        private void DisposeCore()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _ = DeleteAsync(Path);
        }
    }
}
