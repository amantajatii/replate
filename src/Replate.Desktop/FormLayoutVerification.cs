using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace Replate.Desktop;

internal static class FormLayoutVerification
{
    internal static async Task Run(string output)
    {
        void Ensure(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        async Task Settle() => await Task.Delay(350);
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var session = new DemoSession();
        var window = new MainWindow(session);
        try
        {
            window.Show(); await Settle();
            void Shot(string name)
            {
                using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
                bitmap.Render(window); bitmap.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
            }
            FormArea CheckForm(string title)
            {
                var view = window.CurrentPage!;
                var form = view.GetVisualDescendants().OfType<FormArea>().Single();
                Ensure(form.Bounds.Width <= 860.1 && form.Bounds.Width > 0 && form.Bounds.Width <= view.Bounds.Width - 63,
                    "Form respects max width and available page space");
                var heading = view.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == title);
                Ensure(Math.Abs(form.TranslatePoint(default, window)!.Value.X - heading.TranslatePoint(default, window)!.Value.X) < 1,
                    "Form left edge aligns with page heading");
                foreach (var pair in form.GetVisualDescendants().OfType<FormColumns>())
                    Ensure(pair.ColumnDefinitions.Count == (pair.Bounds.Width >= 800 ? 2 : 1), "Form pairs stack when space is insufficient");
                foreach (var input in form.GetVisualDescendants().Where(c => c is TextBox or NumericUpDown or ComboBox or DatePicker or TimePicker))
                {
                    if (input is DatePicker or TimePicker) Ensure(((Control)input).HorizontalAlignment == Avalonia.Layout.HorizontalAlignment.Left, "Date and time controls align with their labels");
                    var x = input.TranslatePoint(default, form)!.Value.X;
                    Ensure(x >= -1 && x + input.Bounds.Width <= form.Bounds.Width + 1, "Inputs fit within form width");
                }
                return form;
            }
            async Task Reach(Control control)
            {
                control.BringIntoView(); await Settle();
                var location = control.TranslatePoint(default, window)!.Value;
                Ensure(location.Y >= 0 && location.Y + control.Bounds.Height <= window.Bounds.Height + 1, "Actions and errors remain reachable by scrolling");
            }
            foreach (var maximized in new[] { false, true })
            {
                var size = maximized ? "maximized" : "minimum";
                window.WindowState = maximized ? WindowState.Maximized : WindowState.Normal;
                if (!maximized) { window.Width = 1040; window.Height = 720; }
                await Task.Delay(650);
                Ensure(!maximized || window.WindowState == WindowState.Maximized, "Native maximized state applied");
                foreach (var edit in new[] { false, true })
                {
                    window.ShowMenuEditor(edit ? session.Restaurant.Menus[0] : null); await Settle();
                    var editor = (MenuEditorView)window.CurrentPage!;
                    editor.DescriptionInput.Text = "Deskripsi panjang untuk memeriksa pembungkusan teks dan mempertahankan isian pengguna. " + new string('x', 200);
                    editor.NameInput.Text = ""; editor.PriceInput.Value = 0; editor.WeightInput.Value = 0;
                    await editor.SelectPhoto(new MemoryStream(new byte[] { 1, 2, 3 }), "invalid.png");
                    Click(editor.SaveButton); await Settle();
                    CheckForm(edit ? "Ubah menu" : "Tambah menu");
                    Ensure(editor.FieldErrors.Values.Where(e => e.IsVisible).All(e => e.FontSize >= 14 && e.TextWrapping == TextWrapping.Wrap) &&
                        editor.PhotoError.FontSize >= 14 && editor.NameInput.FontSize == 15 && editor.SaveButton.FontSize >= 14,
                        "Form input, help, validation, and action typography is readable");
                    Shot($"form-menu-{(edit ? "edit" : "add")}-{size}-errors");
                    await Reach(editor.PickPhotoButton); await Reach(editor.PhotoError); await Reach(editor.SaveButton);
                    Shot($"form-menu-{(edit ? "edit" : "add")}-{size}-photo-actions");
                }
                foreach (var edit in new[] { false, true })
                {
                    window.ShowListingEditor(edit ? session.Listings[0] : null); await Settle();
                    var form = CheckForm(edit ? "Ubah penawaran" : "Buat penawaran");
                    Ensure(form.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text?.StartsWith("Stok tidak boleh") == true).FontSize >= 14,
                        "Listing instructions are readable");
                    var save = form.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Simpan penawaran");
                    await Reach(save);
                    Shot($"form-listing-{(edit ? "edit" : "add")}-{size}");
                }
                window.ShowProduction(); await Settle();
                var production = (ProductionView)window.CurrentPage!;
                production.ProducedInput.Value = 0; Click(production.SaveButton); await Settle();
                var productionForm = CheckForm("Catatan produksi");
                Ensure(production.FieldErrors["produced"].IsVisible && production.FieldErrors["produced"].FontSize >= 14,
                    "Production validation remains inline and readable");
                await Reach(production.FieldErrors["produced"]);
                Shot($"form-production-{size}-errors");
                await Reach(production.SaveButton);
                Ensure(production.Bounds.Width <= 924 || production.GetVisualDescendants().OfType<StackPanel>().Any(p => p.Bounds.Width > productionForm.Bounds.Width),
                    "Production detail table is not constrained to the editor width");
                foreach (var restaurant in new[] { false, true })
                {
                    if (restaurant) window.ShowRestaurantOrders(); else window.ShowCustomerOrders();
                    await Settle();
                    var filter = window.GetVisualDescendants().OfType<ComboBox>().Single();
                    Ensure(filter.Bounds.Width >= 280 && filter.Bounds.Width <= 360 && filter.FontSize >= 14,
                        "Order status dropdown has readable font and compact width in both areas");
                    Ensure(window.CurrentPage!.Bounds.Width > filter.Bounds.Width, "Orders page remains full width");
                    filter.SelectedIndex = 1; await Settle();
                    Shot($"form-order-filter-{(restaurant ? "restaurant" : "customer")}-{size}");
                }
            }
            Console.WriteLine("All Replate form layout checks passed (minimum/maximized, menu/listing add/edit, production errors, photo/actions, both status filters).");
        }
        finally { window.Close(); }
    }
}