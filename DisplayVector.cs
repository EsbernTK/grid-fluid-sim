using Godot;
using System;
[Tool]
public partial class DisplayVector : Node2D
{
    // Vector endpoint in local space (origin is this node's position)
    private Vector2 _value = new(0, 0);
    [Export]
    public Vector2 Value
    {
        get => _value;
        set { _value = value; QueueRedraw(); }
    }

    private float _thickness = 3f;
    [Export(PropertyHint.Range, "1,20,0.5")]
    public float Thickness
    {
        get => _thickness;
        set { _thickness = Mathf.Max(1, value); QueueRedraw(); }
    }

    private Color _color = new("white");
    [Export]
    public Color Color
    {
        get => _color;
        set { _color = value; QueueRedraw(); }
    }

    //When the vector is changed by the user, through dragging the arrowhead, this callback is invoked
    public Action<DisplayVector> OnVectorChanged;

    public int col_index; //Index in the grid of vectors
    public int row_index; //Index in the grid of vectors

    [Export] public bool ShowArrowhead { get; set; } = true;
    [Export(PropertyHint.Range, "4,64,1")] public float ArrowSize { get; set; } = 12f;
    [Export] public bool ShowOriginDot { get; set; } = true;
    [Export(PropertyHint.Range, "2,16,1")] public float OriginDotRadius { get; set; } = 4f;

    public override void _Ready()
    {
        //Print that we are ready
        base._Ready();
    }

    public override void _Draw()
    {
        var dir = Value.Normalized();
        
        // Line from (0,0) to vector
        DrawLine(Vector2.Zero, Value - dir * ArrowSize, Color, Thickness, antialiased: true);
        // Draw a triangle at the end of the line to represent the arrowhead

        if (ShowArrowhead && Value.Length() > 0.01f)
        {
            // Arrowhead is a small triangle at the tip, oriented along the vector
            var tip = Value;
            var left = tip - dir * ArrowSize + dir.Orthogonal() * (ArrowSize * 0.5f);
            var right = tip - dir * ArrowSize - dir.Orthogonal() * (ArrowSize * 0.5f);
            DrawPolygon(new Vector2[] { tip, left, right }, new Color[] { Color, Color, Color });
        }

        if (ShowOriginDot)
            DrawCircle(Vector2.Zero, OriginDotRadius, Color);

    }

}
