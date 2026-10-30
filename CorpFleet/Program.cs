using CorpFleet.Data;
using CorpFleet.UI;

namespace CorpFleet
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();

            string dbPath = Path.Combine(AppContext.BaseDirectory, "corp_fleet.db");
            string seedPath = Path.Combine(AppContext.BaseDirectory, "Data", "corp_fleet_seed.sql");

            try
            {
                DatabaseInitializer.EnsureCreated(dbPath, seedPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось подготовить базу данных:\n" + ex.Message,
                    "CorpFleet Analytics", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Application.Run(new FormMain(dbPath));
        }
    }
}
