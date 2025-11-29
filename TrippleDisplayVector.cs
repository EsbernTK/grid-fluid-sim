using Godot;
using System;

[Tool]
public partial class TrippleDisplayVector : DisplayVector
{
    [Export] public PackedScene VectorScene { get; set; }
    private Vector3 _value = new Vector3(0, 0, 0);
    [Export]
    public new Vector3 Value
    {
        get => _value;
        set { _value = value; QueueRedraw(); }
    }

    private DisplayVector[] vectorComponents = new DisplayVector[3];
    [Export] public float degreesOffset = 60f;
    [Export] public float distanceOffset = 20f;

    [Export] public override bool ShowVector
    {
        get => base.ShowVector;
        set
        {
            base.ShowVector = value;
            if (vectorComponents[0] != null)
                vectorComponents[0].ShowVector = value;
                vectorComponents[0].QueueRedraw();
            if (vectorComponents[1] != null)
                vectorComponents[1].ShowVector = value;
                vectorComponents[1].QueueRedraw();
            if (vectorComponents[2] != null)
                vectorComponents[2].ShowVector = value;
                vectorComponents[2].QueueRedraw();
        }
    }


    public override void _Ready()
    {
        base._Ready();

        float sinAngle = Mathf.Sin(Mathf.DegToRad(degreesOffset));
        float sinAngle2 = Mathf.Sin(Mathf.DegToRad(degreesOffset*2));
        float cosAngle = Mathf.Cos(Mathf.DegToRad(degreesOffset));
        float cosAngle2 = Mathf.Cos(Mathf.DegToRad(degreesOffset*2));

        // Create three DisplayVector children for X, Y, Z components
        vectorComponents[0] = VectorScene.Instantiate<DisplayVector>();
        vectorComponents[0].Position = new Vector2(-cosAngle, sinAngle) * distanceOffset;
        vectorComponents[0].BaseValue = new Vector2(-cosAngle2, -sinAngle2);
        AddChild(vectorComponents[0]);

        vectorComponents[1] = VectorScene.Instantiate<DisplayVector>();
        vectorComponents[1].Position = new Vector2(0, -distanceOffset);
        vectorComponents[1].BaseValue = new Vector2(1, 0);
        AddChild(vectorComponents[1]);

        vectorComponents[2] = VectorScene.Instantiate<DisplayVector>();
        vectorComponents[2].Position = new Vector2(cosAngle, sinAngle) * distanceOffset;
        vectorComponents[2].BaseValue = new Vector2(cosAngle2, -sinAngle2);
        AddChild(vectorComponents[2]);
    }

    public override void SetValue(Vector3 newValue)
    {
        Value = newValue;
        if(vectorComponents[0] == null || vectorComponents[1] == null || vectorComponents[2] == null)
        {
            //GD.PrintErr("Vector components not initialized!");
            return;
        }
        vectorComponents[0].ScaleBaseValue(newValue.X);
        vectorComponents[1].ScaleBaseValue(newValue.Y);
        vectorComponents[2].ScaleBaseValue(newValue.Z);
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Optionally, draw something to represent the tripple vector itself
    }

}
