using System.ComponentModel;

namespace CorpFleet.Services
{
    public class FleetReportDto
    {
        [DisplayName("Id")] public int VehicleId { get; init; }
        [DisplayName("Модель")] public string Model { get; init; }
        [DisplayName("Тип")] public string VehicleType { get; init; }
        [DisplayName("Категория")] public string CategoryName { get; init; }
        [DisplayName("л.с.")] public int Power { get; init; }
        [DisplayName("Базовая цена")] public double BasePrice { get; init; }
        [DisplayName("Ставка")] public double TaxRate { get; init; }
        [DisplayName("Налог")] public double Tax { get; init; }
        [DisplayName("Кол-во ТО")] public int ServiceCount { get; init; }
        [DisplayName("Стоимость ТО")] public double ServiceCost { get; init; }
        [DisplayName("Online ТО")] public double OnlineServiceCost { get; init; }
        [DisplayName("Последнее ТО")] public string LastService { get; init; }
        [DisplayName("Итого владение")] public double TotalCost { get; init; }
    }

    public class CategoryStatsDto
    {
        [DisplayName("Категория")] public string CategoryName { get; init; }
        [DisplayName("Кол-во ТС")] public int Count { get; init; }
        [DisplayName("Сумма цен")] public double TotalPrice { get; init; }
        [DisplayName("Средняя цена")] public double AveragePrice { get; init; }
        [DisplayName("Мин. цена")] public double MinPrice { get; init; }
        [DisplayName("Макс. цена")] public double MaxPrice { get; init; }
        [DisplayName("Налоги")] public double TotalTax { get; init; }
        [DisplayName("Затраты на ТО")] public double TotalService { get; init; }
    }

    public enum SetOperation { Except, Intersect, Union }

    public class SetOperationResult
    {
        public SetOperation Operation { get; init; }
        public string Description { get; init; }
        public List<string> ServiceNames { get; init; }
        public int LogCount { get; init; }
        public double Sum { get; init; }
        public double Average { get; init; }
    }

    public class PageResult<T>
    {
        public List<T> Items { get; init; }
        public int Page { get; init; }
        public int PageSize { get; init; }
        public int TotalItems { get; init; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    }
}
