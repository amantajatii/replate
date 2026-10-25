using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Replate.Domain.Entities;
using Replate.Domain.Enums;

namespace Replate.Desktop;

internal sealed class OrderStatusBadge : Border
{
    internal OrderStatus Status { get; }
    internal OrderStatusBadge(Order order)
    {
        Status = order.Status;
        Background = Brush.Parse(Status switch
        {
            OrderStatus.Reserved => "#FFF3CD",
            OrderStatus.Confirmed => "#DBEAFE",
            OrderStatus.ReadyForPickup => "#EDE0FA",
            OrderStatus.Completed => "#E0E9DF",
            _ => "#E8EAED"
        });
        CornerRadius = new CornerRadius(6);
        Padding = new Thickness(10, 5);
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        var label = DemoSession.Status(order);
        Child = Ui.Text(label, 13, Ui.Ink, true);
        Avalonia.Automation.AutomationProperties.SetName(this, label);
    }
}