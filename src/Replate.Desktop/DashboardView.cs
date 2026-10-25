using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Replate.Domain.Services;

namespace Replate.Desktop;

public sealed class DashboardView : UserControl
{
    internal DashboardView(DemoSession session) => Render(session, 28);

    private void Render(DemoSession session, int days)
    {
        var firstDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1 - days));
        var lastDate = DateOnly.FromDateTime(DateTime.Today);
        var period = new ComboBox { ItemsSource = new[] { "7 hari terakhir", "28 hari terakhir" },
            SelectedIndex = days == 7 ? 0 : 1, MinWidth = 180 };
        period.SelectionChanged += (_, _) => Render(session, period.SelectedIndex == 0 ? 7 : 28);
        var records = session.Restaurant.Menus.SelectMany(m => m.ProductionRecords).Where(r => r.RecordDate >= firstDate && r.RecordDate <= lastDate).ToList();
        var surplus = new SurplusDashboard();
        var finance = new FinancialDashboard();
        var recommendations = new ProductionRecommendationService();
        var header = Ui.Heading("Dashboard restoran", $"{session.Restaurant.Name} · {firstDate:dd MMM}–{lastDate:dd MMM yyyy}", period);

        var production = Ui.Stack(12,
            Summary("Produksi", $"{surplus.CalculateTotalProduced(records):N0} porsi"),
            Summary("Surplus", $"{surplus.CalculateTotalSurplus(records):N0} porsi ({surplus.CalculateSurplusRate(records):0.0}%)"));
        var recovered = Ui.Stack(12,
            Summary("Terselamatkan", $"{surplus.CalculateRescuedFood(records):N0} porsi ({surplus.CalculateRescueRate(records):0.0}%)"),
            Summary("Pendapatan dipulihkan", DemoSession.Rupiah(finance.CalculateRecoveredRevenue(session.Restaurant.Orders.Where(o => DateOnly.FromDateTime(o.OrderedAt.ToLocalTime()) >= firstDate && DateOnly.FromDateTime(o.OrderedAt.ToLocalTime()) <= lastDate)))));
        var summary = Ui.Stack(12, production, recovered);

        var table = Ui.Stack(0, TableRow(true, "Menu", "Produksi", "Surplus", "Diselamatkan", "Terbuang"));
        foreach (var menu in session.Restaurant.Menus)
        {
            var history = records.Where(r => r.MenuId == menu.Id).ToList();
            table.Children.Add(TableRow(false, menu.Name, surplus.CalculateTotalProduced(history).ToString("N0"),
                $"{surplus.CalculateTotalSurplus(history):N0} ({surplus.CalculateSurplusRate(history):0.0}%)",
                surplus.CalculateRescuedFood(history).ToString("N0"), surplus.CalculateWastedFood(history.Where(r => r.RecordDate < lastDate)).ToString("N0")));
        }
        var analysis = Ui.Stack(12, Ui.Text("Surplus per menu", 20, bold: true), table,
            Ui.Text("Dalam porsi. Terbuang dihitung dari hari yang sudah berakhir; surplus hari ini masih dapat diselamatkan.", 12, Ui.Muted));

        var recommendationTable = Ui.Stack(0,
            RecommendationRow(true, "Menu", "Produksi biasa", "Rekomendasi", "Pengurangan"));
        foreach (var menu in session.Restaurant.Menus)
        {
            var history = records.Where(r => r.MenuId == menu.Id).ToList();
            if (history.Count == 0)
            {
                recommendationTable.Children.Add(RecommendationRow(false, menu.Name, "Belum ada data", "—", "—"));
                continue;
            }
            var periodMenu = new Replate.Domain.Entities.Menu { Id = menu.Id, Name = menu.Name };
            periodMenu.ProductionRecords.AddRange(history);
            var recommendation = recommendations.GenerateRecommendation(periodMenu, 2);
            recommendationTable.Children.Add(RecommendationRow(false, menu.Name,
                $"{recommendation.TypicalProduction} porsi", $"{recommendation.RecommendedProduction} porsi",
                $"{recommendation.PotentialSurplusReduction} porsi"));
        }
        var recommendationSection = Ui.Stack(12, Ui.Text("Rekomendasi produksi", 20, bold: true),
            Ui.Text($"Rata-rata penjualan reguler {days} hari + buffer 2 porsi per menu.", 13, Ui.Muted),
            recommendationTable, Ui.Text("Pengurangan menunjukkan selisih dari produksi biasa. Rekomendasi bukan jaminan permintaan.", 12, Ui.Muted));

        var impact = Ui.Stack(12, Ui.Text("Berat dan nilai makanan", 20, bold: true),
            Summary("Berat terselamatkan", $"{surplus.CalculateRescuedWeight(records):0.0} kg"),
            Summary("Nilai terbuang", DemoSession.Rupiah(finance.CalculateEstimatedLoss(records.Where(r => r.RecordDate < lastDate)))),
            Ui.Text("Berat berdasarkan bobot per porsi. Nilai terbuang memakai harga normal menu.", 12, Ui.Muted));
        var body = Ui.Stack(28, header, summary, Ui.Divider(), analysis, recommendationSection, impact,
            Ui.Text("Pendapatan dan porsi terselamatkan bertambah setelah pengambilan diverifikasi. Reservasi belum dihitung sebagai penjualan selesai.", 12, Ui.Muted));
        body.Margin = new Thickness(32);
        Content = new ScrollViewer { Content = body,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    }

    private static Grid Summary(string label, string value) =>
        Ui.Columns("*,Auto", Ui.Text(label, 13, Ui.Muted), Ui.Text(value, 14, bold: true));

    private static Border TableRow(bool header, params string[] cells) =>
        DataRow(header, "1.6*,*,1.25*,1.1*,*", -1, cells);

    private static Border RecommendationRow(bool header, params string[] cells) =>
        DataRow(header, "1.6*,*,1.25*,1.1*", 2, cells);

    private static Border DataRow(bool header, string columns, int emphasis, string[] cells)
    {
        var grid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse(columns), MinHeight = header ? 36 : 48 };
        for (var i = 0; i < cells.Length; i++)
        {
            var text = Ui.Text(cells[i], header ? 12 : 14,
                header ? Ui.Muted : i == emphasis ? Ui.Green : Ui.Ink, !header && (i == 0 || i == emphasis));
            text.Margin = new Thickness(i == 0 ? 0 : 12, 8, 0, 8);
            text.HorizontalAlignment = i == 0 ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            Grid.SetColumn(text, i); grid.Children.Add(text);
        }
        return new Border { Child = grid, BorderBrush = Brush.Parse("#DDE3DA"),
            BorderThickness = new Thickness(0, 0, 0, 1) };
    }
}
