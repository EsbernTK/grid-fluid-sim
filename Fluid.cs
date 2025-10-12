using Godot;
using System;


[Tool]
public partial class Fluid : Node2D
{
    //This class is responsible for populating the scene with objects
    //It should have a property for the object to spawn, as well as the parent node
    [Export] public PackedScene TileScene { get; set; }
    [Export] public PackedScene VectorScene { get; set; }

    private int _n_cols = 10;
    [Export]
    public int NCols
    {
        get => _n_cols;
        set
        {
            _n_cols = value;
            CreateBoard();
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
            CreateBoard();
        }
    }

    Area2D FluidArea => GetNode<Area2D>("FluidArea");
    CollisionShape2D FluidAreaShape => FluidArea.GetNode<CollisionShape2D>("CollisionShape2D");

    //This grid stores the velocity of each tile corner, it will have (NCols + 1) * (NRows + 1) elements
    public Vector2[][] tileCornerVelocityGrid;
    public float[][] tilePressureGrid;

    public float viscosity = 0.1f;
    public float timeStep = 1f / 60f;
    public float density = 1f;

    public DisplayVector[][] vectorGrid;


    public void setCornerVelocityCallback(int col, int row, Vector2 velocity)
    {
        if (col < 0 || col > NCols || row < 0 || row > NRows)
        {
            GD.PrintErr("Corner velocity callback out of bounds: ", col, ", ", row);
            return;
        }
        tileCornerVelocityGrid[col][row] = velocity;
    }

    public void setCornerVelocityCallback(DisplayVector displayVector)
    {
        int col = displayVector.col_index;
        int row = displayVector.row_index;
        Vector2 velocity = displayVector.Value;
        setCornerVelocityCallback(col, row, velocity);
    }


    
    public Node2D[][] tileGrid;


    public override void _Ready()
    {
        CreateBoard();
    }

    private float GetPressureSafe(int col, int row)
    {
        if (col < 0 || col >= NCols || row < 0 || row >= NRows)
        {
            return 0f;
        }
        return tilePressureGrid[col][row];
    }

    private Vector2 GetVelocitySafe(int col, int row)
    {
        if (col < 0 || col >= NCols || row < 0 || row >= NRows)
        {
            return Vector2.Zero;
        }
        return tileCornerVelocityGrid[col][row];
    }

    
    private float GetPressureAtTile(int col, int row)
    {
        //Calculate the pressure at the center of the tile
        //This is based on the divergence of the velocity field at the corners
        Vector2 vTopLeft = GetVelocitySafe(col, row);
        Vector2 vTopRight = GetVelocitySafe(col + 1, row);
        Vector2 vBottomLeft = GetVelocitySafe(col, row + 1);
        Vector2 vBottomRight = GetVelocitySafe(col + 1, row + 1);

        float dx = (vTopRight.X - vTopLeft.X + vBottomRight.X - vBottomLeft.X) * 0.5f;
        float dy = (vBottomLeft.Y - vTopLeft.Y + vBottomRight.Y - vTopRight.Y) * 0.5f;


        float pTopLeft = GetPressureSafe(col - 1, row - 1);
        float pTopMiddle = GetPressureSafe(col, row - 1);
        float pTopRight = GetPressureSafe(col + 1, row - 1);
        float pMiddleLeft = GetPressureSafe(col - 1, row);
        float pMiddleRight = GetPressureSafe(col + 1, row);
        float pBottomLeft = GetPressureSafe(col - 1, row + 1);
        float pBottomMiddle = GetPressureSafe(col, row + 1);
        float pBottomRight = GetPressureSafe(col + 1, row + 1);

        float dpdx = (pTopRight + pMiddleRight + pBottomRight - pTopLeft - pMiddleLeft - pBottomLeft) / 3f;

        float divergence = dx + dy;

        //Pressure is proportional to negative divergence
        float pressure = -density * divergence;

        return pressure;
    }


    private void CreateBoard()
    {
        //Clear existing pegs
        foreach (Node2D child in GetChildren())
        {
            if (child is not Area2D)
            {
                child.QueueFree();
            }
        }

        if (TileScene == null || FluidAreaShape.Shape == null)
        {
            GD.Print("TileScene or FluidAreaShape is null, cannot create board.");
            return;
        }

        Node2D abstractTile = TileScene.Instantiate<Node2D>();
        //Get the size of the tile
        Area2D tileArea = abstractTile.GetNode<Area2D>("Area2D");
        CollisionShape2D tileAreaShape = tileArea.GetNode<CollisionShape2D>("CollisionShape2D");
        Rect2 tileRect = tileAreaShape.Shape.GetRect();
        Vector2 tileSize = tileRect.Size;
        abstractTile.QueueFree();

        //Get the size of the area
        Rect2 areaRect = FluidAreaShape.Shape.GetRect();
        Vector2 areaSize = areaRect.Size;

        //Calculate spacing
        //float xSpacing = areaSize.X / (NCols - 1);
        //float ySpacing = areaSize.Y / (NRows - 1);
        float xSpacing = tileSize.X;
        float ySpacing = tileSize.Y;


        //Find the middle of the tiles
        float xLength = (NCols) * xSpacing;
        float yLength = (NRows) * ySpacing;
        Vector2 gridSize = new Vector2(xLength, yLength);
        Vector2 gridOffset = (areaSize - gridSize) / 2;

        tileGrid = new Node2D[NCols][];
        tilePressureGrid = new float[NCols][];
        //Spawn tiles in a grid pattern
        for (int col = 0; col < NCols; col++)
        {
            tileGrid[col] = new Node2D[NRows];
            tilePressureGrid[col] = new float[NRows];
            for (int row = 0; row < NRows; row++)
            {
                Vector2 position = new Vector2(col * xSpacing, row * ySpacing) + gridOffset;
                var tileInstance = TileScene.Instantiate<Node2D>();
                tileInstance.Position = position;
                AddChild(tileInstance);
                tileGrid[col][row] = tileInstance;
                //Set the tile's fluid property to this
                tilePressureGrid[col][row] = 0f;
            }
        }

        //Initialize the tile corner velocity grid
        tileCornerVelocityGrid = new Vector2[NCols + 1][];
        for (int i = 0; i <= NCols; i++)
        {
            tileCornerVelocityGrid[i] = new Vector2[NRows + 1];
            for (int j = 0; j <= NRows; j++)
            {
                //Initialize all velocities to zero
                Vector2 randomVelocity = new Vector2(
                    (float)GD.RandRange(-50, 50),
                    (float)GD.RandRange(-50, 50)
                );



                tileCornerVelocityGrid[i][j] = randomVelocity;
                DisplayVector vectorInstance = VectorScene.Instantiate<DisplayVector>();
                    if (vectorInstance is not DisplayVector)
                    {
                        GD.PrintErr("VectorScene is not a DisplayVector");
                        return;
                    }
                DisplayVector displayVector = vectorInstance as DisplayVector;
                displayVector.col_index = i;
                displayVector.row_index = j;

                displayVector.Value = randomVelocity;
                displayVector.OnVectorChanged = setCornerVelocityCallback;
                AddChild(vectorInstance);
                vectorInstance.Position = new Vector2(i * xSpacing, j * ySpacing) + gridOffset; //Offset to center the vector on the corner
            }
        }




    }
}
