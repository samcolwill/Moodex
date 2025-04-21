using SamsGameLauncher.Services;
using System.IO;

public class FileMoveService : IFileMoveService
{
    public async Task<bool> MoveFolderAsync(string src, string dst,
                                            IProgress<MoveProgress> prog,
                                            CancellationToken token)
    {
        // 0) simple two‑phase: scan size → copy/delete
        var files = Directory.GetFiles(src, "*.*", SearchOption.AllDirectories);
        long totalBytes = files.Sum(f => new FileInfo(f).Length);
        long doneBytes = 0;

        // create folder tree first
        foreach (var dir in Directory.GetDirectories(src, "*", SearchOption.AllDirectories))
        {
            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(dir.Replace(src, dst));
        }

        // copy files with progress
        const int BUF = 1024 * 1024;          // 1 MiB
        var buffer = new byte[BUF];

        foreach (var file in files)
        {
            token.ThrowIfCancellationRequested();

            var rel = Path.GetRelativePath(src, file);
            var target = Path.Combine(dst, rel);
            using var fsIn = new FileStream(file, FileMode.Open, FileAccess.Read);
            using var fsOut = new FileStream(target, FileMode.Create, FileAccess.Write);

            int read;
            while ((read = await fsIn.ReadAsync(buffer, token)) > 0)
            {
                await fsOut.WriteAsync(buffer.AsMemory(0, read), token);
                doneBytes += read;

                prog.Report(new MoveProgress(
                    Percent: doneBytes * 100.0 / totalBytes,
                    CurrentFile: rel));
            }
        }

        // delete source when done
        Directory.Delete(src, recursive: true);
        return true;
    }
}
