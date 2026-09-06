using Microsoft.Data.Sqlite;
using StreamElementsToStreamerBotOverlayMigrator.DataServices;
using System.IO;

namespace StreamElementsToStreamerBotOverlayMigrator.Services
{
    public class DatabaseManager
    {
        private const           int    MaxDatabaseBackupCount = 5;
        private const           string DatabaseFileExtension  = ".db";

        private static readonly string DefaultRootFolderPath  = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "imA-SB-Widgets"));
        private static readonly string DatabaseFilePath       = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "database.db"));
        private static readonly string ConnectionString       = $"Data Source={DatabaseFilePath}";

        public static string GetDatabaseConnectionString()
            => ConnectionString;

        public static void RestoreDatabaseBackupIfNeeded()
        {
            if (File.Exists(DatabaseFilePath))
                return;

            string backupFolder = Path.GetFullPath(Path.Combine(DefaultRootFolderPath, "Backups"));

            if (!IsPathWithinBaseDirectory(DefaultRootFolderPath, backupFolder) || !Directory.Exists(backupFolder))
                return;

            string? latestBackup = Directory
                .EnumerateFiles(backupFolder, $"*{DatabaseFileExtension}")
                .OrderByDescending(file => file)
                .FirstOrDefault();

            if (latestBackup == null)
                return;

            try
            {
                File.Copy(latestBackup, DatabaseFilePath);
            }
            catch (Exception)
            { }
        }

        public static void CreateDatabaseBackupIfNeeded()
        {
            if (!File.Exists(DatabaseFilePath))
                return;

            try
            {
                string backupFolder = Path.GetFullPath(Path.Combine(DefaultRootFolderPath, "Backups"));

                if (!IsPathWithinBaseDirectory(DefaultRootFolderPath, backupFolder))
                    return;

                Directory.CreateDirectory(backupFolder);

                string todayStamp = DateTime.Now.ToString("yyyy-MM-dd");

                bool backupExistsForToday = Directory
                    .EnumerateFiles(backupFolder, $"*{DatabaseFileExtension}")
                    .Any(file => Path.GetFileName(file).StartsWith(todayStamp));

                if (backupExistsForToday)
                    return;

                string backupPath = Path.Combine(backupFolder, $"{todayStamp}{DatabaseFileExtension}");

                File.Copy(DatabaseFilePath, backupPath, true);

                List<string> allBackups = Directory
                    .EnumerateFiles(backupFolder, $"*{DatabaseFileExtension}")
                    .OrderBy(file => file)
                    .ToList();

                while (allBackups.Count > MaxDatabaseBackupCount)
                {
                    File.Delete(allBackups[0]);
                    allBackups.RemoveAt(0);
                }
            }
            catch (Exception)
            { }
        }

        public static void ResizeDatabaseIfNeeded()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();
            DatabaseManagerDb.VacuumIfNeeded(connection);
        }

        private static bool IsPathWithinBaseDirectory(string baseDirectory, string candidatePath)
        {
            string fullBasePath = Path.GetFullPath(baseDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            string fullCandidatePath = Path.GetFullPath(candidatePath);

            return fullCandidatePath.StartsWith(fullBasePath, StringComparison.OrdinalIgnoreCase);
        }
    }
}