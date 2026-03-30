using System.IO.Compression;

namespace LTDSaveEditor.Core;

public class BackupManager
{
    public string BackupDirectory { get; }
    public int MaxBackups { get; set; } = 10;

    public BackupManager(string path)
    {
        BackupDirectory = Path.Combine(path, "Backups");

        Directory.CreateDirectory(BackupDirectory);
    }

    public void CreateBackup(params string[] sourceFilePaths)
    {
        if (sourceFilePaths == null || sourceFilePaths.Length == 0)
            throw new ArgumentException("At least one source file path must be provided.", nameof(sourceFilePaths));

        // Validate if files exist
        foreach (var sourceFilePath in sourceFilePaths)
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException("Source file not found.", sourceFilePath);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupName = $"Backup_{timestamp}.zip";
        string backupPath = Path.Combine(BackupDirectory, backupName);

        // Create zip archive
        using (var archive = ZipFile.Open(backupPath, ZipArchiveMode.Create))
        {
            foreach (var sourceFilePath in sourceFilePaths)
            {
                string entryName = Path.GetFileName(sourceFilePath);
                archive.CreateEntryFromFile(sourceFilePath, entryName);
            }
        }

        CleanupOldBackups();
    }

    private void CleanupOldBackups()
    {
        var backupFiles = new DirectoryInfo(BackupDirectory)
            .GetFiles("Backup_*.zip")
            .OrderByDescending(f => f.CreationTime)
            .ToList();

        if (backupFiles.Count <= MaxBackups)
            return;

        var filesToDelete = backupFiles.Skip(MaxBackups);

        foreach (var file in filesToDelete)
        {
            try
            {
                file.Delete();
            }
            catch
            {
                // Optional: log or ignore failures
            }
        }
    }
}
