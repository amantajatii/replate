using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Replate.Desktop;

internal sealed class LocationBrowserDialog : ModalDialog
{
    private const string Title = "Buka browser untuk lokasi?";
    internal Button CancelButton { get; }
    internal Button ConfirmButton { get; }
    internal bool Accepted { get; private set; }

    internal LocationBrowserDialog() : base(keyboard: true)
    {
        CancelButton = Ui.Button("Batal", Close);
        ConfirmButton = Ui.Button("Lanjutkan", () => { Accepted = true; Close(); }, true);
        var actions = Ui.Row(12, CancelButton, ConfirmButton);
        actions.HorizontalAlignment = HorizontalAlignment.Right;
        var content = Ui.Stack(16, Ui.Columns("*,Auto", Ui.Text(Title, 22, bold: true), CloseButton),
            Ui.Text("Replate akan membuka tab browser untuk meminta izin lokasi perangkatmu. Lokasi hanya dipakai untuk menghitung jarak ke restoran dan disimpan di perangkat ini. Setelah lokasi terkirim, kembali ke Replate.", 14),
            Ui.Text("Koordinat dikirim ke Nominatim untuk nama lokasi dan ke OSRM untuk estimasi perjalanan.", 13, Ui.Muted),
            actions);
        content.Margin = new Thickness(Ui.Spacing.Section);
        SetCard(500, content, Title);
    }
}
