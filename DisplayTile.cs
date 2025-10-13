using Godot;
using System;

[Tool]
public partial class DisplayTile : Node2D
{

    [Export] public int Col { get; set; }
    [Export] public int Row { get; set; }

    [Export] public Color NegativeColor { get; set; } = new Color(0, 0, 1);
    [Export] public Color NeutralColor { get; set; } = new Color(1, 1, 1);
    [Export] public Color PositiveColor { get; set; } = new Color(1, 0, 0);

    [Export] public float MinPressure { get; set; } = -10f;
    [Export] public float MaxPressure { get; set; } = 10f;

    ColorRect ColorRect => GetNode<ColorRect>("ColorRect");

    RichTextLabel Label => GetNode<RichTextLabel>("RichTextLabel");

    public void UpdateColor(float pressure)
    {


        Label.Text = "[font_size=10] " + pressure.ToString("F2") + " [/font_size]";
        
        if (pressure < 0)
        {
            //Map pressure to a color between NeutralColor and NegativeColor
            float t = pressure / MinPressure;
            t = Mathf.Clamp(t, 0f, 1f);
            Color color = NeutralColor + (NegativeColor - NeutralColor) * t;
            ColorRect.Color = color;
            return;
        }

        if (pressure > 0)
        {
            //Map pressure to a color between NeutralColor and PositiveColor
            float t = pressure / MaxPressure;
            t = Mathf.Clamp(t, 0f, 1f);
            Color color = NeutralColor + (PositiveColor - NeutralColor) * t;
            ColorRect.Color = color;
            return;
        }
    }


}
