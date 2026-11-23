using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using BruTile.Cache;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Avalonia;
using Replate.Desktop.Data;
using Replate.Domain.ValueObjects;
using MapBrush = Mapsui.Styles.Brush;
using MapColor = Mapsui.Styles.Color;
using MapPen = Mapsui.Styles.Pen;

namespace Replate.Desktop;

internal sealed record MapPin(GeoPoint Point, bool IsCustomer);

internal static class MapView
{
    internal const string Unavailable = "Lokasi restoran belum tersedia di peta.";
    internal static bool OfflineTiles { get; set; }

    internal static Control Create(GeoPoint center, IReadOnlyList<MapPin> pins, double height)
    {
        var map = new Map { CRS = "EPSG:3857", BackColor = MapColor.FromString("#EEF4F1") };
        if (!OfflineTiles) AddTiles(map);
        var features = pins.Select(pin => CreatePin(pin)).ToArray();
        map.Layers.Add(new MemoryLayer { Name = "Lokasi", Features = features, Style = null });
        var control = new MapControl { Height = height, Map = map };
        AutomationProperties.SetName(control, "Peta lokasi");
        map.ViewportInitialized += (_, _) => FramePins(map, Project(center), pins);

        var attribution = Ui.Text("© OpenStreetMap contributors", 12, Ui.Muted);
        attribution.HorizontalAlignment = HorizontalAlignment.Right;
        return Ui.Stack(6, new Border { CornerRadius = new CornerRadius(12), ClipToBounds = true, Child = control }, attribution);
    }

    private static void AddTiles(Map map)
    {
        try
        {
            OpenStreetMap.DefaultCache ??= new FileCache(
                Path.Combine(Path.GetDirectoryName(DemoSession.DefaultStorePath)!, "osm-tiles"), "png", TimeSpan.FromDays(7));
            map.Layers.Add(OpenStreetMap.CreateTileLayer(GeoHttp.UserAgent));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Pins are still available if the tile cache cannot be opened.
        }
    }

    private static PointFeature CreatePin(MapPin pin)
    {
        var color = ((ISolidColorBrush)(pin.IsCustomer ? Ui.Accent : Ui.Green)).Color;
        var feature = new PointFeature(Project(pin.Point));
        feature.Styles.Add(new SymbolStyle
        {
            SymbolScale = .65, SymbolType = SymbolType.Ellipse,
            Fill = new MapBrush(new MapColor(color.R, color.G, color.B)),
            Outline = new MapPen(MapColor.White, 2)
        });
        return feature;
    }

    private static MPoint Project(GeoPoint point)
    {
        var (x, y) = SphericalMercator.FromLonLat(point.Longitude, Math.Clamp(point.Latitude, -85.05112878, 85.05112878));
        return new MPoint(x, y);
    }

    private static void FramePins(Map map, MPoint center, IReadOnlyList<MapPin> pins)
    {
        const double streetResolution = 4.777314;
        var points = pins.Select(pin => Project(pin.Point)).Append(center).ToList();
        var minX = points.Min(p => p.X);
        var maxX = points.Max(p => p.X);
        var minY = points.Min(p => p.Y);
        var maxY = points.Max(p => p.Y);
        var viewport = map.Navigator.Viewport;
        var resolution = Math.Max(streetResolution, Math.Max(
            (maxX - minX) / Math.Max(1, viewport.Width - 64),
            (maxY - minY) / Math.Max(1, viewport.Height - 64)));
        map.Navigator.CenterOnAndZoomTo(new MPoint((minX + maxX) / 2, (minY + maxY) / 2), resolution);
    }
}
