using Godot;
using System;


[Tool]
public partial class Fluid : Node2D
{
    //This class is responsible for populating the scene with objects
    //It should have a property for the object to spawn, as well as the parent node
    [Export] public PackedScene TileScene { get; set; }
    [Export] public PackedScene VectorScene { get; set; }


    private bool _stepSimulation;
    //Make a button in the UI to calculate the next step
    [Export]
    public bool StepSimulation
    {
        get => _stepSimulation;
        set
        {
            _stepSimulation = value;
            _Process(0);
        }
    }
    
    [Export] public int SimulationStepsPerFrame { get; set; } = 10;


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
    public float cellSize = 1f;

    [Export] public float MaxVelocity = 5f;

    public DisplayVector[][] vectorGrid;
    public DisplayTile[][] tileGrid;


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


    public override void _Ready()
    {
        CreateBoard();
        _stepSimulation = true;
        _Process(0);
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
        if (col <= 0 || col >= NCols || row <= 0 || row >= NRows)
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

        float dx = (vTopLeft.X - vTopRight.X + vBottomLeft.X - vBottomRight.X);
        float dy = (vTopLeft.Y - vBottomLeft.Y + vTopRight.Y - vBottomRight.Y);
        float divergence = (dx + dy) / 2f;

        float pTopLeft = GetPressureSafe(col - 1, row - 1);
        float pTopMiddle = GetPressureSafe(col, row - 1);
        float pTopRight = GetPressureSafe(col + 1, row - 1);
        float pMiddleLeft = GetPressureSafe(col - 1, row);
        float pMiddleRight = GetPressureSafe(col + 1, row);
        float pBottomLeft = GetPressureSafe(col - 1, row + 1);
        float pBottomMiddle = GetPressureSafe(col, row + 1);
        float pBottomRight = GetPressureSafe(col + 1, row + 1);

        float pressureSum = pTopLeft + pTopMiddle + pTopRight +
                            pMiddleLeft + pMiddleRight +
                            pBottomLeft + pBottomMiddle + pBottomRight;


        //Pressure is proportional to negative divergence
        float pressure = (pressureSum - density * cellSize * divergence / timeStep) / 8f;

        return pressure;
    }

    private Vector2 GetVelocityAtCorner(int col, int row)
    {
        //Calculate the new velocity at the corner based on the average of the surrounding tiles' pressures
        float pTopLeft = GetPressureSafe(col - 1, row - 1);
        float pTopRight = GetPressureSafe(col, row - 1);
        float pBottomLeft = GetPressureSafe(col - 1, row);
        float pBottomRight = GetPressureSafe(col, row);

        Vector2 pressureGradient = new Vector2(
            (pTopLeft + pBottomLeft - pTopRight - pBottomRight) / (2 * cellSize),
            (pTopLeft + pTopRight - pBottomLeft - pBottomRight) / (2 * cellSize)
        );

        Vector2 currentVelocity = GetVelocitySafe(col, row);
        Vector2 newVelocity = currentVelocity - (pressureGradient / density) * timeStep;

        //Apply simple viscosity
        //newVelocity *= (1f - viscosity);

        return newVelocity;
    }

    public void Simulate()
    {
        //Calculate the new pressure values for each tile
        for (int col = 0; col < NCols; col++)
        {
            for (int row = 0; row < NRows; row++)
            {
                float newPressure = GetPressureAtTile(col, row);
                tilePressureGrid[col][row] = newPressure;
                tileGrid[col][row].UpdateColor(newPressure);
            }
        }
        //Calculate the new velocity values for each corner
        for (int col = 0; col <= NCols; col++)
        {
            for (int row = 0; row <= NRows; row++)
            {
                Vector2 newVelocity = GetVelocityAtCorner(col, row);
                tileCornerVelocityGrid[col][row] = newVelocity;
                //newVelocity = GetVelocitySafe(col, row);
                //tileCornerVelocityGrid[col][row] = newVelocity;
                vectorGrid[col][row].Value = newVelocity;
            }
        }

    }


    public override void _Process(double delta)
    {
        if (_stepSimulation)
        {
            //If we are in the editor, we need to reset the flag
            if (Engine.IsEditorHint())
                _stepSimulation = false;

            for (int i = 0; i < SimulationStepsPerFrame; i++)
            {
                Simulate();
            }
        }
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

        DisplayTile abstractTile = TileScene.Instantiate<DisplayTile>();
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

        tileGrid = new DisplayTile[NCols][];
        tilePressureGrid = new float[NCols][];
        //Spawn tiles in a grid pattern
        for (int col = 0; col < NCols; col++)
        {
            tileGrid[col] = new DisplayTile[NRows];
            tilePressureGrid[col] = new float[NRows];
            for (int row = 0; row < NRows; row++)
            {
                Vector2 position = new Vector2(col * xSpacing, row * ySpacing) + gridOffset;
                var tileInstance = TileScene.Instantiate<DisplayTile>();
                tileInstance.Position = position;
                AddChild(tileInstance);
                tileGrid[col][row] = tileInstance;
                //Set the tile's fluid property to this
                tilePressureGrid[col][row] = 0f;
            }
        }

        //Initialize the tile corner velocity grid
        tileCornerVelocityGrid = new Vector2[NCols + 1][];
        vectorGrid = new DisplayVector[NCols + 1][];
        for (int i = 0; i <= NCols; i++)
        {
            tileCornerVelocityGrid[i] = new Vector2[NRows + 1];
            vectorGrid[i] = new DisplayVector[NRows + 1];
            for (int j = 0; j <= NRows; j++)
            {
                //Initialize all velocities to zero
                Vector2 randomVelocity = new Vector2(
                    (float)GD.RandRange(-50, 50) / 50 * MaxVelocity,
                    (float)GD.RandRange(-50, 50) / 50 * MaxVelocity
                );



                tileCornerVelocityGrid[i][j] = randomVelocity;
                randomVelocity = GetVelocitySafe(i, j);
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
                vectorGrid[i][j] = displayVector;
                AddChild(vectorInstance);
                vectorInstance.Position = new Vector2(i * xSpacing, j * ySpacing) + gridOffset; //Offset to center the vector on the corner
            }
        }




    }
}
