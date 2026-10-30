using System.Text;
using CorpFleet.Interfaces;

namespace CorpFleet.Models
{

    public abstract class Vehicle : IStorable, IComparable<Vehicle>, ICloneable
    {
   
        public int Id { get; private set; }
        public string Model { get; private set; }
        public int Power { get; private set; }
        public double BasePrice { get; private set; }

        public int CategoryId { get; private set; }
        public Category Category { get; private set; }


        public List<MaintenanceRecord> ServiceLogs { get; private set; } = new List<MaintenanceRecord>();


        public abstract string VehicleType { get; }

        protected Vehicle(int id, string model, int power, double price)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Id не может быть отрицательным.");
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Модель не может быть пустой.", nameof(model));
            if (power <= 0 || power > 5000)
                throw new ArgumentOutOfRangeException(nameof(power), "Мощность должна быть в диапазоне 1..5000 л.с.");
            if (price <= 0)
                throw new ArgumentOutOfRangeException(nameof(price), "Базовая стоимость должна быть больше 0.");

            Id = id;
            Model = model.Trim();
            Power = power;
            BasePrice = price;
        }

  
        public void AssignCategory(Category category)
        {
            Category = category ?? throw new ArgumentNullException(nameof(category));
            CategoryId = category.Id;
        }

        public void AddServiceLog(MaintenanceRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            ServiceLogs.Add(record);
        }

        internal void SetId(int id)
        {
            Id = id;
            foreach (var log in ServiceLogs) log.ReassignVehicle(id);
        }


        public virtual double CalculateTax() => BasePrice * (Category?.TaxRate ?? 0);

        public double ServiceTotal => ServiceLogs.Sum(l => l.Cost);


        public string FilePath => Path.Combine(AppContext.BaseDirectory, "export", $"vehicle_{Id}_{Sanitize(Model)}.txt");

        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            var sb = new StringBuilder();
            sb.AppendLine(ToString());

            if (this is IExportable exportable)
                sb.AppendLine(exportable.GetExportInfo());
            sb.AppendLine($"Налог: {CalculateTax():N2}");
            sb.AppendLine("Сервисная история:");
            foreach (var log in ServiceLogs.OrderBy(l => l.LogDate))
                sb.AppendLine("  " + log);

            File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
        }

        private static string Sanitize(string s) =>
            new string(s.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) || ch == ' ' ? '_' : ch).ToArray());

   
        public int CompareTo(Vehicle other)
        {
            if (other is null) return 1;
            return BasePrice.CompareTo(other.BasePrice);
        }


        public object Clone()
        {
            var copy = (Vehicle)MemberwiseClone();         
            copy.Category = (Category)Category?.Clone();     
            copy.ServiceLogs = ServiceLogs                   
                .Select(l => (MaintenanceRecord)l.Clone())
                .ToList();
            copy.CloneExtra(this);
            return copy;
        }

    
        protected virtual void CloneExtra(Vehicle source) { }

        public override string ToString() =>
            $"[{VehicleType}] #{Id} {Model}, {Power} л.с., {BasePrice:N0} ({Category?.CategoryName ?? "без категории"})";


        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            return obj is Vehicle other
                   && string.Equals(Model, other.Model, StringComparison.OrdinalIgnoreCase)
                   && Power == other.Power;
        }

        public override int GetHashCode() =>
            HashCode.Combine(Model?.ToUpperInvariant(), Power);
    }
}
