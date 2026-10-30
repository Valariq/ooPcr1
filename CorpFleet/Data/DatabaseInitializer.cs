using Microsoft.Data.Sqlite;

namespace CorpFleet.Data
{

    public static class DatabaseInitializer
    {
        public static string ConnectionString(string dbPath) =>
            new SqliteConnectionStringBuilder { DataSource = dbPath, ForeignKeys = true }.ToString();

        public static void EnsureCreated(string dbPath, string seedScriptPath)
        {
            bool needSeed = !File.Exists(dbPath) || new FileInfo(dbPath).Length == 0;

            using var connection = new SqliteConnection(ConnectionString(dbPath));
            connection.Open();

            if (!needSeed)
            {
                using var check = connection.CreateCommand();
                check.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name IN ('Categories','Vehicles','MaintenanceLogs')";
                needSeed = Convert.ToInt32(check.ExecuteScalar()) < 3;
            }

            if (!needSeed) return;

            if (!File.Exists(seedScriptPath))
                throw new FileNotFoundException("Не найден SQL-скрипт инициализации.", seedScriptPath);

            using var tx = connection.BeginTransaction();
            using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = File.ReadAllText(seedScriptPath);
            cmd.ExecuteNonQuery();
            tx.Commit();
        }
    }
}
