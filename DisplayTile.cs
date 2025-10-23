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

    ColorRect MyColorRect => GetNodeOrNull<ColorRect>("ColorRect");
    Polygon2D MyPolygon => GetNodeOrNull<Polygon2D>("Polygon2D");

    RichTextLabel Label => GetNode<RichTextLabel>("RichTextLabel");


    public override void _Ready()
    {
        //we are not sure if this script will have a ColorRect or Polygon2D child, so we try to get both
        //try
        //{
        //    MyColorRect = GetNode<ColorRect>("ColorRect");
        //}
        //catch (Exception)
        //{
        //    //do nothing
        //}
        //try
        //{
        //    MyPolygon = GetNode<Polygon2D>("Polygon2D");
        //}
        //catch (Exception)
        //{
        //    //do nothing
        //}
    }


    public Vector2 getPolygonSize(Polygon2D polygon)
    {
        if (polygon != null)
        {
            //Get the bounding box of the polygon
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (Vector2 point in polygon.Polygon)
            {
                if (point.X < min.X) min.X = point.X;
                if (point.Y < min.Y) min.Y = point.Y;
                if (point.X > max.X) max.X = point.X;
                if (point.Y > max.Y) max.Y = point.Y;
            }
            return (max - min) * polygon.Scale;
        }
        else
        {
            return Vector2.Zero;
        }
    }

    public Vector2 GetTileSize()
    {   
        GD.Print("Getting Tile Size, MyColorRect: ", MyColorRect, " MyPolygon: ", MyPolygon);
        if (MyColorRect != null)
        {
            return MyColorRect.Size;
        }
        else if (MyPolygon != null)
        {
            //Get the bounding box of the polygon
            Vector2 bbox = getPolygonSize(MyPolygon);
            return bbox;
        }
        else
        {
            return Vector2.Zero;
        }
    }

    public void UpdateColor(float pressure)
    {


        Label.Text = "[font_size=10] " + pressure.ToString("F2") + " [/font_size]";
        
        if (pressure < 0)
        {
            //Map pressure to a color between NeutralColor and NegativeColor
            float t = pressure / MinPressure;
            t = Mathf.Clamp(t, 0f, 1f);
            Color color = NeutralColor + (NegativeColor - NeutralColor) * t;
            if (MyPolygon != null)
            {
                MyPolygon.Color = color;
            }
            if (MyColorRect != null)
            {
                MyColorRect.Color = color;
            }
            return;
        }

        if (pressure > 0)
        {
            //Map pressure to a color between NeutralColor and PositiveColor
            float t = pressure / MaxPressure;
            t = Mathf.Clamp(t, 0f, 1f);
            Color color = NeutralColor + (PositiveColor - NeutralColor) * t;
            if (MyPolygon != null)
            {
                MyPolygon.Color = color;
            }
            if (MyColorRect != null)
            {
                MyColorRect.Color = color;
            }
            return;
        }
    }


}
