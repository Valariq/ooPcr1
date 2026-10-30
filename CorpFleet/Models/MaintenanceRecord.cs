namespace CorpFleet.Models
{

    public class MaintenanceRecord : ICloneable
    {
        public const string Online = "Online";
        public const string Offline = "Offline";

        public int Id { get; private set; }
        public int VehicleId { get; private set; }
        public string ServiceName { get; private set; }
        public double Cost { get; private set; }
        public DateTime LogDate { get; private set; }
        public string Channel { get; private set; }

        public MaintenanceRecord(int id, int vehicleId, string serviceName, double cost, DateTime logDate, string channel)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                throw new ArgumentException("Название услуги не может быть пустым.", nameof(serviceName));
            if (cost < 0)
                throw new ArgumentOutOfRangeException(nameof(cost), "Стоимость не может быть отрицательной.");
            if (channel != Online && channel != Offline)
                throw new ArgumentException("Канал должен быть 'Online' или 'Offline'.", nameof(channel));

            Id = id;
            VehicleId = vehicleId;
            ServiceName = serviceName;
            Cost = cost;
            LogDate = logDate;
            Channel = channel;
        }

        internal void SetId(int id) => Id = id;
        internal void ReassignVehicle(int vehicleId) => VehicleId = vehicleId;

   
        public void ChangeCost(double newCost)
        {
            if (newCost < 0) throw new ArgumentOutOfRangeException(nameof(newCost));
            Cost = newCost;
        }

        public object Clone() => new MaintenanceRecord(Id, VehicleId, ServiceName, Cost, LogDate, Channel);

        public override string ToString() => $"{LogDate:dd.MM.yyyy} {ServiceName} [{Channel}] — {Cost:N0}";
    }
}
