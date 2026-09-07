using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NovaSourceG6Config;

public partial class RfStatusIndicator : UserControl
{
    public static readonly DependencyProperty IsRfOnProperty = DependencyProperty.Register(
        nameof(IsRfOn), typeof(bool?), typeof(RfStatusIndicator),
        new PropertyMetadata(null, (sender, _) => ((RfStatusIndicator)sender).UpdateWave()));

    private static readonly Geometry SineWave = CreateSineWave();
    private static readonly Geometry UnknownLine = Geometry.Parse("M1,8 L31,8");

    public bool? IsRfOn
    {
        get => (bool?)GetValue(IsRfOnProperty);
        set => SetValue(IsRfOnProperty, value);
    }

    public RfStatusIndicator()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            SystemParameters.StaticPropertyChanged += AnimationPreferenceChanged;
            UpdateWave();
        };
        Unloaded += (_, _) =>
        {
            SystemParameters.StaticPropertyChanged -= AnimationPreferenceChanged;
            WaveOffset.BeginAnimation(TranslateTransform.XProperty, null);
        };
        UpdateWave();
    }

    private void AnimationPreferenceChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.ClientAreaAnimation)) UpdateWave();
    }

    private void UpdateWave()
    {
        if (Wave is null) return;
        WaveOffset.BeginAnimation(TranslateTransform.XProperty, null);
        WaveOffset.X = 0;
        Wave.Data = IsRfOn is null ? UnknownLine : SineWave;
        Wave.Stroke = IsRfOn == true ? Brushes.SeaGreen : Brushes.Gray;
        Wave.StrokeDashArray = IsRfOn is null ? new DoubleCollection { 1.8, 2.4 } : null;
        var description = IsRfOn switch { true => "RF on", false => "RF off", null => "RF state unknown" };
        ToolTip = description;
        AutomationProperties.SetName(this, description);
        if (IsRfOn == true && IsLoaded && SystemParameters.ClientAreaAnimation)
        {
            WaveOffset.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(0, -24, TimeSpan.FromSeconds(1.2)) { RepeatBehavior = RepeatBehavior.Forever });
        }
    }

    private static Geometry CreateSineWave()
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(0, 8), false, false);
            for (var x = 0.25; x <= 64; x += 0.25)
                context.LineTo(new Point(x, 8 - 5.5 * Math.Sin(2 * Math.PI * x / 24)), true, false);
        }
        geometry.Freeze();
        return geometry;
    }
}
