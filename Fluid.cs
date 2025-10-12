using Godot;
using System;


[Tool]
public partial class Fluid : Node2D
{
    //This class is responsible for populating the scene with objects
    //It should have a property for the object to spawn, as well as the parent node
    [Export] public PackedScene TileScene { get; set; }

    private int _n_cols = 10;
    [Export]
    public int NCols
    {
        get => _n_cols;
        set
        {
            _n_cols = value;
            UpdateBoard();
        }
    }

    private int _n_rows = 10;
    [Export]
    public int NRows
    {
        get => _n_rows;
        set
        {
            _n_rows = value;
            UpdateBoard();
        }
    }

    Area2D FluidArea => GetNode<Area2D>("FluidArea");
    CollisionShape2D FluidAreaShape => FluidArea.GetNode<CollisionShape2D>("CollisionShape2D");

    public override void _Ready()
    {
        UpdateBoard();
    }


    private void UpdateBoard()
    {
        //Clear existing pegs
        foreach (Node2D child in GetChildren())
        {
            if (child is not Area2D)
            {
                child.QueueFree();
            }
        }

        //Get the size of the area
        Rect2 areaRect = FluidAreaShape.Shape.GetRect();
        Vector2 areaSize = areaRect.Size;

        //Calculate spacing
        float xSpacing = areaSize.X / (NCols - 1);
        float ySpacing = areaSize.Y / (NRows - 1);

        //Spawn tiles in a grid pattern
        for (int row = 0; row < NRows; row++)
        {
            for (int col = 0; col < NCols; col++)
            {
                Vector2 position = new Vector2(col * xSpacing, row * ySpacing); //+ areaRect.Position / 2;
                var tileInstance = TileScene.Instantiate<Node2D>();
                tileInstance.Position = position;
                AddChild(tileInstance);
            }
        }
    }
}
