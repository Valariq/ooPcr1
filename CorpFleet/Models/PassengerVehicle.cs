using CorpFleet.Interfaces;

namespace CorpFleet.Models
{

    public class PassengerVehicle : Vehicle, IExportable
    {
        public const int PremiumPowerThreshold = 250;

        public override string VehicleType => "Пассажирский";

        public bool IsPremium => Power > PremiumPowerThreshold;

        public PassengerVehicle(int id, string model, int power, double price)
            : base(id, model, power, price)
        {
        }

 
        public override double CalculateTax()
        {
            double baseTax = base.CalculateTax();
            return IsPremium ? baseTax * 1.5 : baseTax;
        }


        public string GetExportInfo() =>
            $"EXPORT|PASSENGER|{Id}|{Model}|{Power} hp|premium={IsPremium}|price={BasePrice:F2}|tax={CalculateTax():F2}";
    }
}
