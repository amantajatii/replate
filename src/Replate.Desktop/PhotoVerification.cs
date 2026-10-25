using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;

namespace Replate.Desktop;

internal static class PhotoVerification
{
    internal static async Task Run(MainWindow window, string output)
    {
        void Ensure(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
        void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        async Task Settle() => await Task.Delay(250);
        void Shot(string name)
        {
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.Bounds.Width, (int)window.Bounds.Height), new Vector(96, 96));
            bitmap.Render(window); bitmap.Save(Path.Combine(output, name + ".png"), PngBitmapEncoderOptions.Default);
        }
        var session = window.Session;
        var path = session.StorePath!;
        var source = Path.Combine(Path.GetDirectoryName(path)!, "source.png");
        using (var bitmap = new RenderTargetBitmap(new PixelSize(120, 80), new Vector(96, 96)))
        {
            var swatch = new Border { Background = Ui.Green, Width = 120, Height = 80 };
            swatch.Measure(new Size(120, 80)); swatch.Arrange(new Rect(0, 0, 120, 80));
            bitmap.Render(swatch); bitmap.Save(source, PngBitmapEncoderOptions.Default);
        }
        var bytes = File.ReadAllBytes(source);
        byte[] jpegBytes;
        using (var decoded = new Bitmap(source))
        using (var encoded = new MemoryStream())
        {
            decoded.Save(encoded, JpegBitmapEncoderOptions.Default);
            jpegBytes = encoded.ToArray();
        }
        foreach (var extension in new[] { ".jpg", ".jpeg" })
        {
            using var encoded = new MemoryStream(jpegBytes);
            Ensure((await MenuPhotoChange.Read(encoded, "food" + extension)).Data is not null, "JPG/JPEG files decode");
        }
        var menu = session.Listings.First(l => l.Quantity > 0).Menu;
        window.ShowMenuEditor(menu); await Settle();
        var editor = (MenuEditorView)window.CurrentPage!;
        Ensure(!editor.PhotoPreview.HasPhoto, "Menu without photo has placeholder");
        await editor.SelectPhoto(new MemoryStream(bytes), "food.png"); await Settle();
        Ensure(editor.PhotoPreview.HasPhoto && menu.ImageUrl == "", "Selection previews without changing stored menu");
        window.ShowMenus();
        Ensure(menu.ImageUrl == "", "Cancel photo choice leaves menu unchanged");
        window.ShowMenuEditor(menu); await Settle();
        editor = (MenuEditorView)window.CurrentPage!;
        await editor.SelectPhoto(new MemoryStream(bytes), "food.png");
        Click(editor.SaveButton); await Settle();
        var firstReference = menu.ImageUrl;
        var firstPhoto = session.PhotoPath(firstReference)!;
        Ensure(File.Exists(firstPhoto) && firstPhoto != source && window.SuccessMessage == "Menu berhasil disimpan.", "Save copies photo into app data");
        File.Move(source, source + ".moved");
        var reloaded = new DemoSession(path);
        var reloadedMenu = reloaded.Restaurant.Menus.Single(m => m.Id == menu.Id);
        Ensure(reloaded.StorageError is null && reloadedMenu.ImageUrl == firstReference && File.Exists(reloaded.PhotoPath(reloadedMenu.ImageUrl)), "Photo survives reload with original moved");
        window.ShowMarketplace(); await Settle();
        Ensure(window.GetVisualDescendants().OfType<MenuPhotoView>().Any(p => p.HasPhoto), "Marketplace shows photo");
        Shot("marketplace-with-photo");
        window.ShowFood(session.Listings.First(l => l.MenuId == menu.Id)); await Settle();
        Ensure(window.GetVisualDescendants().OfType<MenuPhotoView>().Single().HasPhoto, "Food detail shows photo");
        Shot("food-detail-with-photo");
        window.ShowMenuEditor(menu); await Settle();
        editor = (MenuEditorView)window.CurrentPage!;
        await editor.SelectPhoto(new MemoryStream(jpegBytes), "replacement.jpeg");
        Ensure(menu.ImageUrl == firstReference && File.Exists(firstPhoto), "Replacement is staged and old photo remains");
        Click(editor.SaveButton); await Settle();
        Ensure(menu.ImageUrl != firstReference && File.Exists(firstPhoto) && File.Exists(session.PhotoPath(menu.ImageUrl)), "Replacement saves unique photo and retains previous file");
        var replacementReference = menu.ImageUrl;
        window.ShowMenuEditor(menu); await Settle();
        editor = (MenuEditorView)window.CurrentPage!;
        editor.RemovePhoto(); await Settle();
        Ensure(!editor.PhotoPreview.HasPhoto && menu.ImageUrl == replacementReference, "Remove photo is staged");
        window.ShowMenus();
        Ensure(menu.ImageUrl == replacementReference, "Cancel removal preserves photo");
        window.ShowMenuEditor(menu); await Settle();
        editor = (MenuEditorView)window.CurrentPage!;
        await editor.SelectPhoto(new MemoryStream(new byte[] { 1, 2, 3 }), "invalid.png");
        Ensure(editor.PhotoError.IsVisible, "Invalid image rejected inline");
        Click(editor.SaveButton);
        Ensure(window.CurrentPage == editor && menu.ImageUrl == replacementReference, "Invalid photo prevents saving");
        await editor.SelectPhoto(new MemoryStream(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 1, 2, 3 }), "corrupt.png");
        Ensure(editor.PhotoError.IsVisible, "Corrupt image with valid signature rejected");
        await editor.SelectPhoto(new MemoryStream(new byte[MenuPhotoChange.MaximumBytes + 1]), "large.png");
        Ensure(editor.PhotoError.Text == "Ukuran foto maksimal 5 MB.", "Oversize image rejected");
        await editor.SelectPhoto(new MemoryStream(bytes), "wrong.gif");
        Ensure(editor.PhotoError.IsVisible, "Unsupported extension rejected");
        editor.RemovePhoto();
        Ensure(!editor.PhotoError.IsVisible, "Remove clears photo error");
        Click(editor.SaveButton); await Settle();
        Ensure(menu.ImageUrl == "" && new DemoSession(path).Restaurant.Menus.Single(m => m.Id == menu.Id).ImageUrl == "", "Photo removal persists");
        // A failed snapshot write must retain the old photo and menu values.
        MenuPhotoChange change;
        using (var stream = new MemoryStream(bytes)) change = await MenuPhotoChange.Read(stream, "photo.png");
        session.SaveMenu(menu, menu.Name, menu.Description, menu.Category, menu.NormalPrice, menu.WeightPerPortion, change);
        var beforeFailure = menu.ImageUrl;
        var beforeName = menu.Name;
        Directory.CreateDirectory(path + ".tmp");
        try
        {
            try
            {
                session.SaveMenu(menu, "Failed update", menu.Description, menu.Category, menu.NormalPrice, menu.WeightPerPortion, MenuPhotoChange.Remove);
                throw new InvalidOperationException("Expected storage failure");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
            Ensure(menu.ImageUrl == beforeFailure && menu.Name == beforeName && File.Exists(session.PhotoPath(beforeFailure)), "Failed save preserves old photo and menu");
        }
        finally { Directory.Delete(path + ".tmp"); }
        File.Delete(session.PhotoPath(beforeFailure)!);
        window.ShowFood(session.Listings.First(l => l.MenuId == menu.Id)); await Settle();
        Ensure(!window.GetVisualDescendants().OfType<MenuPhotoView>().Single().HasPhoto, "Missing photo uses placeholder");
        Shot("food-detail-missing-photo");
        // Emulate a legacy snapshot with the optional photo field absent.
        var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        foreach (var node in json["Menus"]!.AsArray()) node!.AsObject().Remove("ImageUrl");
        var legacy = Path.Combine(Path.GetDirectoryName(path)!, "legacy.json");
        File.WriteAllText(legacy, json.ToJsonString());
        var oldSession = new DemoSession(legacy);
        Ensure(oldSession.StorageError is null && oldSession.Restaurant.Menus.All(m => string.IsNullOrEmpty(m.ImageUrl)), "Legacy data without photos remains readable");
        window.ShowMenuEditor(); await Settle();
        editor = (MenuEditorView)window.CurrentPage!;
        editor.NameInput.Text = "Menu foto baru";
        await editor.SelectPhoto(new MemoryStream(jpegBytes), "new.jpg");
        await Settle();
        Shot("menu-editor-photo-preview");
        Click(editor.SaveButton); await Settle();
        var added = session.Restaurant.Menus.Single(m => m.Name == "Menu foto baru");
        var addedReloaded = new DemoSession(path).Restaurant.Menus.Single(m => m.Id == added.Id);
        Ensure(File.Exists(session.PhotoPath(addedReloaded.ImageUrl)), "New menu photo persists");
        Ensure(File.ReadAllBytes(source + ".moved").SequenceEqual(bytes), "Original image is unchanged");
        Console.WriteLine("All Replate photo checks passed.");
    }
}