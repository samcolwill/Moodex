using System.IO;
using System.Text.Json;
using Moodex.Models;
using Moodex.Models.Manifests;
using SharpCompress.Common;
using SharpCompress.Readers;
using SharpCompress.Writers;
using SharpCompress.Writers.Zip;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;

namespace Moodex.Services
{
    public record MoveProgress(double Percent, string? CurrentFile);

    public record ArchiveResult(bool Success, string? Message = null);

    public interface IArchiveService
    {
        Task<ArchiveResult> ArchiveGameAsync(GameInfo game, string archiveRoot, IProgress<MoveProgress>? progress, CancellationToken token);
        Task<ArchiveResult> RestoreGameAsync(GameInfo game, string archiveRoot, IProgress<MoveProgress>? progress, CancellationToken token);
    }

    public class ArchiveService : IArchiveService
    {
        private sealed class ReportingStream : Stream
        {
            private readonly Stream _inner;
            private readonly Action<long> _onRead;
            public ReportingStream(Stream inner, Action<long> onRead)
            {
                _inner = inner;
                _onRead = onRead;
            }
            public override bool CanRead => _inner.CanRead;
            public override bool CanSeek => _inner.CanSeek;
            public override bool CanWrite => _inner.CanWrite;
            public override long Length => _inner.Length;
            public override long Position { get => _inner.Position; set => _inner.Position = value; }
            public override void Flush() => _inner.Flush();
            public override int Read(byte[] buffer, int offset, int count)
            {
                int read = _inner.Read(buffer, offset, count);
                if (read > 0) _onRead(read);
                return read;
            }
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
            public override void SetLength(long value) => _inner.SetLength(value);
            public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
            protected override void Dispose(bool disposing)
            {
                if (disposing) _inner.Dispose();
                base.Dispose(disposing);
            }
        }
        private readonly ISettingsService _settings;

        public ArchiveService(ISettingsService settings)
        {
            _settings = settings;
        }

        public async Task<ArchiveResult> ArchiveGameAsync(GameInfo game, string archiveRoot, IProgress<MoveProgress>? progress, CancellationToken token)
        {
            if (string.IsNullOrEmpty(game.GameRootPath) || string.IsNullOrEmpty(game.GameGuid))
                return new ArchiveResult(false, "Game root or GUID missing");

            var dataPath = Path.Combine(game.GameRootPath, "data");
            if (!Directory.Exists(dataPath))
                return new ArchiveResult(false, "No data folder to archive");

            var gameDataRoot = Path.Combine(archiveRoot, "Game Data");
            Directory.CreateDirectory(gameDataRoot);
            var files = Directory.GetFiles(dataPath, "*", SearchOption.AllDirectories);
            long total = files.Sum(f => new FileInfo(f).Length);
            long done = 0;

            if (_settings.Load().CompressOnArchive)
            {
                var zipPath = Path.Combine(gameDataRoot, game.GameGuid + ".zip");
            progress?.Report(new MoveProgress(0, "Preparing compression..."));
                if (File.Exists(zipPath)) File.Delete(zipPath);

                await Task.Run(() =>
                {
                    using var outStream = File.Create(zipPath);
                    var options = new ZipWriterOptions(CompressionType.Deflate)
                    {
                        LeaveStreamOpen = false,
                        ArchiveEncoding = new ArchiveEncoding(),
                        UseZip64 = true
                    };
                    using var writer = WriterFactory.Open(outStream, ArchiveType.Zip, options);
                    foreach (var file in files)
                    {
                        token.ThrowIfCancellationRequested();
                        var rel = Path.GetRelativePath(dataPath, file).Replace('\\', '/');
                        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan);
                        using var rs = new ReportingStream(fs, bytes =>
                        {
                            done += bytes;
                            var pct = total > 0 ? (done * 100.0 / total) : 100;
                            progress?.Report(new MoveProgress(pct, rel));
                        });
                        writer.Write(rel, rs, DateTime.Now);
                    }
                }, token);
            }
            else
            {
                var folderPath = Path.Combine(gameDataRoot, game.GameGuid);
                progress?.Report(new MoveProgress(0, "Preparing move..."));
                Directory.CreateDirectory(folderPath);
                await Task.Run(() =>
                {
                    foreach (var file in files)
                    {
                        token.ThrowIfCancellationRequested();
                        var rel = Path.GetRelativePath(dataPath, file);
                        var dest = Path.Combine(folderPath, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        File.Copy(file, dest, overwrite: true);
                        done += new FileInfo(file).Length;
                        var pct = total > 0 ? (done * 100.0 / total) : 100;
                        progress?.Report(new MoveProgress(pct, rel));
                    }
                }, token);
            }

            progress?.Report(new MoveProgress(100, "Finalizing..."));

            // delete data folder content after success
            Directory.Delete(dataPath, recursive: true);

            // verify cleanup
            if (Directory.Exists(dataPath))
            {
                return new ArchiveResult(false, "Data folder still exists after archive");
            }

            // update manifest
            var manifestPath = Path.Combine(game.GameRootPath!, ".moodex_game");
            if (File.Exists(manifestPath))
            {
                var json = await File.ReadAllTextAsync(manifestPath, token);
                var man = JsonSerializer.Deserialize<GameManifest>(json) ?? new GameManifest();
                man.Archived = true;
                man.ArchivedDateTime = DateTime.UtcNow;
                await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(man, new JsonSerializerOptions { WriteIndented = true }), token);
            }

            progress?.Report(new MoveProgress(100, "Archived"));
            return new ArchiveResult(true);
        }

        public async Task<ArchiveResult> RestoreGameAsync(GameInfo game, string archiveRoot, IProgress<MoveProgress>? progress, CancellationToken token)
        {
            if (string.IsNullOrEmpty(game.GameRootPath) || string.IsNullOrEmpty(game.GameGuid))
                return new ArchiveResult(false, "Game root or GUID missing");

            var gameDataRoot = Path.Combine(archiveRoot, "Game Data");
            var zipPath = Path.Combine(gameDataRoot, game.GameGuid + ".zip");
            var folderPath = Path.Combine(gameDataRoot, game.GameGuid);
            if (!File.Exists(zipPath) && !Directory.Exists(folderPath))
                return new ArchiveResult(false, "Archive not found");

            var dataPath = Path.Combine(game.GameRootPath, "data");
            Directory.CreateDirectory(dataPath);

            progress?.Report(new MoveProgress(0, "Extracting..."));

            if (File.Exists(zipPath))
            {
                // Extract zip with per-byte progress for large single files
                await Task.Run(() =>
                {
                    using var archive = ZipArchive.Open(zipPath);
                    var entries = archive.Entries.Where(e => !e.IsDirectory).ToList();
                    long total = entries.Sum(e => (long)e.Size);
                    long done = 0;
                    foreach (var entry in entries)
                    {
                        token.ThrowIfCancellationRequested();
                        var outPath = Path.Combine(dataPath, entry.Key.Replace('/', Path.DirectorySeparatorChar));
                        var outDir = Path.GetDirectoryName(outPath);
                        if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);

                        using var inStream = entry.OpenEntryStream();
                        using var outStream = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan);

                        var buffer = new byte[1024 * 1024];
                        int read;
                        while ((read = inStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            token.ThrowIfCancellationRequested();
                            outStream.Write(buffer, 0, read);
                            done += read;
                            var pct = total > 0 ? (done * 100.0 / total) : 100;
                            progress?.Report(new MoveProgress(pct, entry.Key));
                        }
                    }
                }, token);
                // delete zip after successful restore
                File.Delete(zipPath);
            }
            else
            {
                // Copy folder back with streaming progress
                var filesToCopy = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
                long totalBack = filesToCopy.Sum(f => new FileInfo(f).Length);
                long doneBack = 0;
                await Task.Run(() =>
                {
                    foreach (var f in filesToCopy)
                    {
                        token.ThrowIfCancellationRequested();
                        var rel = Path.GetRelativePath(folderPath, f);
                        var dest = Path.Combine(dataPath, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        using var src = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan);
                        using var dst = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.SequentialScan);
                        var buffer = new byte[1024 * 1024];
                        int read;
                        while ((read = src.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            token.ThrowIfCancellationRequested();
                            dst.Write(buffer, 0, read);
                            doneBack += read;
                            var pct = totalBack > 0 ? (doneBack * 100.0 / totalBack) : 100;
                            progress?.Report(new MoveProgress(pct, rel));
                        }
                    }
                }, token);
                // remove archive folder
                Directory.Delete(folderPath, recursive: true);
            }

            progress?.Report(new MoveProgress(100, "Finalizing..."));

            // verify extraction
            if (!Directory.Exists(dataPath) || Directory.GetFiles(dataPath, "*", SearchOption.AllDirectories).Length == 0)
            {
                return new ArchiveResult(false, "No files found after extraction");
            }

            // update manifest
            var manifestPath = Path.Combine(game.GameRootPath!, ".moodex_game");
            if (File.Exists(manifestPath))
            {
                var json = await File.ReadAllTextAsync(manifestPath, token);
                var man = JsonSerializer.Deserialize<GameManifest>(json) ?? new GameManifest();
                man.Archived = false;
                man.ArchivedDateTime = null;
                await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(man, new JsonSerializerOptions { WriteIndented = true }), token);
            }

            progress?.Report(new MoveProgress(100, "Restored"));
            return new ArchiveResult(true);
        }
    }
}


