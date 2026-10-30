using System.Text;
using CorpFleet.Models;

namespace CorpFleet.Services
{
    public class FleetAnalyticsService
    {
        private readonly IReadOnlyList<Vehicle> _vehicles;
        private readonly List<Category> _categories;
        private readonly List<MaintenanceRecord> _logs;

        public FleetAnalyticsService(IReadOnlyList<Vehicle> vehicles, List<Category> categories, List<MaintenanceRecord> logs)
        {
            _vehicles = vehicles;
            _categories = categories;
            _logs = logs;
        }

 

        public List<FleetReportDto> BuildReport()
        {
            return _vehicles
                .Join(_categories,
                      v => v.CategoryId,
                      c => c.Id,
                      (v, c) => new { Vehicle = v, Category = c })
                .GroupJoin(_logs,
                      vc => vc.Vehicle.Id,
                      l => l.VehicleId,
                      (vc, logs) => new { vc.Vehicle, vc.Category, Logs = logs.ToList() })
                .Select(x =>
                {
                    double tax = x.Vehicle.CalculateTax();
                    double serviceCost = x.Logs.Sum(l => l.Cost);
                    return new FleetReportDto
                    {
                        VehicleId = x.Vehicle.Id,
                        Model = x.Vehicle.Model,
                        VehicleType = x.Vehicle.VehicleType,
                        CategoryName = x.Category.CategoryName,
                        Power = x.Vehicle.Power,
                        BasePrice = x.Vehicle.BasePrice,
                        TaxRate = x.Category.TaxRate,
                        Tax = Math.Round(tax, 2),
                        ServiceCount = x.Logs.Count,
                        ServiceCost = serviceCost,
                        OnlineServiceCost = x.Logs.Where(l => l.Channel == MaintenanceRecord.Online).Sum(l => l.Cost),
                        LastService = x.Logs.Any() ? x.Logs.Max(l => l.LogDate).ToString("dd.MM.yyyy") : "—",
                        TotalCost = Math.Round(x.Vehicle.BasePrice + tax + serviceCost, 2)
                    };
                })
                .ToList();
        }


        public static IEnumerable<FleetReportDto> Filter(IEnumerable<FleetReportDto> source,
            double? minPrice, double? maxPrice, string searchText)
        {
            string search = searchText?.Trim();
            return source
                .Where(d => !minPrice.HasValue || d.BasePrice >= minPrice.Value)
                .Where(d => !maxPrice.HasValue || d.BasePrice <= maxPrice.Value)
                .Where(d => string.IsNullOrEmpty(search)
                            || d.Model.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || d.CategoryName.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || d.VehicleType.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<FleetReportDto> Sort(IEnumerable<FleetReportDto> source) =>
            source.OrderBy(v => v.CategoryName).ThenByDescending(v => v.BasePrice);

        public static PageResult<T> Page<T>(IEnumerable<T> source, int page, int pageSize)
        {
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
            var list = source as IList<T> ?? source.ToList();
            int totalPages = Math.Max(1, (int)Math.Ceiling(list.Count / (double)pageSize));
            page = Math.Clamp(page, 1, totalPages);

            return new PageResult<T>
            {
                Items = list.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = list.Count
            };
        }


        public List<CategoryStatsDto> GroupByCategory(IEnumerable<FleetReportDto> report) =>
            report
                .GroupBy(d => d.CategoryName)
                .Select(g => new CategoryStatsDto
                {
                    CategoryName = g.Key,
                    Count = g.Count(),
                    TotalPrice = g.Sum(d => d.BasePrice),
                    AveragePrice = Math.Round(g.Average(d => d.BasePrice), 2),
                    MinPrice = g.Min(d => d.BasePrice),
                    MaxPrice = g.Max(d => d.BasePrice),
                    TotalTax = Math.Round(g.Sum(d => d.Tax), 2),
                    TotalService = g.Sum(d => d.ServiceCost)
                })
                .OrderByDescending(s => s.TotalPrice)
                .ToList();

        public string BuildSummary(List<CategoryStatsDto> stats)
        {
            if (stats.Count == 0) return "Реестр пуст — сводка недоступна.";
            return stats.Aggregate(
                new StringBuilder()
                    .AppendLine("СВОДКА ПРЕДПРИЯТИЯ «CorpFleet Analytics»")
                    .AppendLine(new string('=', 46)),
                (sb, s) => sb.AppendLine(
                    $"{s.CategoryName,-14} | ТС: {s.Count,2} | Σ {s.TotalPrice,16:N0} | ср. {s.AveragePrice,14:N0}"),
                sb => sb
                    .AppendLine(new string('-', 46))
                    .AppendLine($"Всего ТС: {stats.Sum(s => s.Count)}")
                    .AppendLine($"Стоимость парка: {stats.Sum(s => s.TotalPrice):N0}")
                    .AppendLine($"Налоговая нагрузка: {stats.Sum(s => s.TotalTax):N0}")
                    .AppendLine($"Затраты на обслуживание: {stats.Sum(s => s.TotalService):N0}")
                    .AppendLine($"Самая дорогая категория: {stats.Aggregate((a, b) => a.TotalPrice >= b.TotalPrice ? a : b).CategoryName}")
                    .ToString());
        }

   

        public IEnumerable<string> OnlineServices =>
            _logs.Where(l => l.Channel == MaintenanceRecord.Online).Select(l => l.ServiceName).Distinct();

        public IEnumerable<string> OfflineServices =>
            _logs.Where(l => l.Channel == MaintenanceRecord.Offline).Select(l => l.ServiceName).Distinct();

        public SetOperationResult RunSetOperation(SetOperation operation)
        {
            IEnumerable<string> names;
            IEnumerable<MaintenanceRecord> relevantLogs;
            string description;

            switch (operation)
            {
                case SetOperation.Except:
                    names = OnlineServices.Except(OfflineServices);
                    description = "Услуги, оказываемые ТОЛЬКО онлайн (Online \\ Offline)";
                    break;
                case SetOperation.Intersect:
                    names = OnlineServices.Intersect(OfflineServices);
                    description = "Услуги, доступные в ОБОИХ каналах (Online ∩ Offline)";
                    break;
                default:
                    names = OnlineServices.Union(OfflineServices);
                    description = "Все уникальные услуги (Online ∪ Offline)";
                    break;
            }

            var nameList = names.OrderBy(n => n).ToList();
            relevantLogs = _logs.Where(l => nameList.Contains(l.ServiceName));
            if (operation == SetOperation.Except)
                relevantLogs = relevantLogs.Where(l => l.Channel == MaintenanceRecord.Online);

            var logList = relevantLogs.ToList();
            return new SetOperationResult
            {
                Operation = operation,
                Description = description,
                ServiceNames = nameList,
                LogCount = logList.Count,
                Sum = logList.Sum(l => l.Cost),
                Average = logList.Any() ? Math.Round(logList.Average(l => l.Cost), 2) : 0
            };
        }

   
        public (List<int> both, List<int> onlyOffline) VehicleChannelSets()
        {
            var onlineIds = _logs.Where(l => l.Channel == MaintenanceRecord.Online).Select(l => l.VehicleId);
            var offlineIds = _logs.Where(l => l.Channel == MaintenanceRecord.Offline).Select(l => l.VehicleId);
            return (onlineIds.Intersect(offlineIds).OrderBy(i => i).ToList(),
                    offlineIds.Except(onlineIds).OrderBy(i => i).ToList());
        }

    

        public List<string> RunChecks(double expensiveServiceThreshold)
        {
            var result = new List<string>();

            bool anyExpensive = _logs.Any(l => l.Cost > expensiveServiceThreshold);
            result.Add($"Any: есть ли услуги дороже {expensiveServiceThreshold:N0}? → {(anyExpensive ? "ДА" : "НЕТ")}");

            bool allPositive = _vehicles.All(v => v.BasePrice > 0 && v.Power > 0);
            result.Add($"All: у всех ТС цена и мощность > 0? → {(allPositive ? "ДА" : "НЕТ")}");

            bool allServiced = _vehicles.All(v => _logs.Any(l => l.VehicleId == v.Id));
            result.Add($"All+Any: каждое ТС проходило ТО хотя бы раз? → {(allServiced ? "ДА" : "НЕТ")}");

            bool anyNoLogs = _vehicles.Any(v => !v.ServiceLogs.Any());
            result.Add($"Any: есть ТС без сервисной истории? → {(anyNoLogs ? "ДА" : "НЕТ")}");

            var firstOnlineExpensive = _logs
                .Where(l => l.Channel == MaintenanceRecord.Online)
                .OrderBy(l => l.LogDate)
                .FirstOrDefault(l => l.Cost > expensiveServiceThreshold / 5);
            result.Add("FirstOrDefault: первая онлайн-услуга дороже " +
                       $"{expensiveServiceThreshold / 5:N0} → {(firstOnlineExpensive?.ToString() ?? "не найдено")}");

            var missing = _vehicles.FirstOrDefault(v => v.Model == "Несуществующая модель");
            result.Add($"FirstOrDefault (нет совпадений) → {(missing == null ? "null" : missing.ToString())}");

            var (both, onlyOffline) = VehicleChannelSets();
            result.Add($"Intersect по ТС: обслуживались в обоих каналах → {both.Count} ТС [{string.Join(", ", both)}]");
            result.Add($"Except по ТС: только офлайн → {onlyOffline.Count} ТС [{string.Join(", ", onlyOffline)}]");

            return result;
        }
    }
}
