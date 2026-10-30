namespace CorpFleet.Events
{
    public class VehicleEventArgs : EventArgs
    {
        public string Message { get; }
        public double OperationPrice { get; }
        public DateTime Timestamp { get; }

        public VehicleEventArgs(string message, double operationPrice)
        {
            Message = message ?? string.Empty;
            OperationPrice = operationPrice;
            Timestamp = DateTime.Now;
        }
    }
}
