using System.Diagnostics;
using System.Text;

namespace Hercules
{
    public sealed class Logger : IDisposable
    {
        private const int MaxHistoryEntries = 150;
        private readonly object _sync = new();
        private readonly List<string> _history = new();
        private FileStream? _fileStream;
        private bool _disposed;

        public IReadOnlyList<string> History
        {
            get
            {
                lock (_sync)
                    return _history.ToArray();
            }
        }

        public bool Initialized { get; private set; }
        public bool NoWriteMode { get; private set; }
        public string? FileLocation { get; private set; }

        public string AsDocument
        {
            get
            {
                lock (_sync)
                    return string.Join('\n', _history);
            }
        }

        public void Initialize(bool useTempDir = false)
        {
            const string logIdent = "Logger::Initialize";

            lock (_sync)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (Initialized)
                    return;
            }

            string directory = useTempDir ? Paths.TempLogs : Path.Combine(Paths.Base, "Logs");
            Directory.CreateDirectory(directory);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
            string location = Path.Combine(directory, $"{App.ProjectName}_{timestamp}.log");

            try
            {
                lock (_sync)
                {
                    ObjectDisposedException.ThrowIf(_disposed, this);
                    FileLocation = location;
                    _fileStream = new FileStream(
                        location,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.Read,
                        4096,
                        useAsync: false);
                    Initialized = true;

                    if (_history.Count > 0)
                        WriteToFileLocked(string.Join("\r\n", _history));
                }

                WriteLine(logIdent, $"Logger initialized at {location}");
                CleanupOldLogs(directory);
            }
            catch (UnauthorizedAccessException)
            {
                lock (_sync)
                    NoWriteMode = true;

                Frontend.ShowMessageBox(
                    string.Format(Strings.Logger_NoWriteMode, directory),
                    System.Windows.MessageBoxImage.Warning,
                    System.Windows.MessageBoxButton.OK);
            }
            catch (IOException ex)
            {
                WriteLine(logIdent, $"Failed to initialize due to IO exception: {ex.Message}");
            }
        }

        private void CleanupOldLogs(string directory)
        {
            if (!Paths.Initialized || !Directory.Exists(directory))
                return;

            foreach (FileInfo log in new DirectoryInfo(directory).GetFiles())
            {
                if (log.LastWriteTimeUtc.AddDays(7) > DateTime.UtcNow)
                    continue;

                try
                {
                    log.Delete();
                    WriteLine("Logger::Cleanup", $"Deleted old log file '{log.Name}'");
                }
                catch (Exception ex)
                {
                    WriteException("Logger::Cleanup", ex);
                }
            }
        }

        private void WriteLine(string message)
        {
            string timestamp = DateTime.UtcNow.ToString("s") + "Z";
            string sanitizedMessage = message.Replace(
                Paths.UserProfile,
                "%UserProfile%",
                StringComparison.InvariantCultureIgnoreCase);
            string output = $"{timestamp} {sanitizedMessage}";

            Debug.WriteLine(output);

            lock (_sync)
            {
                if (_disposed)
                    return;

                _history.Add(output);
                if (_history.Count > MaxHistoryEntries)
                    _history.RemoveAt(0);

                if (Initialized && _fileStream is not null)
                {
                    try
                    {
                        WriteToFileLocked(output);
                    }
                    catch (IOException ex)
                    {
                        Debug.WriteLine($"Logger write failed: {ex.Message}");
                    }
                    catch (ObjectDisposedException)
                    {
                        // Shutdown raced with a final log entry.
                    }
                }
            }
        }

        private void WriteToFileLocked(string message)
        {
            if (_fileStream is null)
                return;

            byte[] buffer = Encoding.UTF8.GetBytes(message + "\r\n");
            _fileStream.Write(buffer, 0, buffer.Length);
            _fileStream.Flush();
        }

        public void WriteLine(string identifier, string message) =>
            WriteLine($"[{identifier}] {message}");

        public void WriteException(string identifier, Exception ex)
        {
            string hresult = $"0x{ex.HResult:X8}";
            WriteLine($"[{identifier}] ({hresult}) {ex}");
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed)
                    return;

                _disposed = true;
                _fileStream?.Dispose();
                _fileStream = null;
                Initialized = false;
            }
        }
    }
}
