using Microsoft.Data.Sqlite;

namespace StreamElementsToStreamerBotOverlayMigrator.DataServices
{
    public class DatabaseManagerDb
    {
        public static void VacuumIfNeeded(SqliteConnection connection, double freeRatioThreshold = 0.15)
        {
            using SqliteCommand pageCountCommand = connection.CreateCommand();
            pageCountCommand.CommandText = "PRAGMA page_count;";
            long totalPages = (long) (pageCountCommand.ExecuteScalar() ?? 0L);

            if (totalPages == 0)
                return;

            using SqliteCommand freelistCommand = connection.CreateCommand();
            freelistCommand.CommandText = "PRAGMA freelist_count;";
            long freePages = (long) (freelistCommand.ExecuteScalar() ?? 0L);

            double freeRatio = (double) freePages / totalPages;

            if (freeRatio > freeRatioThreshold)
            {
                using SqliteCommand vacuumCommand = connection.CreateCommand();
                vacuumCommand.CommandText = "VACUUM;";
                vacuumCommand.ExecuteNonQuery();
            }
        }
    }
}