using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Replate.Domain.Enums;

namespace Replate.Desktop;

internal static class DashboardVerification
{
    internal static async Task Run(string output)
    {
        void Ensure(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        async Task Settle() => await Task.Delay(350);
        var culture = CultureInfo.GetCultureInfo("id-ID");
        var session = new DemoSession(); // Only isolated in-memory demonstration data.
        // Pending revenue must never enter the recovered-revenue card.
        session.Restaurant.Orders.Add(new Replate.Domain.Entities.Order { TotalPrice = 999999999, OrderedAt = DateTime.UtcNow });
        var window = new MainWindow(session) { Width = 1600, Height = 940 };
        try
        {
            window.Show(); window.ShowDashboard(); await Settle();
            string[] Values(DashboardView view) => view.SummaryCards.Children.Cast<Border>()
                .Select(card => ((StackPanel)card.Child!).Children.OfType<TextBlock>().ElementAt(1).Text!).ToArray();
            void CheckData(int days)
            {
                var view = (DashboardView)window.CurrentPage!;
                var first = DateOnly.FromDateTime(DateTime.Today.AddDays(1 - days));
                var last = DateOnly.FromDateTime(DateTime.Today);
                var records = session.Restaurant.Menus.SelectMany(m => m.ProductionRecords).Where(r => r.RecordDate >= first && r.RecordDate <= last).ToList();
                var produced = records.Sum(r => r.ProducedQuantity);
                var surplus = records.Sum(r => Math.Max(0, r.ProducedQuantity - r.RegularSoldQuantity));
                var rescued = records.Sum(r => r.RescuedQuantity);
                var revenue = session.Restaurant.Orders.Where(o => o.Status == OrderStatus.Completed &&
                    DateOnly.FromDateTime(o.OrderedAt.ToLocalTime()) >= first && DateOnly.FromDateTime(o.OrderedAt.ToLocalTime()) <= last).Sum(o => o.TotalPrice);
                var expected = new[] { produced.ToString("N0", culture) + " porsi", surplus.ToString("N0", culture) + " porsi",
                    rescued.ToString("N0", culture) + " porsi", DemoSession.Rupiah(revenue) };
                Ensure(Values(view).SequenceEqual(expected), "All four cards follow selected period and completed revenue only");
                var supporting = view.SummaryCards.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
                Ensure(supporting.Contains((produced == 0 ? 0 : (decimal)surplus / produced * 100).ToString("0.0", culture) + "% dari total produksi") &&
                    supporting.Contains((surplus == 0 ? 0 : (decimal)rescued / surplus * 100).ToString("0.0", culture) + "% dari surplus"), "Percentages retained with zero-safe calculations");
                Ensure(view.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Text == "Total Produksi") == 1 &&
                    view.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Text == "Pendapatan Dipulihkan") == 1, "Summary is not duplicated");
                Ensure(view.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Surplus per menu") &&
                    view.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Rekomendasi produksi"), "Detail and recommendations preserved");
            }
            void CheckLayout(int columns)
            {
                var view = (DashboardView)window.CurrentPage!;
                var grid = view.SummaryCards;
                Ensure(grid.ColumnDefinitions.Count == columns, "Responsive column count");
                var cards = grid.Children.Cast<Border>().ToList();
                for (var i = 0; i < cards.Count; i++)
                {
                    Ensure(Grid.GetColumn(cards[i]) == i % columns && Grid.GetRow(cards[i]) == i / columns, "Cards arranged without overlap");
                    Ensure(cards[i].Bounds.Right <= grid.Bounds.Width + 1 && cards[i].Bounds.Width > 0, "Card fits available width");
                    var body = (StackPanel)cards[i].Child!;
                    Ensure(body.Children.All(t => t.Bounds.Width <= body.Bounds.Width + 1), "Card text fits without clipping");
                }
            }
            void Shot(string name)
            {
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
                bitmap.Render(window); bitmap.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
            }
            async Task CheckHover(string size)
            {
                var view = (DashboardView)window.CurrentPage!;
                var cards = view.SummaryCards.Children.Cast<Border>().ToList();
                var pointer = new Pointer(123, PointerType.Mouse, true);
                void Hover(Border card, bool entered) => card.RaiseEvent(new PointerEventArgs(
                    entered ? InputElement.PointerEnteredEvent : InputElement.PointerExitedEvent,
                    card, pointer, window, new Point(0, 0), 0, new PointerPointProperties(), KeyModifiers.None));
                foreach (var card in cards) Hover(card, false);
                await Settle();
                var bounds = cards.Select(c => c.Bounds).ToArray();
                for (var i = 0; i < cards.Count; i++)
                {
                    var card = cards[i];
                    var scale = (ScaleTransform)card.RenderTransform!;
                    Ensure(((ISolidColorBrush)card.Background!).Color == Colors.White && Math.Abs(scale.ScaleX - 1) < .0001,
                        "Every card starts white at normal scale");
                    Ensure(card.RenderTransformOrigin == RelativePoint.Center && card.Cursor is not null,
                        "Centered render transform and standard cursor configured");
                    Ensure(card.Transitions!.OfType<BrushTransition>().Single().Duration == TimeSpan.FromMilliseconds(180) &&
                        scale.Transitions!.OfType<DoubleTransition>().All(t => t.Duration == TimeSpan.FromMilliseconds(180)), "Color and scale use 180ms transitions");
                    Hover(card, true); await Settle();
                    Ensure(((ISolidColorBrush)card.Background!).Color == Color.Parse("#E0E9DF") &&
                        Math.Abs(scale.ScaleX - 1.02) < .0001 && Math.Abs(scale.ScaleY - 1.02) < .0001,
                        "Hover turns card green and scales both axes to 1.02");
                    Ensure(cards.Select(c => c.Bounds).SequenceEqual(bounds), "Hover never changes layout bounds or moves neighboring cards");
                    var expanded = new Rect(card.Bounds.X - card.Bounds.Width * .01, card.Bounds.Y - card.Bounds.Height * .01,
                        card.Bounds.Width * 1.02, card.Bounds.Height * 1.02);
                    Ensure(cards.Where(c => c != card).All(c => !expanded.Intersects(c.Bounds)), "Hovered card does not overlap a neighbor");
                    var topLeft = card.TranslatePoint(new Point(-card.Bounds.Width * .01, -card.Bounds.Height * .01), window)!.Value;
                    var bottomRight = card.TranslatePoint(new Point(card.Bounds.Width * 1.01, card.Bounds.Height * 1.01), window)!.Value;
                    Ensure(topLeft.X >= 0 && topLeft.Y >= 0 && bottomRight.X <= window.Bounds.Width && bottomRight.Y <= window.Bounds.Height,
                        "Hovered card remains within the visible window");
                    if (i == 0) Shot($"dashboard-hover-{size}");
                    Hover(card, false); await Settle();
                    Ensure(((ISolidColorBrush)card.Background!).Color == Colors.White && Math.Abs(scale.ScaleX - 1) < .0001 && Math.Abs(scale.ScaleY - 1) < .0001,
                        "Pointer exit restores white and normal scale");
                }
            }
            CheckData(28); CheckLayout(4); Shot("dashboard-cards-wide-28-days");
            await CheckHover("wide");
            var defaultValues = Values((DashboardView)window.CurrentPage!);
            ((DashboardView)window.CurrentPage!).PeriodPicker.SelectedIndex = 0; await Settle();
            CheckData(7); CheckLayout(4);
            Ensure(!Values((DashboardView)window.CurrentPage!).SequenceEqual(defaultValues), "Period selection changes actual values");
            Shot("dashboard-cards-wide-7-days");
            window.Width = 1040; window.Height = 720; await Settle();
            CheckData(7); CheckLayout(2); Shot("dashboard-cards-compact-7-days");
            await CheckHover("compact");
            ((DashboardView)window.CurrentPage!).PeriodPicker.SelectedIndex = 1; await Settle();
            CheckData(28); CheckLayout(2); Shot("dashboard-cards-compact-28-days");
            window.Width = 1600; await Settle(); CheckLayout(4);
            foreach (var menu in session.Restaurant.Menus) menu.ProductionRecords.Clear();
            session.Restaurant.Orders.Clear();
            window.ShowDashboard(); await Settle();
            CheckData(28);
            Ensure(Values((DashboardView)window.CurrentPage!).SequenceEqual(new[] { "0 porsi", "0 porsi", "0 porsi", "Rp0" }), "Empty dashboard shows zero values");
            Shot("dashboard-cards-empty");
            ((DashboardView)window.CurrentPage!).PeriodPicker.SelectedIndex = 0; await Settle(); CheckData(7);
            Console.WriteLine("All Replate dashboard card checks passed (7/28 days, completed revenue, percentages, empty data, 4/2 columns, resizing).");
        }
        finally { window.Close(); }
    }
}