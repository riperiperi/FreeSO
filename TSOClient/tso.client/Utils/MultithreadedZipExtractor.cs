using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace FSO.Client.Utils
{
    public enum ZipExtractionStatus
    {
        Preparing,
        Extracting,
        Completed
    }

    public class MultithreadedZipExtractor : IDisposable
    {
        private struct QueuedFile
        {
            public string Path;
            public byte[] Data;
        }

        public delegate void ZipExtractionProgressDelegate(ZipExtractionStatus status, int extracted, int total);
        private const int IOThreadCount = 4;

        private string _extractPath;
        private int _entryCount;
        private Thread _extractThread;

        private HashSet<string> _createdFolders = new HashSet<string>();

        private int _extractedCount;
        private bool _cancelled;
        private ZipExtractionProgressDelegate _onUpdate;

        public MultithreadedZipExtractor(string path, string extractPath, ZipExtractionProgressDelegate onUpdate)
        {
            _onUpdate = onUpdate;
            _extractPath = extractPath;

            _extractThread = new Thread(() => ExtractThread(path));
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
                    if (_cancelled) break;

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

                Thread[] consumers = new Thread[IOThreadCount];

                for (int i = 0; i < consumers.Length; i++)
                {
                    consumers[i] = new Thread(() => ConsumeIO(queue));
                    consumers[i].Start();
                }

                foreach (var entry in entries)
                {
                    if (_cancelled) break;

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

        private void ConsumeIO(BlockingCollection<QueuedFile> queue)
        {
            while (true)
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

        private void SignalUpdate()
        {
            int extracted = Interlocked.Increment(ref _extractedCount);

            _onUpdate?.Invoke(extracted == _entryCount ? ZipExtractionStatus.Completed : ZipExtractionStatus.Extracting, extracted, _entryCount);
        }

        private string GetDirectory(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string targetDir = Path.Combine(_extractPath, dir);

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

            return Path.Combine(_extractPath, path);
        }

        public void Dispose()
        {
            _cancelled = true;
        }
    }
}
