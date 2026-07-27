using FSO.Common.Utils;
using System.Collections.Concurrent;
using System.IO.Compression;
using static FSO.Client.Utils.MultithreadedZipExtractor;

namespace FSO.Client.Utils
{
    public enum ZipExtractionStatus
    {
        Preparing,
        Extracting,
        Completed,
        Error
    }

    public abstract class AbstractExtractor : IDisposable
    {
        protected string _extractPath;
        protected ZipExtractionProgressDelegate _onUpdate;

        protected bool _failed;
        public Exception Error { get; private set; }
        public string Filename { get; protected set; }

        public virtual void Start(string path, string extractPath, ZipExtractionProgressDelegate onUpdate)
        {
            Filename = Path.GetFileName(path);
            _onUpdate = onUpdate;
            _extractPath = extractPath;
        }

        public abstract void Dispose();

        protected virtual void HandleError()
        {

        }

        public void ReportError(Exception e)
        {
            if (Interlocked.Exchange(ref _failed, true) == false)
            {
                Error = e;

                HandleError();

                _onUpdate?.Invoke(ZipExtractionStatus.Error, 0, 0);
            }
        }
    }

    public class MultithreadedZipExtractor : AbstractExtractor
    {
        private struct QueuedFile
        {
            public string Path;
            public byte[] Data;
        }

        public delegate void ZipExtractionProgressDelegate(ZipExtractionStatus status, int extracted, int total);
        private const int IOThreadCount = 4;

        private int _entryCount;
        private Thread _extractThread;

        private HashSet<string> _createdFolders = new HashSet<string>();

        private int _extractedCount;
        private bool _cancelled;
        private BlockingCollection<QueuedFile> _fileQueue;

        public override void Start(string path, string extractPath, ZipExtractionProgressDelegate onUpdate)
        {
            base.Start(path, extractPath, onUpdate);

            _extractThread = new Thread(() =>
            {
                try
                {
                    ExtractThread(path);
                }
                catch (Exception e)
                {
                    ReportError(e);
                }
            });
            _extractThread.Start();
        }

        public void ExtractThread(string path)
        {
            using (var file = ZipFile.OpenRead(path))
            {
                _entryCount = 0;
                var entries = new List<ZipArchiveEntry>();

                _onUpdate?.Invoke(ZipExtractionStatus.Preparing, 0, 0);

                foreach (var entry in file.Entries)
                {
                    if (_cancelled || _failed) break;

                    if (entry.Name.Length == 0) continue;

                    entries.Add(entry);
                    _entryCount++;

                    _onUpdate?.Invoke(ZipExtractionStatus.Preparing, 0, _entryCount);
                }

                if (_entryCount == 0)
                {
                    _onUpdate?.Invoke(ZipExtractionStatus.Completed, 0, 0);
                    return;
                }

                var queue = new BlockingCollection<QueuedFile>(50);
                _fileQueue = queue;

                Thread[] consumers = new Thread[IOThreadCount];

                for (int i = 0; i < consumers.Length; i++)
                {
                    consumers[i] = new Thread(() => ConsumeIO(queue));
                    consumers[i].Start();
                }

                foreach (var entry in entries)
                {
                    if (_cancelled || _failed) break;

                    bool tooBig = entry.Length > 10_000_000;

                    if (tooBig)
                    {
                        string realPath = GetDirectory(entry.FullName);
                        entry.ExtractToFile(realPath, true);

                        SignalUpdate();
                    }
                    else
                    {
                        byte[] data;

                        using (var stream = entry.Open())
                        {
                            using (var mem = new MemoryStream())
                            {
                                stream.CopyTo(mem);
                                data = mem.ToArray();
                            }
                        }

                        var filepath = entry.FullName;

                        queue.Add(new QueuedFile()
                        {
                            Path = entry.FullName,
                            Data = data
                        });
                    }
                }

                for (int i = 0; i < consumers.Length; i++)
                {
                    queue.Add(new QueuedFile()); // Empty items signal for the consumers to shutdown.
                }

                for (int i = 0; i < consumers.Length; i++)
                {
                    consumers[i].Join();
                }

                queue.Dispose();
            }
        }

        protected override void HandleError()
        {
            base.HandleError();

            if (_fileQueue != null)
            {

                for (int i = 0; i < IOThreadCount; i++)
                {
                    // Wake the consumers so that they try to exit.

                    _fileQueue.TryAdd(new QueuedFile());
                }
            }
        }

        private void ConsumeIO(BlockingCollection<QueuedFile> queue)
        {
            try
            {
                while (!_failed && !_cancelled)
                {
                    var item = queue.Take();

                    if (item.Data == null)
                    {
                        return;
                    }

                    string realPath = GetDirectory(item.Path);
                    File.WriteAllBytes(realPath, item.Data);

                    SignalUpdate();
                }
            }
            catch (Exception e)
            {
                ReportError(e);
            }
        }

        private void SignalUpdate()
        {
            int extracted = Interlocked.Increment(ref _extractedCount);

            _onUpdate?.Invoke(extracted == _entryCount ? ZipExtractionStatus.Completed : ZipExtractionStatus.Extracting, extracted, _entryCount);
        }

        private string GetDirectory(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string targetDir = PathUtils.SafeCombine(_extractPath, dir);

            bool isCreated = false;
            lock (_createdFolders)
            {
                isCreated = _createdFolders.Contains(targetDir);
            }

            if (!isCreated)
            {
                Directory.CreateDirectory(targetDir);

                lock (_createdFolders)
                {
                    _createdFolders.Add(targetDir);
                }
            }

            return PathUtils.SafeCombine(_extractPath, path);
        }

        public override void Dispose()
        {
            _cancelled = true;
        }
    }
}
