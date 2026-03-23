using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;

using MapLocation = PopulationSimulation.Models.Location;
using Citizen = PopulationSimulation.Models.Citizen;

namespace PopulationSimulation.Views;

public class MapControl : Control
{
    public static readonly StyledProperty<ObservableCollection<MapLocation>?> LocationsProperty =
        AvaloniaProperty.Register<MapControl, ObservableCollection<MapLocation>?>(nameof(Locations));
    public static readonly StyledProperty<ObservableCollection<Citizen>?> CitizensProperty =
        AvaloniaProperty.Register<MapControl, ObservableCollection<Citizen>?>(nameof(Citizens));
    public static readonly StyledProperty<Citizen?> SelectedCitizenProperty =
        AvaloniaProperty.Register<MapControl, Citizen?>(nameof(SelectedCitizen));
    public static readonly StyledProperty<MapLocation?> SelectedLocationProperty =
        AvaloniaProperty.Register<MapControl, MapLocation?>(nameof(SelectedLocation));

    public ObservableCollection<MapLocation>? Locations
    { get => GetValue(LocationsProperty); set => SetValue(LocationsProperty, value); }
    public ObservableCollection<Citizen>? Citizens
    { get => GetValue(CitizensProperty); set => SetValue(CitizensProperty, value); }
    public Citizen? SelectedCitizen
    { get => GetValue(SelectedCitizenProperty); set => SetValue(SelectedCitizenProperty, value); }
    public MapLocation? SelectedLocation
    { get => GetValue(SelectedLocationProperty); set => SetValue(SelectedLocationProperty, value); }

    static MapControl()
    {
        AffectsRender<MapControl>(LocationsProperty, CitizensProperty, SelectedCitizenProperty, SelectedLocationProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LocationsProperty || change.Property == CitizensProperty)
        {
            if (change.OldValue is INotifyCollectionChanged old) old.CollectionChanged -= OnColl;
            if (change.NewValue is INotifyCollectionChanged nw) nw.CollectionChanged += OnColl;
        }
    }

    private void OnColl(object? s, NotifyCollectionChangedEventArgs e) => InvalidateVisual();
    public void Refresh() => InvalidateVisual();

    public override void Render(DrawingContext dc)
    {
        double size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0) return;

        double ox = (Bounds.Width - size) / 2;
        double oy = (Bounds.Height - size) / 2;

        // Background
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(34, 40, 49)), null, new Rect(ox, oy, size, size));

        // Grid lines
        var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)), 0.5);
        for (int i = 1; i < 10; i++)
        {
            double gx = ox + (i / 10.0) * size;
            dc.DrawLine(gridPen, new Point(gx, oy), new Point(gx, oy + size));
            double gy = oy + (i / 10.0) * size;
            dc.DrawLine(gridPen, new Point(ox, gy), new Point(ox + size, gy));
        }
        dc.DrawRectangle(null, new Pen(Brushes.Gray, 1), new Rect(ox, oy, size, size));

        // Icon size: recognizable but no overlap for coords > 250m apart
        // 250m on 10000m map at size pixels = (250/10000)*size
        double maxIconPx = (250.0 / 10000.0) * size;
        double iconSize = Math.Clamp(size / 55, 8, maxIconPx);

        // Draw locations
        if (Locations != null)
        {
            foreach (var loc in Locations)
            {
                double px = ox + (loc.X / 10000.0) * size;
                double py = oy + (loc.Y / 10000.0) * size;
                if (px < ox - iconSize || px > ox + size + iconSize || py < oy - iconSize || py > oy + size + iconSize)
                    continue;

                if (loc == SelectedLocation)
                    dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(100, 0, 150, 255)), null,
                        new Point(px, py), iconSize, iconSize);

                DrawIcon(dc, loc.Icon, px, py, iconSize);
            }
        }

        // Draw citizens
        if (Citizens != null)
        {
            foreach (var cit in Citizens)
            {
                double px = ox + (cit.CurrentX / 10000.0) * size;
                double py = oy + (cit.CurrentY / 10000.0) * size;
                if (px < ox - iconSize || px > ox + size + iconSize || py < oy - iconSize || py > oy + size + iconSize)
                    continue;

                if (cit == SelectedCitizen)
                    dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 255, 200, 0)), null,
                        new Point(px, py), iconSize, iconSize);

                DrawIcon(dc, cit.Icon, px, py - iconSize * 0.7, iconSize * 0.85);
            }
        }
    }

    private static void DrawIcon(DrawingContext dc, string text, double x, double y, double fontSize)
    {
        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI Emoji, Apple Color Emoji, Noto Color Emoji, sans-serif"),
            fontSize, Brushes.White);
        dc.DrawText(ft, new Point(x - ft.Width / 2, y - ft.Height / 2));
    }

    protected override Size MeasureOverride(Size a)
    {
        double w = double.IsInfinity(a.Width) ? 500 : a.Width;
        double h = double.IsInfinity(a.Height) ? 500 : a.Height;
        return new Size(w, h);
    }
}
