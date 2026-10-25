using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Replate.Domain.Services;

namespace Replate.Desktop;

public sealed class DashboardView : UserControl
{
    private static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");
    internal Grid SummaryCards { get; private set; } = null!;
    internal ComboBox PeriodPicker { get; private set; } = null!;
    internal DashboardView(DemoSession session) => Render(session, 28);

    private void Render(DemoSession session, int days)
    {
        var firstDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1 - days));
        var lastDate = DateOnly.FromDateTime(DateTime.Today);
        var period = new ComboBox { ItemsSource = new[] { "7 hari terakhir", "28 hari terakhir" },
            SelectedIndex = days == 7 ? 0 : 1, MinWidth = 180 };
        PeriodPicker = period;
        period.SelectionChanged += (_, _) => Render(session, period.SelectedIndex == 0 ? 7 : 28);
        var records = session.Restaurant.Menus.SelectMany(m => m.ProductionRecords).Where(r => r.RecordDate >= firstDate && r.RecordDate <= lastDate).ToList();
        var surplus = new SurplusDashboard();
        var finance = new FinancialDashboard();
        var recommendations = new ProductionRecommendationService();
        var header = Ui.Heading("Dashboard restoran", $"{session.Restaurant.Name} · {firstDate:dd MMM}–{lastDate:dd MMM yyyy}", period);

        var summary = new Grid { ColumnSpacing = 16, RowSpacing = 16 };
        summary.Children.Add(MetricCard("Total Produksi", $"{surplus.CalculateTotalProduced(records).ToString("N0", Indonesian)} porsi", "Porsi pada periode terpilih"));
        summary.Children.Add(MetricCard("Surplus", $"{surplus.CalculateTotalSurplus(records).ToString("N0", Indonesian)} porsi",
            $"{surplus.CalculateSurplusRate(records).ToString("0.0", Indonesian)}% dari total produksi"));
        summary.Children.Add(MetricCard("Porsi Terselamatkan", $"{surplus.CalculateRescuedFood(records).ToString("N0", Indonesian)} porsi",
            $"{surplus.CalculateRescueRate(records).ToString("0.0", Indonesian)}% dari surplus"));
        summary.Children.Add(MetricCard("Pendapatan Dipulihkan",
            DemoSession.Rupiah(finance.CalculateRecoveredRevenue(session.Restaurant.Orders.Where(o =>
                DateOnly.FromDateTime(o.OrderedAt.ToLocalTime()) >= firstDate && DateOnly.FromDateTime(o.OrderedAt.ToLocalTime()) <= lastDate))),
            "Dari pesanan selesai"));
        var columns = 0;
        void ArrangeCards(int count)
        {
            if (columns == count) return;
            columns = count;
            summary.ColumnDefinitions = ColumnDefinitions.Parse(count == 4 ? "*,*,*,*" : "*,*");
            summary.RowDefinitions = RowDefinitions.Parse(count == 4 ? "Auto" : "Auto,Auto");
            for (var i = 0; i < summary.Children.Count; i++)
            {
                Grid.SetColumn(summary.Children[i], i % count);
                Grid.SetRow(summary.Children[i], i / count);
            }
        }
        ArrangeCards(2);
        summary.SizeChanged += (_, e) => ArrangeCards(e.NewSize.Width >= 1000 ? 4 : 2);
        SummaryCards = summary;
        var table = Ui.Stack(0, TableRow(true, "Menu", "Produksi", "Surplus", "Diselamatkan", "Terbuang"));
        foreach (var menu in session.Restaurant.Menus)
        {
            var history = records.Where(r => r.MenuId == menu.Id).ToList();
            table.Children.Add(TableRow(false, menu.Name, surplus.CalculateTotalProduced(history).ToString("N0", Indonesian),
                $"{surplus.CalculateTotalSurplus(history).ToString("N0", Indonesian)} ({surplus.CalculateSurplusRate(history).ToString("0.0", Indonesian)}%)",
                surplus.CalculateRescuedFood(history).ToString("N0", Indonesian), surplus.CalculateWastedFood(history.Where(r => r.RecordDate < lastDate)).ToString("N0", Indonesian)));
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
            Summary("Berat terselamatkan", $"{surplus.CalculateRescuedWeight(records).ToString("0.0", Indonesian)} kg"),
            Summary("Nilai terbuang", DemoSession.Rupiah(finance.CalculateEstimatedLoss(records.Where(r => r.RecordDate < lastDate)))),
            Ui.Text("Berat berdasarkan bobot per porsi. Nilai terbuang memakai harga normal menu.", 12, Ui.Muted));
        var body = Ui.Stack(28, header, summary, Ui.Divider(), analysis, recommendationSection, impact,
            Ui.Text("Pendapatan dan porsi terselamatkan bertambah setelah pengambilan diverifikasi. Reservasi belum dihitung sebagai penjualan selesai.", 12, Ui.Muted));
        body.Margin = new Thickness(32);
        Content = new ScrollViewer { Content = body,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
    }

    private static Border MetricCard(string title, string value, string supporting)
    {
        var duration = TimeSpan.FromMilliseconds(180);
        var scale = new ScaleTransform(1, 1)
        {
            Transitions = new Transitions
            {
                new DoubleTransition { Property = ScaleTransform.ScaleXProperty, Duration = duration, Easing = new CubicEaseOut() },
                new DoubleTransition { Property = ScaleTransform.ScaleYProperty, Duration = duration, Easing = new CubicEaseOut() }
            }
        };
        var card = new Border
        {
            Background = Brushes.White,
            BorderBrush = Brush.Parse("#DDE3DA"), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8), Padding = new Thickness(20), MinHeight = 140,
            RenderTransform = scale, RenderTransformOrigin = RelativePoint.Center,
            Cursor = new Cursor(StandardCursorType.Arrow),
            Transitions = new Transitions
            {
                new BrushTransition { Property = Border.BackgroundProperty, Duration = duration, Easing = new CubicEaseOut() }
            },
            Child = Ui.Stack(12, Ui.Text(title, 13, Ui.Muted), Ui.Text(value, 28, Ui.Ink, true),
                Ui.Text(supporting, 12, Ui.Muted))
        };
        card.PointerEntered += (_, _) =>
        {
            card.Background = Brush.Parse("#E0E9DF");
            scale.ScaleX = scale.ScaleY = 1.02;
        };
        card.PointerExited += (_, _) =>
        {
            card.Background = Brushes.White;
            scale.ScaleX = scale.ScaleY = 1;
        };
        return card;
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
