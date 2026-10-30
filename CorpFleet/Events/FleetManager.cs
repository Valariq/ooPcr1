using CorpFleet.Interfaces;
using CorpFleet.Models;

namespace CorpFleet.Events
{

    public class FleetManager
    {
        private readonly IRepository<Vehicle> _repository;
        private List<Vehicle> _vehicles;

        public event Action<object, VehicleEventArgs> StatusChanged;

        public FleetManager(IRepository<Vehicle> repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _vehicles = new List<Vehicle>();

        }

        public IReadOnlyList<Vehicle> Vehicles => _vehicles;


        public void Notify(string message, double operationPrice = 0)
        {
            StatusChanged?.Invoke(this, new VehicleEventArgs(message, operationPrice));
        }

        public void Reload()
        {
            _vehicles = _repository.GetAll();
            Notify($"Реестр загружен из SQLite: {_vehicles.Count} ТС", _vehicles.Sum(v => v.BasePrice));
        }

        public void AddVehicle(Vehicle vehicle)
        {
            _repository.Add(vehicle);
            _vehicles.Add(vehicle);
            Notify($"Добавлено ТС #{vehicle.Id} «{vehicle.Model}»", vehicle.BasePrice);
        }


        public Vehicle CloneVehicle(int id)
        {
            var original = _vehicles.FirstOrDefault(v => v.Id == id)
                           ?? throw new InvalidOperationException($"ТС #{id} не найдено.");

            var clone = (Vehicle)original.Clone();
            _repository.Add(clone);               
            _vehicles.Add(clone);
            Notify($"Клонировано ТС #{original.Id} → #{clone.Id} «{clone.Model}» (логов: {clone.ServiceLogs.Count})",
                clone.BasePrice);
            return clone;
        }

        public bool RemoveVehicle(int id)
        {
            var vehicle = _vehicles.FirstOrDefault(v => v.Id == id);
            bool removed = _repository.Delete(id);
            if (removed && vehicle != null)
            {
                _vehicles.Remove(vehicle);
                Notify($"Удалено ТС #{id} «{vehicle.Model}»", -vehicle.BasePrice);
            }
            return removed;
        }


        public void SortByPrice(bool descending = false)
        {
            _vehicles.Sort();
            if (descending) _vehicles.Reverse();
            Notify($"Реестр отсортирован по BasePrice ({(descending ? "убыв." : "возр.")}) — IComparable");
        }



        public List<Vehicle> FindAll(Predicate<Vehicle> match) => _vehicles.FindAll(match);

        public double Calculate(Func<Vehicle, double> selector, Predicate<Vehicle> filter = null) =>
            _vehicles.Where(v => filter == null || filter(v)).Sum(selector);

        public static Predicate<Vehicle> CreatePriceRange(double min, double max) =>
            v => v.BasePrice >= min && v.BasePrice <= max;


        public static Func<Vehicle, double> CreateOwnershipCost(double yearlyInflation, int years) =>
            v => (v.BasePrice + v.CalculateTax() * years + v.ServiceTotal) * Math.Pow(1 + yearlyInflation, years);
    }
}
