using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using Replate.Domain.Enums;

namespace Replate.Desktop;

internal static class ProductionVerification
{
    internal static async Task Run(MainWindow window, string output)
    {
        void Ensure(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        void Click(Button b) => b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        async Task Settle() => await Task.Delay(250);
        var session = window.Session;
        // Wait for earlier success notifications to expire before testing invalid submissions.
        await Task.Delay(4200);
        var listing = session.Listings.First(l => l.Status == ListingStatus.Active && l.Quantity > 0);
        var menu = listing.Menu;
        var date = DateOnly.FromDateTime(DateTime.Today);
        var record = menu.ProductionRecords.Single(r => r.RecordDate == date);
        var originalProduced = record.ProducedQuantity;
        var originalSold = record.RegularSoldQuantity;
        window.ShowProduction(); await Settle();
        var view = (ProductionView)window.CurrentPage!;
        view.MenuInput.SelectedItem = menu; await Settle();
        var before = File.ReadAllBytes(session.StorePath!);
        view.ProducedInput.Value = 0;
        Click(view.SaveButton); await Settle();
        Ensure(view.FieldErrors["produced"].Text?.StartsWith("Jumlah produksi minimal ") == true && view.FieldErrors["sold"].IsVisible,
            "Insufficient production and excessive sales shown together inline");
        Ensure(view.ProducedInput.GetVisualDescendants().Contains(window.FocusManager!.GetFocusedElement() as Avalonia.Visual), "Focus first invalid production field");
        Ensure(view.ProducedInput.Value == 0 && view.SoldInput.Value == originalSold && view.MenuInput.SelectedItem == menu,
            "Production validation retains all input");
        Ensure(record.ProducedQuantity == originalProduced && before.SequenceEqual(File.ReadAllBytes(session.StorePath!)) && window.SuccessMessage is null,
            "Invalid production does not mutate, persist, or notify");
        Ensure(!window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.IsVisible && t.Text?.StartsWith("Jumlah produksi minimal ") == true &&
            !view.GetVisualDescendants().Contains(t)), "Production error not duplicated in banner");
        using (var shot = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96)))
        { shot.Render(window); shot.Save(Path.Combine(output, "production-inline-errors.png"), PngBitmapEncoderOptions.Default); }
        view.ProducedInput.Value = originalProduced;
        Ensure(view.FieldErrors.Values.All(t => !t.IsVisible), "Correcting production clears related errors immediately");
        view.ProducedInput.Text = "abc";
        Ensure(view.FieldErrors["produced"].Text?.Contains("bilangan bulat") == true, "Invalid numeric text reported inline");
        view.ProducedInput.Value = originalProduced + 1;
        view.SoldInput.Text = "";
        Click(view.SaveButton);
        Ensure(view.FieldErrors["sold"].IsVisible, "Empty sales reported inline");
        view.SoldInput.Value = originalSold;
        view.DateInput.SelectedDate = DateTimeOffset.Now.AddDays(1);
        Ensure(view.FieldErrors["date"].IsVisible, "Future date revalidated inline");
        view.DateInput.SelectedDate = DateTimeOffset.Now;
        Ensure(!view.FieldErrors["date"].IsVisible, "Date correction clears error");
        view.MenuInput.SelectedIndex = -1;
        Ensure(view.FieldErrors["menu"].IsVisible, "Missing menu revalidated inline");
        view.MenuInput.SelectedItem = menu;
        Ensure(view.FieldErrors.Values.All(t => !t.IsVisible), "Menu correction revalidates all fields");
        view.ProducedInput.Value = originalProduced + 1;
        Click(view.SaveButton); await Settle();
        Ensure(record.ProducedQuantity == originalProduced + 1 && view.FieldErrors.Values.All(t => !t.IsVisible), "Corrected production saved");
        var reloaded = new DemoSession(session.StorePath);
        Ensure(reloaded.Restaurant.Menus.Single(m => m.Id == menu.Id).ProductionRecords.Single(r => r.RecordDate == date).ProducedQuantity == originalProduced + 1,
            "Corrected production survives reload");
        Directory.CreateDirectory(session.StorePath + ".tmp");
        try
        {
            Click(view.SaveButton);
            Ensure(window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.IsVisible && t.Text?.StartsWith("Perubahan pada sesi ini belum tersimpan") == true),
                "Storage failures still use general banner");
            Ensure(view.FieldErrors.Values.All(t => !t.IsVisible), "Storage failure not shown as field error");
        }
        finally { Directory.Delete(session.StorePath + ".tmp"); }
        Console.WriteLine("All Replate production validation checks passed.");
    }
}