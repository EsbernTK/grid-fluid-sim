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

    public void SetValue(Vector2 newValue)
    {
        Value = newValue;
        QueueRedraw();
    }

    public void SetValue(float x, float y)
    {
        Value = new Vector2(x, y);
        QueueRedraw();
    }

    public void SetValue(Vector3 newValue)
    {
        Value = new Vector2(newValue.X, newValue.Y);
        QueueRedraw();
    }
    
    public void SetValue(object newValue)
    {
        switch (newValue)
        {
            case Vector2 v2:
                Value = v2;
                break;
            case Vector3 v3:
                Value = new Vector2(v3.X, v3.Y);
                break;
            default:
                GD.PrintErr("Unsupported type for SetValue: ", newValue.GetType());
                break;
        }
        QueueRedraw();
    }

    /*
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                // Start dragging
                GetViewport().SetInputAsHandled();
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion)
        {
            if (mouseMotion.ButtonMask.HasFlag(MouseButton.Left))
            {
                // Update vector value based on mouse position
                Vector2 localMousePos = ToLocal(mouseMotion.Position);
                Value = localMousePos;
                OnVectorChanged?.Invoke(this);
                GetViewport().SetInputAsHandled();
            }
        }
    }
    */
}
