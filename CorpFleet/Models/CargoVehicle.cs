using CorpFleet.Interfaces;

namespace CorpFleet.Models
{

    public class CargoVehicle : Vehicle, IExportable
    {
        public double MaxLoadTon { get; private set; }

        public override string VehicleType => "Грузовой";

        public CargoVehicle(int id, string model, int power, double price, double maxLoadTon)
            : base(id, model, power, price)
        {
            if (maxLoadTon <= 0 || maxLoadTon > 200)
                throw new ArgumentOutOfRangeException(nameof(maxLoadTon), "Грузоподъёмность должна быть в диапазоне 0..200 т.");
            MaxLoadTon = maxLoadTon;
        }

        public override double CalculateTax()
        {
            double baseTax = base.CalculateTax();
            return MaxLoadTon > 10 ? baseTax * 1.2 : baseTax;
        }

        string IExportable.GetExportInfo() =>
            $"EXPORT|CARGO|{Id}|{Model}|{Power} hp|{MaxLoadTon:0.0} t|price={BasePrice:F2}|tax={CalculateTax():F2}";

        public override string ToString() => base.ToString() + $", {MaxLoadTon:0.0} т";
    }
}
