using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Replate.Desktop;

internal sealed class SignOutDialog : ModalDialog
{
    internal const double CardWidth = 400;
    internal const string TitleText = "Keluar dari akun?";
    internal const string MessageText = "Anda akan kembali ke halaman masuk.";
    internal const string CancelLabel = "Batal";
    internal const string ConfirmLabel = "Keluar";

    private readonly Action signOut;
    private bool confirmed;
    internal TextBlock Title { get; }
    internal TextBlock MessageTextBlock { get; }
    internal Button CancelButton { get; }
    internal Button ConfirmButton { get; }
    protected override Control InitialFocus => ConfirmButton;

    internal SignOutDialog(Action signOut, bool keyboard) : base(keyboard)
    {
        this.signOut = signOut;
        CollectionView.AddTextLinkStyles(Styles);

        // Content
        Title = Ui.Text(TitleText, 20, Ui.Ink, true);
        Title.FontWeight = FontWeight.Bold;
        MessageTextBlock = Ui.Text(MessageText, 14, Ui.Muted);

        // Actions
        CancelButton = CollectionView.CreateTextLink(CancelLabel, Close);
        ConfirmButton = Ui.Button(ConfirmLabel, Confirm, true);
        AutomationProperties.SetName(ConfirmButton, "Keluar dari akun");
        var buttons = Ui.Row(12, CancelButton, ConfirmButton);
        buttons.HorizontalAlignment = HorizontalAlignment.Right;

        // Card
        var content = Ui.Stack(Ui.Spacing.Related, Title, MessageTextBlock, buttons);
        content.Margin = new Thickness(Ui.Spacing.Section);
        SetCard(CardWidth, content, TitleText);
        AutomationProperties.SetName(this, TitleText);
        AddHandler(KeyDownEvent, OnKey, RoutingStrategies.Tunnel);
    }

    // State
    internal void Confirm()
    {
        if (confirmed) return;
        confirmed = true;
        signOut();
    }

    private void OnKey(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || CancelButton.IsFocused) return;
        e.Handled = true;
        Confirm();
    }
}
