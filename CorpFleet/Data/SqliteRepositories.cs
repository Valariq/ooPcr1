using System.Globalization;
using CorpFleet.Interfaces;
using CorpFleet.Models;
using Microsoft.Data.Sqlite;

namespace CorpFleet.Data
{
 
    public class CategoryRepository
    {
        private readonly string _cs;
        public CategoryRepository(string dbPath) => _cs = DatabaseInitializer.ConnectionString(dbPath);

        public List<Category> GetAll()
        {
            var result = new List<Category>();
            using var con = new SqliteConnection(_cs);
            con.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id, CategoryName, TaxRate FROM Categories ORDER BY Id";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                result.Add(new Category(r.GetInt32(0), r.GetString(1), r.GetDouble(2)));
            return result;
        }
    }


    public class MaintenanceLogRepository
    {
        private readonly string _cs;
        public MaintenanceLogRepository(string dbPath) => _cs = DatabaseInitializer.ConnectionString(dbPath);

        public List<MaintenanceRecord> GetAll()
        {
            var result = new List<MaintenanceRecord>();
            using var con = new SqliteConnection(_cs);
            con.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id, VehicleId, ServiceName, Cost, LogDate, Channel FROM MaintenanceLogs";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                DateTime.TryParseExact(r.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date);
                result.Add(new MaintenanceRecord(r.GetInt32(0), r.GetInt32(1), r.GetString(2),
                    r.GetDouble(3), date, r.GetString(5)));
            }
            return result;
        }

        internal static void Insert(SqliteConnection con, SqliteTransaction tx, MaintenanceRecord log)
        {
            using var cmd = con.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO MaintenanceLogs (VehicleId, ServiceName, Cost, LogDate, Channel)
                                VALUES ($vid, $name, $cost, $date, $channel); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$vid", log.VehicleId);
            cmd.Parameters.AddWithValue("$name", log.ServiceName);
            cmd.Parameters.AddWithValue("$cost", log.Cost);
            cmd.Parameters.AddWithValue("$date", log.LogDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            cmd.Parameters.AddWithValue("$channel", log.Channel);
            log.SetId(Convert.ToInt32(cmd.ExecuteScalar()));
        }
    }


    public class SqliteVehicleRepository : IRepository<Vehicle>
    {
        private readonly string _cs;
        private readonly CategoryRepository _categories;
        private readonly MaintenanceLogRepository _logs;

        public SqliteVehicleRepository(string dbPath)
        {
            _cs = DatabaseInitializer.ConnectionString(dbPath);
            _categories = new CategoryRepository(dbPath);
            _logs = new MaintenanceLogRepository(dbPath);
        }

        private record VehicleRow(int Id, string Model, double BasePrice, int Power, int CategoryId);

        private List<VehicleRow> ReadRows()
        {
            var rows = new List<VehicleRow>();
            using var con = new SqliteConnection(_cs);
            con.Open();
            using var cmd = con.CreateCommand();
            cmd.CommandText = "SELECT Id, Model, BasePrice, Power, CategoryId FROM Vehicles";
            using var r = cmd.ExecuteReader();
            while (r.Read())
                rows.Add(new VehicleRow(r.GetInt32(0), r.GetString(1), r.GetDouble(2), r.GetInt32(3), r.GetInt32(4)));
            return rows;
        }


        public static Vehicle CreateVehicle(int id, string model, int power, double price, Category category, double? maxLoadTon = null)
        {
            Vehicle v = category.IsCargo
                ? new CargoVehicle(id, model, power, price, maxLoadTon ?? EstimateLoad(power))
                : new PassengerVehicle(id, model, power, price);
            v.AssignCategory(category);
            return v;
        }


        public static double EstimateLoad(int power) => Math.Max(1.0, Math.Round(power / 30.0, 1));

        public List<Vehicle> GetAll()
        {
            var categories = _categories.GetAll();
            var logs = _logs.GetAll();
            var rows = ReadRows();

            return rows
                .Join(categories, row => row.CategoryId, c => c.Id, (row, c) => new { row, c })
                .GroupJoin(logs, x => x.row.Id, l => l.VehicleId, (x, vehicleLogs) =>
                {
                    var v = CreateVehicle(x.row.Id, x.row.Model, x.row.Power, x.row.BasePrice, x.c);
                    foreach (var log in vehicleLogs) v.AddServiceLog(log);
                    return v;
                })
                .OrderBy(v => v.Id)
                .ToList();
        }

        public Vehicle GetById(int id) => GetAll().FirstOrDefault(v => v.Id == id);

        public void Add(Vehicle item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (item.Category == null) throw new InvalidOperationException("У ТС не задана категория.");

            using var con = new SqliteConnection(_cs);
            con.Open();
            using var tx = con.BeginTransaction();

            using (var cmd = con.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = @"INSERT INTO Vehicles (Model, BasePrice, Power, CategoryId)
                                    VALUES ($model, $price, $power, $cat); SELECT last_insert_rowid();";
                cmd.Parameters.AddWithValue("$model", item.Model);
                cmd.Parameters.AddWithValue("$price", item.BasePrice);
                cmd.Parameters.AddWithValue("$power", item.Power);
                cmd.Parameters.AddWithValue("$cat", item.CategoryId);
                item.SetId(Convert.ToInt32(cmd.ExecuteScalar()));  
            }

            foreach (var log in item.ServiceLogs)
                MaintenanceLogRepository.Insert(con, tx, log);

            tx.Commit();
        }

        public bool Delete(int id)
        {
            using var con = new SqliteConnection(_cs);
            con.Open();
            using var tx = con.BeginTransaction();

            using (var delLogs = con.CreateCommand())
            {
                delLogs.Transaction = tx;
                delLogs.CommandText = "DELETE FROM MaintenanceLogs WHERE VehicleId = $id";
                delLogs.Parameters.AddWithValue("$id", id);
                delLogs.ExecuteNonQuery();
            }

            int affected;
            using (var delVehicle = con.CreateCommand())
            {
                delVehicle.Transaction = tx;
                delVehicle.CommandText = "DELETE FROM Vehicles WHERE Id = $id";
                delVehicle.Parameters.AddWithValue("$id", id);
                affected = delVehicle.ExecuteNonQuery();
            }

            tx.Commit();
            return affected > 0;
        }
    }
}
