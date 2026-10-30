namespace CorpFleet.Models
{
    
    public class Category : ICloneable
    {
        public int Id { get; private set; }
        public string CategoryName { get; private set; }
        public double TaxRate { get; private set; }

        public Category(int id, string categoryName, double taxRate)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentException("Название категории не может быть пустым.", nameof(categoryName));
            if (taxRate < 0 || taxRate > 1)
                throw new ArgumentOutOfRangeException(nameof(taxRate), "Налоговая ставка должна быть в диапазоне 0..1.");

            Id = id;
            CategoryName = categoryName.Trim();
            TaxRate = taxRate;
        }

        public bool IsCargo => CategoryName == "Грузовой" || CategoryName == "Спецтехника";

        public object Clone() => new Category(Id, CategoryName, TaxRate);

        public override string ToString() => $"{CategoryName} ({TaxRate:P0})";
    }
}
