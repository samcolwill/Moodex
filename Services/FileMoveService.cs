using SamsGameLauncher.Services;
using System.Diagnostics;
using System.IO;

public class FileMoveService : IFileMoveService
{
    public async Task<bool> MoveFolderAsync(string src, string dst,
                                            IProgress<MoveProgress> prog,
                                            CancellationToken token,
                                            bool compressToArchive = false,
                                            string? sevenZipPath = null)
    {
        // Check if source is a zip file (moving from archive to active)
        bool isSourceZipped = File.Exists(src) && src.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
        
        if (isSourceZipped)
        {
            // Extract zip file to destination
            return await ExtractZipAsync(src, dst, sevenZipPath, prog, token);
        }
        else if (compressToArchive && !string.IsNullOrEmpty(sevenZipPath))
        {
            // Compress folder to zip when archiving
            return await CompressToZipAsync(src, dst, sevenZipPath, prog, token);
        }
        else
        {
            // Normal folder move (for non-compressed archive or when compression is disabled)
            return await MoveFolderNormalAsync(src, dst, prog, token);
        }
    }

    private async Task<bool> MoveFolderNormalAsync(string src, string dst,
                                                    IProgress<MoveProgress> prog,
                                                    CancellationToken token)
    {
        // simple two‑phase: scan size → copy/delete
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
        const int BUF = 1024 * 1024;          // 1 MiB
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

    private async Task<bool> CompressToZipAsync(string srcFolder, string dstFolder,
                                                 string sevenZipPath,
                                                 IProgress<MoveProgress> prog,
                                                 CancellationToken token)
    {
        try
        {
            // Get the folder name to use for the zip file and the folder inside the zip
            var folderName = Path.GetFileName(srcFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            
            // Create the destination directory structure
            // Instead of just the console dir, create: <Archive>\<Console>\<GameName>\
            // This will hold both the cover image and the zip file
            var gameArchiveFolder = dstFolder; // This is already <Archive>\<Console>\<GameName>
            Directory.CreateDirectory(gameArchiveFolder);
            
            var zipFilePath = Path.Combine(gameArchiveFolder, $"{folderName}.zip");

            prog.Report(new MoveProgress(5, "Copying cover image..."));

            // Find and copy the cover image before compressing
            await CopyCoverImageAsync(srcFolder, folderName, gameArchiveFolder);

            prog.Report(new MoveProgress(10, "Preparing compression..."));

            // Use 7zip to create a zip file with the folder structure preserved
            // This will create MyGame.zip containing MyGame\file1, MyGame\file2, etc.
            // Command: 7z a -tzip "output.zip" "sourceFolder" -mx=5 -bsp1 (progress to stdout)
            var processInfo = new ProcessStartInfo
            {
                FileName = sevenZipPath,
                Arguments = $"a -tzip \"{zipFilePath}\" \"{srcFolder}\" -mx=5 -bsp1",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(srcFolder)
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();

            // Track progress updates
            double lastReportedPercent = 10;
            var progressTimer = new System.Timers.Timer(500); // Update every 500ms
            progressTimer.Elapsed += (s, e) =>
            {
                // Gradually increase progress if we haven't received updates
                if (lastReportedPercent < 90)
                {
                    lastReportedPercent += 2;
                    prog.Report(new MoveProgress(lastReportedPercent, "Compressing..."));
                }
            };
            progressTimer.Start();

            // Read output for progress
            var outputTask = Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (line != null && line.Contains("%"))
                    {
                        // Try to parse percentage from 7zip output
                        var parts = line.Split('%');
                        if (parts.Length > 0)
                        {
                            var percentStr = new string(parts[0].Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
                            if (double.TryParse(percentStr, out double percent))
                            {
                                lastReportedPercent = percent;
                                prog.Report(new MoveProgress(percent, "Compressing..."));
                            }
                        }
                    }
                }
            });

            await process.WaitForExitAsync(token);
            await outputTask;
            progressTimer.Stop();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"7zip compression failed: {error}");
            }

            prog.Report(new MoveProgress(95, "Cleaning up source folder..."));

            // Delete source folder after successful compression
            Directory.Delete(srcFolder, recursive: true);

            prog.Report(new MoveProgress(100, "Compression complete!"));
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to compress folder: {ex.Message}", ex);
        }
    }

    private async Task<bool> ExtractZipAsync(string zipFilePath, string dstFolder,
                                             string? sevenZipPath,
                                             IProgress<MoveProgress> prog,
                                             CancellationToken token)
    {
        try
        {
            prog.Report(new MoveProgress(10, "Preparing extraction..."));

            // Ensure destination folder exists
            Directory.CreateDirectory(dstFolder);

            if (!string.IsNullOrEmpty(sevenZipPath) && File.Exists(sevenZipPath))
            {
                // Use 7zip to extract
                // Command: 7z x "archive.zip" -o"outputFolder" -y -bsp1 (progress to stdout)
                var processInfo = new ProcessStartInfo
                {
                    FileName = sevenZipPath,
                    Arguments = $"x \"{zipFilePath}\" -o\"{dstFolder}\" -y -bsp1",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                // Track progress updates
                double lastReportedPercent = 10;
                var progressTimer = new System.Timers.Timer(500); // Update every 500ms
                progressTimer.Elapsed += (s, e) =>
                {
                    // Gradually increase progress if we haven't received updates
                    if (lastReportedPercent < 90)
                    {
                        lastReportedPercent += 2;
                        prog.Report(new MoveProgress(lastReportedPercent, "Extracting..."));
                    }
                };
                progressTimer.Start();

                // Read output for progress
                var outputTask = Task.Run(async () =>
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        var line = await process.StandardOutput.ReadLineAsync();
                        if (line != null && line.Contains("%"))
                        {
                            // Try to parse percentage from 7zip output
                            var parts = line.Split('%');
                            if (parts.Length > 0)
                            {
                                var percentStr = new string(parts[0].Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
                                if (double.TryParse(percentStr, out double percent))
                                {
                                    lastReportedPercent = percent;
                                    prog.Report(new MoveProgress(percent, "Extracting..."));
                                }
                            }
                        }
                    }
                });

                await process.WaitForExitAsync(token);
                await outputTask;
                progressTimer.Stop();

                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"7zip extraction failed: {error}");
                }
            }
            else
            {
                // Fallback to .NET's built-in ZipFile
                prog.Report(new MoveProgress(20, "Extracting..."));
                await Task.Run(() =>
                {
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipFilePath, dstFolder, true);
                }, token);
                prog.Report(new MoveProgress(90, "Extraction complete..."));
            }

            prog.Report(new MoveProgress(95, "Cleaning up zip file..."));

            // Delete the zip file after successful extraction
            File.Delete(zipFilePath);

            prog.Report(new MoveProgress(100, "Extraction complete!"));
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to extract zip: {ex.Message}", ex);
        }
    }

    private async Task CopyCoverImageAsync(string srcFolder, string gameName, string dstFolder)
    {
        try
        {
            // Look for cover images with the game name
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg" };
            
            foreach (var ext in imageExtensions)
            {
                var coverPath = Path.Combine(srcFolder, gameName + ext);
                if (File.Exists(coverPath))
                {
                    var destCoverPath = Path.Combine(dstFolder, gameName + ext);
                    await Task.Run(() => File.Copy(coverPath, destCoverPath, overwrite: true));
                    return; // Found and copied the cover, we're done
                }
            }
        }
        catch (Exception ex)
        {
            // Don't fail the whole operation if cover copy fails, just log it
            System.Diagnostics.Debug.WriteLine($"Failed to copy cover image: {ex.Message}");
        }
    }
}
