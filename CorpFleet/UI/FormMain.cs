using System.ComponentModel;
using CorpFleet.Data;
using CorpFleet.Events;
using CorpFleet.Interfaces;
using CorpFleet.Models;
using CorpFleet.Services;

namespace CorpFleet.UI
{
    
    public class FormMain : Form
    {
 
        private readonly CategoryRepository _categoryRepo;
        private readonly MaintenanceLogRepository _logRepo;
        private readonly FleetManager _manager;
        private FleetAnalyticsService _analytics;
        private List<Category> _categories = new();
        private List<FleetReportDto> _report = new();
        private int _currentPage = 1;


        private TabControl tabControl;
        private ToolStripStatusLabel lblStatus;
        private ToolStripStatusLabel lblEventCount;


        private TextBox txtModel;
        private NumericUpDown numPower, numPrice, numLoad;
        private ComboBox cmbCategory;
        private DataGridView gridRegistry;
        private TextBox txtOutput;
        private NumericUpDown numClosureMin, numClosureMax, numClosurePower, numYears;

   
        private NumericUpDown numFilterMin, numFilterMax;
        private TextBox txtSearch;
        private DataGridView gridReport;
        private Label lblReportCount;


        private RadioButton rbExcept, rbIntersect, rbUnion;
        private ListBox lstSetResult;
        private Label lblSetStats, lblSetDescription;
        private DataGridView gridStats;
        private TextBox txtSummary, txtChecks;
        private NumericUpDown numCheckThreshold;


        private DataGridView gridPage;
        private Label lblPage;
        private NumericUpDown numPageSize;
        private ListBox lstEvents;

        public FormMain(string dbPath)
        {
            _categoryRepo = new CategoryRepository(dbPath);
            _logRepo = new MaintenanceLogRepository(dbPath);
            _manager = new FleetManager(new SqliteVehicleRepository(dbPath));

            Text = "CorpFleet Analytics — управление и аналитика автопарка";
            Width = 1320;
            Height = 820;
            MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);

            BuildUi();
            SubscribeToEvents();

            _categories = _categoryRepo.GetAll();
            cmbCategory.DataSource = _categories;
            _manager.Reload();
            RefreshAll();
        }


        private void SubscribeToEvents()
        {
            _manager.StatusChanged += (sender, e) =>
                lstEvents.Items.Insert(0,
                    $"[{e.Timestamp:HH:mm:ss}] {e.Message}" +
                    (e.OperationPrice != 0 ? $"  | сумма: {e.OperationPrice:N0}" : string.Empty));

        
            _manager.StatusChanged += (sender, e) => lblStatus.Text = e.Message;

        
            int counter = 0;
            _manager.StatusChanged += (sender, e) =>
            {
                counter++;
                lblEventCount.Text = $"Событий StatusChanged: {counter}";
            };
        }

        private void BuildUi()
        {
            tabControl = new TabControl { Dock = DockStyle.Fill };
            tabControl.TabPages.Add(BuildRegistryTab());
            tabControl.TabPages.Add(BuildLinqTab());
            tabControl.TabPages.Add(BuildSetsTab());
            tabControl.TabPages.Add(BuildPagingTab());

            var status = new StatusStrip();
            lblStatus = new ToolStripStatusLabel("Готово") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            lblEventCount = new ToolStripStatusLabel("Событий StatusChanged: 0");
            status.Items.Add(lblStatus);
            status.Items.Add(lblEventCount);

            Controls.Add(tabControl);
            Controls.Add(status);
        }

      
        private TabPage BuildRegistryTab()
        {
            var page = new TabPage("1. Реестр ТС и ООП-Модель");

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(6) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

         
            var left = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true
            };

            var gbAdd = NewGroup("Новое транспортное средство", 300);
            var addLayout = NewFormGrid();
            txtModel = new TextBox { Dock = DockStyle.Fill, PlaceholderText = "напр. Volvo FH 460" };
            numPower = NewNumeric(1, 5000, 150, 0, 10);
            numPrice = NewNumeric(1, 1_000_000_000, 15_000_000, 0, 100_000);
            cmbCategory = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCategory.SelectedIndexChanged += (s, e) =>
                numLoad.Enabled = cmbCategory.SelectedItem is Category c && c.IsCargo;
            numLoad = NewNumeric(0.1m, 200, 10, 1, 0.5m);
            AddRow(addLayout, "Модель:", txtModel);
            AddRow(addLayout, "Мощность, л.с.:", numPower);
            AddRow(addLayout, "Цена:", numPrice);
            AddRow(addLayout, "Категория:", cmbCategory);
            AddRow(addLayout, "Груз-ть, т:", numLoad);
            var btnAdd = NewButton("➕ Добавить в реестр", BtnAdd_Click, 270);
            addLayout.Controls.Add(btnAdd);
            addLayout.SetColumnSpan(btnAdd, 2);
            gbAdd.Controls.Add(addLayout);

            var gbOps = NewGroup("Операции с выбранным ТС", 300);
            var opsFlow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Location = new Point(8, 20) };
            opsFlow.Controls.Add(NewButton("⧉ Глубокое клонирование (ICloneable)", BtnClone_Click, 270));
            opsFlow.Controls.Add(NewButton("↑ Сортировать по цене (IComparable)", (s, e) => SortRegistry(false), 270));
            opsFlow.Controls.Add(NewButton("↓ Сортировать по цене, убыв.", (s, e) => SortRegistry(true), 270));
            opsFlow.Controls.Add(NewButton("⇪ Экспорт (is IExportable)", BtnExport_Click, 270));
            opsFlow.Controls.Add(NewButton("💾 Сохранить в файл (IStorable)", BtnSave_Click, 270));
            opsFlow.Controls.Add(NewButton("= Сравнить 2 выбранных (Equals)", BtnEquals_Click, 270));
            opsFlow.Controls.Add(NewButton("✖ Удалить", BtnDelete_Click, 270));
            gbOps.Controls.Add(opsFlow);

            var gbClosure = NewGroup("Делегаты Predicate / Func и замыкания", 300);
            var clLayout = NewFormGrid();
            numClosureMin = NewNumeric(0, 1_000_000_000, 10_000_000, 0, 1_000_000);
            numClosureMax = NewNumeric(0, 1_000_000_000, 60_000_000, 0, 1_000_000);
            numClosurePower = NewNumeric(0, 5000, 150, 0, 10);
            numYears = NewNumeric(1, 30, 5, 0, 1);
            AddRow(clLayout, "Цена от:", numClosureMin);
            AddRow(clLayout, "Цена до:", numClosureMax);
            AddRow(clLayout, "Мощность ≥:", numClosurePower);
            AddRow(clLayout, "Лет владения:", numYears);
            var btnClosure = NewButton("λ Выполнить расчёт", BtnClosure_Click, 270);
            clLayout.Controls.Add(btnClosure);
            clLayout.SetColumnSpan(btnClosure, 2);
            gbClosure.Controls.Add(clLayout);

            left.Controls.Add(gbAdd);
            left.Controls.Add(gbOps);
            left.Controls.Add(gbClosure);

           
            var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
            gridRegistry = NewGrid();
            gridRegistry.MultiSelect = true;
            txtOutput = new TextBox
            {
                Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9.5F), BackColor = Color.FromArgb(250, 250, 245)
            };
            right.Controls.Add(gridRegistry, 0, 0);
            right.Controls.Add(txtOutput, 0, 1);

            root.Controls.Add(left, 0, 0);
            root.Controls.Add(right, 1, 0);
            page.Controls.Add(root);
            return page;
        }

        private TabPage BuildLinqTab()
        {
            var page = new TabPage("2. LINQ-Аналитика и DTO");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(6) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            numFilterMin = NewNumeric(0, 1_000_000_000, 0, 0, 1_000_000, 130);
            numFilterMax = NewNumeric(0, 1_000_000_000, 100_000_000, 0, 1_000_000, 130);
            txtSearch = new TextBox { Width = 220, PlaceholderText = "модель / категория / тип" };
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            lblReportCount = new Label { AutoSize = true, Padding = new Padding(12, 7, 0, 0), Font = new Font(Font, FontStyle.Bold) };

            bar.Controls.Add(NewBarLabel("Цена от:"));
            bar.Controls.Add(numFilterMin);
            bar.Controls.Add(NewBarLabel("до:"));
            bar.Controls.Add(numFilterMax);
            bar.Controls.Add(NewBarLabel("Поиск:"));
            bar.Controls.Add(txtSearch);
            bar.Controls.Add(NewButton("Применить (Where)", (s, e) => ApplyFilter(), 150));
            bar.Controls.Add(NewButton("Сбросить", (s, e) =>
            {
                numFilterMin.Value = 0;
                numFilterMax.Value = 100_000_000;
                txtSearch.Clear();
                ApplyFilter();
            }, 90));
            bar.Controls.Add(NewButton("⟳ Перечитать БД", (s, e) => { _manager.Reload(); RefreshAll(); }, 130));
            bar.Controls.Add(lblReportCount);

            gridReport = NewGrid();
            root.Controls.Add(bar, 0, 0);
            root.Controls.Add(gridReport, 0, 1);
            page.Controls.Add(root);
            return page;
        }

    
        private TabPage BuildSetsTab()
        {
            var page = new TabPage("3. Группировка, Агрегаты и Множества");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(6) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

   
            var gbSets = new GroupBox { Text = "Множества услуг: Online vs Offline", Dock = DockStyle.Fill };
            var setsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
            setsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            setsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            setsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            setsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var radios = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            rbExcept = new RadioButton { Text = "Except", AutoSize = true, Checked = true, Appearance = Appearance.Button, MinimumSize = new Size(90, 28), TextAlign = ContentAlignment.MiddleCenter };
            rbIntersect = new RadioButton { Text = "Intersect", AutoSize = true, Appearance = Appearance.Button, MinimumSize = new Size(90, 28), TextAlign = ContentAlignment.MiddleCenter };
            rbUnion = new RadioButton { Text = "Union", AutoSize = true, Appearance = Appearance.Button, MinimumSize = new Size(90, 28), TextAlign = ContentAlignment.MiddleCenter };
            foreach (var rb in new[] { rbExcept, rbIntersect, rbUnion })
            {
                rb.CheckedChanged += (s, e) => { if (((RadioButton)s).Checked) RefreshSets(); };
                radios.Controls.Add(rb);
            }

            lblSetDescription = new Label { AutoSize = true, Padding = new Padding(3, 6, 3, 3), Font = new Font(Font, FontStyle.Italic) };
            lstSetResult = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            lblSetStats = new Label { AutoSize = true, Padding = new Padding(3, 6, 3, 3), Font = new Font(Font, FontStyle.Bold) };

            setsLayout.Controls.Add(radios, 0, 0);
            setsLayout.Controls.Add(lblSetDescription, 0, 1);
            setsLayout.Controls.Add(lstSetResult, 0, 2);
            setsLayout.Controls.Add(lblSetStats, 0, 3);
            gbSets.Controls.Add(setsLayout);

            var gbGroup = new GroupBox { Text = "GroupBy по категориям: Count / Sum / Average / Min / Max", Dock = DockStyle.Fill };
            gridStats = NewGrid();
            gbGroup.Controls.Add(gridStats);

          
            var gbSummary = new GroupBox { Text = "Итоговая сводка (Aggregate)", Dock = DockStyle.Fill };
            txtSummary = NewMonoBox();
            gbSummary.Controls.Add(txtSummary);

            var gbChecks = new GroupBox { Text = "Проверки Any / All / FirstOrDefault", Dock = DockStyle.Fill };
            var checksLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
            checksLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            checksLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var checksBar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            numCheckThreshold = NewNumeric(0, 10_000_000, 200_000, 0, 10_000, 120);
            checksBar.Controls.Add(NewBarLabel("Порог «дорогой услуги»:"));
            checksBar.Controls.Add(numCheckThreshold);
            checksBar.Controls.Add(NewButton("Проверить", (s, e) => RefreshChecks(), 100));
            txtChecks = NewMonoBox();
            checksLayout.Controls.Add(checksBar, 0, 0);
            checksLayout.Controls.Add(txtChecks, 0, 1);
            gbChecks.Controls.Add(checksLayout);

            root.Controls.Add(gbSets, 0, 0);
            root.Controls.Add(gbGroup, 1, 0);
            root.Controls.Add(gbChecks, 0, 1);
            root.Controls.Add(gbSummary, 1, 1);
            page.Controls.Add(root);
            return page;
        }

 
        private TabPage BuildPagingTab()
        {
            var page = new TabPage("4. Постраничный просмотр и События");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(6) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var nav = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            lblPage = new Label { AutoSize = true, Padding = new Padding(10, 7, 10, 0), Font = new Font(Font, FontStyle.Bold) };
            numPageSize = NewNumeric(1, 50, 5, 0, 1, 60);
            numPageSize.ValueChanged += (s, e) => { _currentPage = 1; ShowPage(); };

            nav.Controls.Add(NewButton("⏮", (s, e) => GoToPage(1), 40));
            nav.Controls.Add(NewButton("◀ Назад", (s, e) => GoToPage(_currentPage - 1), 80));
            nav.Controls.Add(lblPage);
            nav.Controls.Add(NewButton("Вперёд ▶", (s, e) => GoToPage(_currentPage + 1), 80));
            nav.Controls.Add(NewButton("⏭", (s, e) => GoToPage(int.MaxValue), 40));
            nav.Controls.Add(NewBarLabel("Размер страницы:"));
            nav.Controls.Add(numPageSize);

            var evBar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            evBar.Controls.Add(new Label { Text = "Журнал событий StatusChanged", AutoSize = true, Padding = new Padding(0, 7, 10, 0), Font = new Font(Font, FontStyle.Bold) });
            evBar.Controls.Add(NewButton("Очистить", (s, e) => lstEvents.Items.Clear(), 80));
            evBar.Controls.Add(NewButton("Тест-событие", (s, e) =>
                _manager.Notify("Ручное тестовое событие", _manager.Vehicles.Select(v => v.BasePrice).DefaultIfEmpty(0).Average()), 100));

            gridPage = NewGrid();
            lstEvents = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, HorizontalScrollbar = true, Font = new Font("Consolas", 9F) };

            root.Controls.Add(nav, 0, 0);
            root.Controls.Add(evBar, 1, 0);
            root.Controls.Add(gridPage, 0, 1);
            root.Controls.Add(lstEvents, 1, 1);
            page.Controls.Add(root);
            return page;
        }

        private void RefreshAll()
        {
            _analytics = new FleetAnalyticsService(_manager.Vehicles, _categories, _logRepo.GetAll());
            _report = _analytics.BuildReport();

            RefreshRegistry();
            ApplyFilter(notify: false);
            RefreshSets();
            RefreshGroups();
            RefreshChecks();
            ShowPage(notify: false);
        }

        public class RegistryRow
        {
            [DisplayName("Id")] public int Id { get; init; }
            [DisplayName("Тип")] public string Type { get; init; }
            [DisplayName("Модель")] public string Model { get; init; }
            [DisplayName("л.с.")] public int Power { get; init; }
            [DisplayName("Базовая цена")] public double BasePrice { get; init; }
            [DisplayName("Категория")] public string Category { get; init; }
            [DisplayName("Налог (CalculateTax)")] public double Tax { get; init; }
            [DisplayName("Груз-ть, т")] public string Load { get; init; }
            [DisplayName("Записей ТО")] public int Logs { get; init; }
        }

        private void RefreshRegistry()
        {
            gridRegistry.DataSource = _manager.Vehicles.Select(v => new RegistryRow
            {
                Id = v.Id,
                Type = v.VehicleType,
                Model = v.Model,
                Power = v.Power,
                BasePrice = v.BasePrice,
                Category = v.Category?.CategoryName,
                Tax = Math.Round(v.CalculateTax(), 2),
                Load = v is CargoVehicle cargo ? cargo.MaxLoadTon.ToString("0.0") : "—",
                Logs = v.ServiceLogs.Count
            }).ToList();
        }

        private void ApplyFilter(bool notify = true)
        {
            double? min = numFilterMin.Value > 0 ? (double)numFilterMin.Value : null;
            double? max = (double)numFilterMax.Value;

            var data = FleetAnalyticsService.Sort(
                           FleetAnalyticsService.Filter(_report, min, max, txtSearch.Text))
                       .ToList();

            gridReport.DataSource = data;
            lblReportCount.Text = $"Найдено: {data.Count} из {_report.Count} | Σ цен: {data.Sum(d => d.BasePrice):N0}";
            if (notify)
                _manager.Notify($"LINQ-фильтр: {data.Count} записей (поиск: «{txtSearch.Text}»)", data.Sum(d => d.TotalCost));
        }

        private void RefreshSets()
        {
            if (_analytics == null) return;
            var op = rbIntersect.Checked ? SetOperation.Intersect
                   : rbUnion.Checked ? SetOperation.Union
                   : SetOperation.Except;

            var result = _analytics.RunSetOperation(op);
            lblSetDescription.Text = result.Description;
            lstSetResult.DataSource = result.ServiceNames;
            lblSetStats.Text = $"Услуг: {result.ServiceNames.Count} | записей: {result.LogCount}\n" +
                               $"Sum = {result.Sum:N0}   Average = {result.Average:N2}";
            _manager.Notify($"Множества: {op} → {result.ServiceNames.Count} услуг", result.Sum);
        }

        private void RefreshGroups()
        {
            var stats = _analytics.GroupByCategory(_report);
            gridStats.DataSource = stats;
            txtSummary.Text = _analytics.BuildSummary(stats);
        }

        private void RefreshChecks()
        {
            if (_analytics == null) return;
            txtChecks.Lines = _analytics.RunChecks((double)numCheckThreshold.Value).ToArray();
        }

        private void GoToPage(int page)
        {
            _currentPage = page;
            ShowPage();
        }

        private void ShowPage(bool notify = true)
        {
            var sorted = FleetAnalyticsService.Sort(_report).ToList();
            var result = FleetAnalyticsService.Page(sorted, _currentPage, (int)numPageSize.Value);
            _currentPage = result.Page;

            gridPage.DataSource = result.Items;
            lblPage.Text = $"Страница {result.Page} из {result.TotalPages}  (всего {result.TotalItems})";
            if (notify)
                _manager.Notify($"Пагинация: Skip({(result.Page - 1) * result.PageSize}).Take({result.PageSize}) → стр. {result.Page}/{result.TotalPages}",
                    result.Items.Sum(i => i.BasePrice));
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (cmbCategory.SelectedItem is not Category category)
                    throw new InvalidOperationException("Выберите категорию.");

                var vehicle = SqliteVehicleRepository.CreateVehicle(
                    0, txtModel.Text, (int)numPower.Value, (double)numPrice.Value, category, (double)numLoad.Value);

                if (_manager.Vehicles.Contains(vehicle)) 
                    Output($"⚠ В реестре уже есть ТС с такой же моделью и мощностью (Equals = true). Всё равно добавляю.");

                _manager.AddVehicle(vehicle);
                Output($"Добавлено: {vehicle}");
                txtModel.Clear();
                RefreshAll();
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                MessageBox.Show(ex.Message, "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnClone_Click(object sender, EventArgs e)
        {
            var original = SelectedVehicles().FirstOrDefault();
            if (original == null) { Output("Выберите ТС в таблице."); return; }

            var clone = _manager.CloneVehicle(original.Id);

            Output("=== Глубокое клонирование ===");
            Output($"Оригинал: {original}");
            Output($"Клон:     {clone}");
            Output($"ReferenceEquals(оригинал, клон)            = {ReferenceEquals(original, clone)}");
            Output($"оригинал.Equals(клон) [Model+Power]        = {original.Equals(clone)}");
            Output($"GetHashCode: {original.GetHashCode()} / {clone.GetHashCode()}");
            Output($"ReferenceEquals(ServiceLogs)               = {ReferenceEquals(original.ServiceLogs, clone.ServiceLogs)}");
            Output($"ReferenceEquals(Category)                  = {ReferenceEquals(original.Category, clone.Category)}");

            var probe = (Vehicle)original.Clone();
            if (probe.ServiceLogs.Count > 0)
            {
                double before = original.ServiceLogs[0].Cost;
                probe.ServiceLogs[0].ChangeCost(0);
                Output($"Изменили стоимость 1-го лога у копии на 0 → у оригинала осталось {original.ServiceLogs[0].Cost:N0} " +
                       $"(было {before:N0}) — копия глубокая.");
            }
            Output("");
            RefreshAll();
        }

        private void SortRegistry(bool descending)
        {
            _manager.SortByPrice(descending);
            RefreshRegistry();
            Output($"Реестр отсортирован List<Vehicle>.Sort() через IComparable<Vehicle>.CompareTo (по BasePrice{(descending ? ", убыв." : "")}).");
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            var vehicles = SelectedVehicles();
            if (vehicles.Count == 0) { Output("Выберите ТС в таблице."); return; }

            Output("=== Экспорт через паттерн-матчинг ===");
            foreach (var v in vehicles)
            {
                if (v is IExportable exportable)
                    Output(exportable.GetExportInfo());

                string note = v switch
                {
                    CargoVehicle c => $"  ↳ CargoVehicle: явная реализация, c.GetExportInfo() не компилируется; MaxLoadTon = {c.MaxLoadTon:0.0} т",
                    PassengerVehicle p => $"  ↳ PassengerVehicle: неявная реализация, p.GetExportInfo() доступен напрямую; премиум = {p.IsPremium}",
                    _ => "  ↳ неизвестный тип"
                };
                Output(note);
            }
            Output("");
            _manager.Notify($"Экспорт {vehicles.Count} ТС", vehicles.Sum(v => v.BasePrice));
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            var vehicles = SelectedVehicles();
            if (vehicles.Count == 0) { Output("Выберите ТС в таблице."); return; }

            foreach (IStorable storable in vehicles)
            {
                storable.Save();
                Output($"Сохранено: {storable.FilePath}");
            }
            _manager.Notify($"IStorable.Save(): сохранено файлов — {vehicles.Count}");
        }

        private void BtnEquals_Click(object sender, EventArgs e)
        {
            var sel = SelectedVehicles();
            if (sel.Count != 2) { Output("Выделите ровно две строки (Ctrl + клик)."); return; }

            var (a, b) = (sel[0], sel[1]);
            Output("=== Сравнение System.Object ===");
            Output($"A: {a}");
            Output($"B: {b}");
            Output($"A.Equals(B) = {a.Equals(b)}   (правило: совпадают Model и Power)");
            Output($"HashCode: {a.GetHashCode()} / {b.GetHashCode()}");
            Output($"A.CompareTo(B) = {a.CompareTo(b)}   (по BasePrice)");
            Output("");
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var sel = SelectedVehicles();
            if (sel.Count == 0) { Output("Выберите ТС в таблице."); return; }
            if (MessageBox.Show($"Удалить выбранные ТС ({sel.Count}) из базы?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            foreach (var v in sel)
                Output(_manager.RemoveVehicle(v.Id) ? $"Удалено: {v}" : $"Не удалось удалить #{v.Id}");
            RefreshAll();
        }

        private void BtnClosure_Click(object sender, EventArgs e)
        {
         
            double minPrice = (double)numClosureMin.Value;
            double maxPrice = (double)numClosureMax.Value;
            int minPower = (int)numClosurePower.Value;
            int years = (int)numYears.Value;
            int evaluated = 0;                                   

            Predicate<Vehicle> inRange = FleetManager.CreatePriceRange(minPrice, maxPrice);
            Predicate<Vehicle> powerful = v => v.Power >= minPower;
            Predicate<Vehicle> combined = v => inRange(v) && powerful(v);

            Func<Vehicle, double> ownership = FleetManager.CreateOwnershipCost(0.08, years);
            Func<Vehicle, double> countingOwnership = v => { evaluated++; return ownership(v); };
            Func<Vehicle, string> describe = v => $"{v.Model,-20} {v.BasePrice,14:N0} → за {years} л.: {ownership(v),16:N0}";

            List<Vehicle> found = _manager.FindAll(combined);
            double total = _manager.Calculate(countingOwnership, combined);

            Output($"=== Predicate/Func: цена {minPrice:N0}..{maxPrice:N0}, мощность ≥ {minPower}, {years} лет, инфляция 8% ===");
            found.ForEach(v => Output("  " + describe(v)));
            Output($"Найдено: {found.Count}; лямбда countingOwnership вызвана {evaluated} раз (захваченный счётчик).");
            Output($"Совокупная стоимость владения: {total:N0}");

      
            var perCategory = new List<Func<string>>();
            foreach (var category in _categories)
            {
                perCategory.Add(() =>
                    $"  {category.CategoryName,-14}: {found.Count(v => v.CategoryId == category.Id)} ТС, " +
                    $"налог {found.Where(v => v.CategoryId == category.Id).Sum(v => v.CalculateTax()):N0}");
            }
            Output("Разбивка (список Func<string>, созданных в цикле):");
            perCategory.ForEach(f => Output(f()));
            Output("");

            _manager.Notify($"λ-расчёт: {found.Count} ТС подходят под условия", total);
        }

   
        private List<Vehicle> SelectedVehicles()
        {
            var ids = gridRegistry.SelectedRows.Cast<DataGridViewRow>()
                .Select(r => r.DataBoundItem as RegistryRow)
                .Where(r => r != null)
                .Select(r => r.Id)
                .ToList();
            if (ids.Count == 0 && gridRegistry.CurrentRow?.DataBoundItem is RegistryRow current)
                ids.Add(current.Id);

            return _manager.Vehicles.Where(v => ids.Contains(v.Id)).ToList();
        }

        private void Output(string line) => txtOutput.AppendText(line + Environment.NewLine);

        private static DataGridView NewGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = SystemColors.Window,
                AlternatingRowsDefaultCellStyle = { BackColor = Color.FromArgb(244, 247, 252) }
            };
            grid.DataBindingComplete += (s, e) =>
            {
                foreach (DataGridViewColumn col in grid.Columns)
                {
                    if (col.ValueType == typeof(double))
                    {
                        col.DefaultCellStyle.Format = col.DataPropertyName == "TaxRate" ? "P0" : "N0";
                        col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }
                }
            };
            return grid;
        }

        private static GroupBox NewGroup(string text, int width) =>
            new GroupBox { Text = text, Width = width, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(8) };

        private static TableLayoutPanel NewFormGrid()
        {
            var t = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Location = new Point(8, 20) };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            return t;
        }

        private static void AddRow(TableLayoutPanel t, string label, Control control)
        {
            t.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 4, 0, 0) });
            control.Dock = DockStyle.Fill;
            t.Controls.Add(control);
        }

        private static NumericUpDown NewNumeric(decimal min, decimal max, decimal value, int decimals, decimal increment, int width = 120) =>
            new NumericUpDown
            {
                Minimum = min, Maximum = max, Value = value, DecimalPlaces = decimals,
                Increment = increment, ThousandsSeparator = true, Width = width
            };

        private static Button NewButton(string text, EventHandler onClick, int width)
        {
            var b = new Button { Text = text, Width = width, Height = 28, TextAlign = ContentAlignment.MiddleLeft };
            b.Click += onClick;
            return b;
        }

        private static Label NewBarLabel(string text) =>
            new Label { Text = text, AutoSize = true, Padding = new Padding(6, 7, 0, 0) };

        private static TextBox NewMonoBox() =>
            new TextBox
            {
                Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both,
                WordWrap = false, Font = new Font("Consolas", 9.5F), BackColor = Color.FromArgb(250, 250, 245)
            };
    }
}
