using FSO.Common.Utils;
using FSO.Files.Formats;
using System.Collections.Concurrent;
using static FSO.Client.Utils.MultithreadedZipExtractor;

namespace FSO.Client.Utils
{
    internal class CabExtractor : AbstractExtractor
    {
        private struct QueuedFile
        {
            public string Path;
            public byte[] Data;
        }

        private struct ActiveFile
        {
            public bool IsActive;
            public CabFileEntry InitialEntry;
            public byte[] Data;
            public int WriteOffset;
            public int RemainingSize => (int)InitialEntry.Size - WriteOffset;

            public ActiveFile(CabFileEntry entry)
            {
                IsActive = true;
                InitialEntry = entry;
                Data = new byte[entry.Size];
            }

            public void AddChunk(Span<byte> chunk)
            {
                chunk.CopyTo(Data.AsSpan(WriteOffset));

                WriteOffset += chunk.Length;
            }
        }

        private Thread _fileWriterThread;
        private readonly BlockingCollection<QueuedFile> _fileQueue = new(50);
        private readonly HashSet<string> CreatedFolders = [];
        private int _extractedCount;
        private int _entryCount;

        public override void Start(string path, string extractPath, ZipExtractionProgressDelegate onUpdate)
        {
            base.Start(path, extractPath, onUpdate);

            _onUpdate?.Invoke(ZipExtractionStatus.Preparing, 0, 0);

            _fileWriterThread = new Thread(ConsumeIO);
            _fileWriterThread.Start();

            Task.Run(async () =>
            {
                try
                {
                    await ExtractCab(path);

                    StopFileWriter();
                }
                catch (Exception e)
                {
                    ReportError(e);
                }
            });
        }

        private async Task ExtractCab(string path)
        {
            var firstCab = new CabFile(path);
            string cabRoot = Path.GetDirectoryName(path);

            var cab = firstCab;

            var files = new HashSet<string>();

            // Try and calculate the total number of files by scanning all the cab files.

            while (cab != null)
            {
                foreach (var file in cab.Files)
                {
                    files.Add(file.Filename);
                }

                cab = cab.NextCabName == null ? null : new CabFile(PathUtils.SafeCombine(cabRoot, cab.NextCabName), false);
            }

            cab = firstCab;

            _entryCount = files.Count; // The whole archive counts as a file that needs to be completed.

            _onUpdate?.Invoke(ZipExtractionStatus.Extracting, 0, _entryCount);

            CabBlockDecompressor activeFolder = null;
            do
            {
                var folderData = new CabBlockDecompressor[cab.Folders.Length];

                int folderI = 0;
                foreach (var folder in cab.Folders)
                {
                    var folderDecomp = folderI == 0 && activeFolder != null ? activeFolder : new();

                    var hasNext = folderDecomp.AddBlocks(folder.Blocks);

                    folderData[folderI++] = folderDecomp;

                    if (hasNext)
                    {
                        activeFolder = folderDecomp;
                    }
                    else
                    {
                        activeFolder = null;
                    }
                }

                foreach (var file in cab.Files)
                {
                    bool hasPrev = file.FolderID == 0xFFFD || file.FolderID == 0xFFFF;
                    bool hasNext = file.FolderID == 0xFFFE || file.FolderID == 0xFFFF;

                    ushort folderId = file.FolderID switch
                    {
                        0xFFFD => 0,
                        0xFFFF => 0,
                        0xFFFE => (ushort)(cab.FolderCount - 1),
                        _ => file.FolderID
                    };

                    var dataSource = folderData[folderId];

                    if (!hasNext)
                    {
                        // Flush this file's data to the filesystem.

                        _fileQueue.Add(new QueuedFile()
                        {
                            Path = file.Filename,
                            Data = dataSource.GetData((int)file.Offset, (int)file.Size)
                        });
                    }
                }

                if (cab.NextCabName != null)
                {
                    cab = new CabFile(PathUtils.SafeCombine(cabRoot, cab.NextCabName));

                    Filename = Path.GetFileName(cab.NextCabName);
                }
                else
                {
                    cab = null;
                }
            }
            while (cab != null && !_failed);
        }

        protected override void HandleError()
        {
            base.HandleError();

            _fileQueue?.Add(new QueuedFile());
        }

        private void ConsumeIO()
        {
            try
            {
                while (!_failed)
                {
                    var item = _fileQueue.Take();

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

        private string GetDirectory(string path)
        {
            string dir = Path.GetDirectoryName(path);
            string targetDir = PathUtils.SafeCombine(_extractPath, dir);

            bool isCreated = false;
            lock (CreatedFolders)
            {
                isCreated = CreatedFolders.Contains(targetDir);
            }

            if (!isCreated)
            {
                Directory.CreateDirectory(targetDir);

                lock (CreatedFolders)
                {
                    CreatedFolders.Add(targetDir);
                }
            }

            return PathUtils.SafeCombine(_extractPath, path);
        }

        private void StopFileWriter()
        {
            _fileQueue.Add(default);

            _fileWriterThread.Join();
        }

        private void SignalUpdate()
        {
            int extracted = Interlocked.Increment(ref _extractedCount);

            _onUpdate?.Invoke(extracted == _entryCount ? ZipExtractionStatus.Completed : ZipExtractionStatus.Extracting, extracted, _entryCount);
        }

        public override void Dispose()
        {

        }
    }
}
