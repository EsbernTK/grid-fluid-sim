using Godot;
using System;


public class FluidSimBaseClass
{
    public Vector2[][] velocityGrid;
    public Vector2[][] tempVelocityGrid;
    public float[][] pressureGrid;
    public float[][] tempPressureGrid;

    public float viscosity = 0.1f;
    public float timeStep = 1f / 60f;
    public float density = 1f;
    public float cellSize = 1f;
    public float maxVelocity = 5f;

    public int nCols;
    public int nRows;


    public FluidSimBaseClass(int n_cols, int n_rows, float viscosity, float timeStep, float density, float cellSize, float maxVelocity)
    {
        this.viscosity = viscosity;
        this.timeStep = timeStep;
        this.density = density;
        this.cellSize = cellSize;
        this.maxVelocity = maxVelocity;
        this.nCols = n_cols;
        this.nRows = n_rows;
        CreateGrid();
    }

    public virtual void CreateGrid()
    {
        //Initialize the tile corner velocity grid
        velocityGrid = new Vector2[nCols + 1][];
        tempVelocityGrid = new Vector2[nCols + 1][];
        pressureGrid = new float[nCols][];
        tempPressureGrid = new float[nCols][];
        for (int i = 0; i <= nCols; i++)
        {
            velocityGrid[i] = new Vector2[nRows + 1];
            tempVelocityGrid[i] = new Vector2[nRows + 1];
            if (i < nCols)
            {
                pressureGrid[i] = new float[nRows];
                tempPressureGrid[i] = new float[nRows];
            }
            for (int j = 0; j <= nRows; j++)
            {

                Vector2 velocity = Vector2.Zero;
                if (IsCornerValid(i, j))
                {
                    velocity = GetRandomVelocity();
                }
                //Initialize all velocities to zero
                velocityGrid[i][j] = velocity;
                tempVelocityGrid[i][j] = velocity;
                if (i < nCols && j < nRows)
                {
                    pressureGrid[i][j] = 0f;
                    tempPressureGrid[i][j] = 0f;
                }
            }
        }
    }

    public virtual void RandomizeVelocityGrid()
    {
        for (int i = 0; i <= nCols; i++)
        {
            for (int j = 0; j <= nRows; j++)
            {
                if (IsCornerValid(i, j))
                {
                    velocityGrid[i][j] = GetRandomVelocity();
                }
                else
                {
                    velocityGrid[i][j] = Vector2.Zero;
                }
            }
        }
    }

    public virtual void RandomizePressureGrid()
    {
        for (int i = 0; i < nCols; i++)
        {
            for (int j = 0; j < nRows; j++)
            {
                pressureGrid[i][j] = (float)GD.RandRange(-100, 100) / 100;
            }
        }
    }

    public virtual void RandomizeGrid()
    {
        RandomizeVelocityGrid();
        RandomizePressureGrid();
    }

    public virtual Vector2 GetVelocityUV(int col, int row)
    {
        //Returns where the velocity is in UV space
        float u = (float)col / nCols;
        float v = (float)row / nRows;
        return new Vector2(u, v);
    }

    public virtual Vector2 GetRandomVelocity()
    {
        return new Vector2(
            (float)GD.RandRange(-100, 100) / 100 * maxVelocity,
            (float)GD.RandRange(-100, 100) / 100 * maxVelocity
        );
    }

    //Simulation methods would go here
    protected virtual bool IsTileValid(int col, int row)
    {
        return (col >= 0 && col < nCols && row >= 0 && row < nRows);
    }

    protected virtual float GetPressureSafe(int col, int row)
    {
        if (!IsTileValid(col, row))
        {
            return 0f;
        }
        return pressureGrid[col][row];
    }

    protected virtual bool IsCornerValid(int col, int row)
    {
        return (col > 0 && col < nCols && row > 0 && row < nRows);
    }

    protected virtual Vector2 GetVelocitySafe(int col, int row)
    {
        if (!IsCornerValid(col, row))
        {
            return Vector2.Zero;
        }
        return velocityGrid[col][row];
    }

    public virtual float UpdatePressure(int col, int row)
    {
        return GetPressureSafe(col, row);
    }

    public virtual Vector2 UpdateVelocity(int col, int row)
    {
        return GetVelocitySafe(col, row);
    }

    public virtual float[][] UpdatePressureMap()
    {
        //Calculate the new pressure values for each tile
        for (int col = 0; col < nCols; col++)
        {
            for (int row = 0; row < nRows; row++)
            {
                float newPressure = UpdatePressure(col, row);
                tempPressureGrid[col][row] = newPressure;
            }
        }
        //Swap pressure grids
        pressureGrid = tempPressureGrid;
        return pressureGrid;
    }

    
    public virtual Vector2[][] UpdateVelocityMap()
    {
        //Calculate the new velocity values for each corner
        for (int col = 0; col <= nCols; col++)
        {
            for (int row = 0; row <= nRows; row++)
            {
                Vector2 newVelocity = UpdateVelocity(col, row);

                if (!IsCornerValid(col, row))
                {
                    newVelocity = Vector2.Zero;
                }

                tempVelocityGrid[col][row] = newVelocity;
            }
        }
        //Swap velocity grids
        velocityGrid = tempVelocityGrid;
        return velocityGrid;
    }

    

}

public class FluidCornerSim : FluidSimBaseClass
{


    public FluidCornerSim(int n_cols, int n_rows, float viscosity, float timeStep, float density, float cellSize, float maxVelocity)
        : base(n_cols, n_rows, viscosity, timeStep, density, cellSize, maxVelocity)
    {
    }


    public override Vector2 GetVelocityUV(int col, int row)
    {
        //Returns where the velocity is in UV space
        float u = (float)col / nCols;
        float v = (float)row / nRows;
        return new Vector2(u, v);
    }

    public float GetPressureAtTile(int col, int row)
    {
        //Calculate the pressure at the center of the tile
        //This is based on the divergence of the velocity field at the corners
        Vector2 vTopLeft = GetVelocitySafe(col, row);
        Vector2 vTopRight = GetVelocitySafe(col + 1, row);
        Vector2 vBottomLeft = GetVelocitySafe(col, row + 1);
        Vector2 vBottomRight = GetVelocitySafe(col + 1, row + 1);


        Vector2 vTop = (vTopLeft + vTopRight) / 2f;
        Vector2 vBottom = (vBottomLeft + vBottomRight) / 2f;
        Vector2 vLeft = (vTopLeft + vBottomLeft) / 2f;
        Vector2 vRight = (vTopRight + vBottomRight) / 2f;

        float dx = vLeft.X - vRight.X;
        float dy = vTop.Y - vBottom.Y;
        //float dx = (vRight.X - vLeft.X);
        //float dy = (vBottom.Y - vTop.Y);

        //float dx = (vTopLeft.X - vTopRight.X + vBottomLeft.X - vBottomRight.X);
        //float dy = (vTopLeft.Y - vBottomLeft.Y + vTopRight.Y - vBottomRight.Y);


        float divergence = dx + dy;

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

        //float pressureSum =  0f + pTopMiddle + 0f +
        //                    pMiddleLeft + 0f + pMiddleRight +
        //                    0f + pBottomMiddle + 0f;

        //float K = timeStep / (density * cellSize);
        //Pressure is proportional to negative divergence
        float pressure = (pressureSum - density * cellSize * divergence / timeStep) / 8f;
        //float pressure = (pressureSum - divergence * K) / 8f;

        if (col == 0 && row == 0)
        {
            GD.Print("Tile (", col, ", ", row, ") divergence: ", divergence, " pressure sum: ", pressureSum, " new pressure: ", pressure, " corner velocities: TL", vTopLeft, " TR", vTopRight, " BL", vBottomLeft, " BR", vBottomRight, "Top", vTop, " Bottom", vBottom, " Left", vLeft, " Right", vRight);
        }


        return pressure;
    }


    public Vector2 GetVelocityAtCorner(int col, int row)
    {
        //Calculate the new velocity at the corner based on the average of the surrounding tiles' pressures
        float pTopLeft = GetPressureSafe(col - 1, row - 1);
        float pTopRight = GetPressureSafe(col, row - 1);
        float pBottomLeft = GetPressureSafe(col - 1, row);
        float pBottomRight = GetPressureSafe(col, row);

        //Vector2 pressureGradient = new Vector2(
        //    (pTopLeft + pBottomLeft - pTopRight - pBottomRight) / (4 * cellSize),
        //    (pTopLeft + pTopRight - pBottomLeft - pBottomRight) / (4 * cellSize)
        //);

        float pTop = (pTopLeft + pTopRight) / 2f;
        float pBottom = (pBottomLeft + pBottomRight) / 2f;
        float pLeft = (pTopLeft + pBottomLeft) / 2f;
        float pRight = (pTopRight + pBottomRight) / 2f;

        float pDiagBottomLeft = (pBottomLeft + pTopLeft / 2 + pBottomRight / 2) / 2f;
        float pDiagTopRight = (pTopRight + pBottomRight / 2 + pTopLeft / 2) / 2f;



        float pDiagBottomLeftTopRightGradient = pDiagBottomLeft - pDiagTopRight;
        //float pDiagBottomLeftTopRightX = pDiagBottomLeftTopRightGradient  * Mathf.Cos(Mathf.Pi / 4);
        //float pDiagBottomLeftTopRightY = pDiagBottomLeftTopRightGradient  * Mathf.Sin(Mathf.Pi / 4);

        float pDiagTopLeft = (pTopLeft + pTopRight / 2 + pBottomLeft / 2) / 2f;
        float pDiagBottomRight = (pBottomRight + pBottomLeft / 2 + pTopRight / 2) / 2f;

        float pDiagTopLeftBottomRightGradient = pDiagTopLeft - pDiagBottomRight;

        //float pDiagTopLeftBottomRightX = pDiagTopLeftBottomRightGradient * Mathf.Cos(Mathf.Pi / 4 );
        //float pDiagTopLeftBottomRightY = -pDiagTopLeftBottomRightGradient  * Mathf.Sin(Mathf.Pi / 4);

        float pTopBottomGradient = pTop - pBottom;
        float pLeftRightGradient = pLeft - pRight;


        Vector2 TopVector = new Vector2(0, pTopBottomGradient);
        Vector2 RightVector = new Vector2(pLeftRightGradient, 0);
        Vector2 DiagBLTRVector = new Vector2(1, -1).Normalized() * pDiagBottomLeftTopRightGradient;
        Vector2 DiagTLBRVector = new Vector2(-1, 1).Normalized() * pDiagTopLeftBottomRightGradient;
        Vector2 pressureGradient = TopVector + RightVector + DiagBLTRVector + DiagTLBRVector;
        // Vector2 pressureGradient = new Vector2(
        //    (pLeft - pRight) / cellSize,
        //    (pTop - pBottom) / cellSize
        //);

        //Vector2 pressureGradient = new Vector2(
        //    (pLeft - pRight + pDiagBottomLeftTopRightX - pDiagTopLeftBottomRightX) / cellSize,
        //    (pTop - pBottom + pDiagBottomLeftTopRightY - pDiagTopLeftBottomRightY) / cellSize
        //);

        Vector2 currentVelocity = GetVelocitySafe(col, row);
        float K = timeStep / (density * cellSize);

        if (col < 2 && row < 2)
            GD.Print("Corner (", col, ", ", row, ") pressure gradient: ", K * pressureGradient, " current velocity: ", currentVelocity);

        Vector2 newVelocity = currentVelocity - K * pressureGradient;

        //Apply simple viscosity
        //newVelocity *= (1f - viscosity);

        return newVelocity;
    }

    public override Vector2 UpdateVelocity(int col, int row)
    {
        return GetVelocityAtCorner(col, row);
    }

    public override float UpdatePressure(int col, int row)
    {
        return GetPressureAtTile(col, row);
    }

}


public class FluidEdgeSim : FluidSimBaseClass
{
    public FluidEdgeSim(int n_cols, int n_rows, float viscosity, float timeStep, float density, float cellSize, float maxVelocity)
        : base(n_cols, n_rows, viscosity, timeStep, density, cellSize, maxVelocity)
    {
    }

    //public override Vector2 GetVelocityUV(int col, int row)
    //{
    //    //Returns where the velocity is in UV space
    //    float u = (float)(col + 0.5f) / nCols;
    //    float v = (float)(row + 0.5f) / nRows;
    //    return new Vector2(u, v);
    //}

    protected override Vector2 GetVelocitySafe(int col, int row)
    {
        if (col < 0 || col > nCols || row < 0 || row > nRows)
        {
            return Vector2.Zero;
        }

        Vector2 vel = velocityGrid[col][row];
        if (col == 0 || col == nCols)
        {
            vel.X = 0f;
        }
        if (row == 0 || row == nRows)
        {
            vel.Y = 0f;
        }
        return vel;
    }
    
    protected override bool IsCornerValid(int col, int row)
    {
        return (col >= 0 && col <= nCols && row >= 0 && row <= nRows);
    }

    public float GetPressureAtTile(int col, int row)
    {        //Calculate the pressure at the center of the tile
        //This is based on the divergence of the velocity field at the edges
        Vector2 vTopLeft = GetVelocitySafe(col, row);
        Vector2 vBottomLeft = GetVelocitySafe(col, row + 1);
        Vector2 vTopRight = GetVelocitySafe(col + 1, row);

        float topVelocity = vTopLeft.Y;
        float bottomVelocity = vBottomLeft.Y;
        float leftVelocity = vTopLeft.X;
        float rightVelocity = vTopRight.X;
        
        float dx = leftVelocity - rightVelocity;
        float dy = topVelocity - bottomVelocity;


        float pressureTop = GetPressureSafe(col, row - 1);
        float pressureBottom = GetPressureSafe(col, row + 1);
        float pressureLeft = GetPressureSafe(col - 1, row);
        float pressureRight = GetPressureSafe(col + 1, row);

        float pressureSum = pressureTop + pressureBottom + pressureLeft + pressureRight;
        float divergence = dx + dy;

        float pressure = (pressureSum - density * cellSize * divergence / timeStep) / 4f;
        return pressure;
    }
    
    public Vector2 GetVelocityAtEdge(int col, int row)
    {
        //Calculate the new velocity at the edge based on the surrounding tiles' pressures
        //Velocity vectors are stored at the top left corner of tile
        float pLeft = GetPressureSafe(col - 1, row);
        float pRight = GetPressureSafe(col, row);
        float pTop = GetPressureSafe(col, row - 1);
        float pBottom = GetPressureSafe(col, row);

        float pressureGradientX = pLeft - pRight;
        float pressureGradientY = pTop - pBottom;

        Vector2 currentVelocity = GetVelocitySafe(col, row);
        float K = timeStep / (density * cellSize);

        Vector2 newVelocity = currentVelocity - K * new Vector2(pressureGradientX, pressureGradientY);

        //Apply simple viscosity
        //newVelocity *= (1f - viscosity);

        return newVelocity;
    }

    //Implement edge-based simulation methods here
        public override Vector2 UpdateVelocity(int col, int row)
    {
        return GetVelocityAtEdge(col, row);
    }

    public override float UpdatePressure(int col, int row)
    {
        return GetPressureAtTile(col, row);
    }
}



[Tool]
public partial class FluidSim : Node2D
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


    private bool _randomizeSimulation;
    //Make a button in the UI to calculate the next step
    [Export]
    public bool RandomizeSimulation
    {
        get => _randomizeSimulation;
        set
        {
            _randomizeSimulation = false;
            if(this._fluidSim == null)
            {
                GD.PrintErr("FluidSim is null, cannot randomize.");
                return;
            }
            this._fluidSim.RandomizeGrid();
            StepSimulation = true;
        }
    }


    [Export] public int SimulationStepsPerFrame { get; set; } = 10;
    [Export] public bool UpdateVelocities { get; set; } = true;
    [Export] public bool UpdatePressures { get; set; } = true;

    [Export] public bool SimulateInEditor { get; set; } = true;
    
    private bool _simulateAtCorners;
    //Make a button in the UI to calculate the next step
    [Export]
    public bool SimulateAtCorners
    {
        get => _simulateAtCorners;
        set
        {
            _simulateAtCorners = value;
            CreateBoard();
            StepSimulation = true;
        }
    }
    


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

    public float viscosity = 0.1f;
    public float timeStep = 1f / 60f;
    public float density = 1f;
    public float cellSize = 1f;

    [Export] public float MaxVelocity = 5f;

    public DisplayVector[][] vectorGrid;
    public DisplayTile[][] tileGrid;


    private FluidSimBaseClass _fluidSim;

    public void setCornerVelocityCallback(int col, int row, Vector2 velocity)
    {
        if (col < 0 || col > NCols || row < 0 || row > NRows)
        {
            GD.PrintErr("Corner velocity callback out of bounds: ", col, ", ", row);
            return;
        }
        //tileCornerVelocityGrid[col][row] = velocity;
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
        GD.Print("FluidSim ready, creating board.", this._fluidSim);
        CreateBoard();
        _stepSimulation = true;
        _Process(0);
    }

    public void Simulate()
    {   
        
        if (UpdatePressures)
        {
            float[][] updatedPressure = _fluidSim.UpdatePressureMap();
            for (int col = 0; col < NCols; col++)
            {
                for (int row = 0; row < NRows; row++)
                {
                    tileGrid[col][row].UpdateColor(updatedPressure[col][row]);
                }
            }


        }

        if (UpdateVelocities)
        {
            Vector2[][] updatedVelocities = _fluidSim.UpdateVelocityMap();
            for (int col = 0; col <= NCols; col++)
            {
                for (int row = 0; row <= NRows; row++)
                {
                    vectorGrid[col][row].Value = updatedVelocities[col][row];
                }
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

        if( SimulateAtCorners)
            this._fluidSim = new FluidCornerSim(NCols, NRows, viscosity, timeStep, density, cellSize, MaxVelocity);
        else
            this._fluidSim = new FluidEdgeSim(NCols, NRows, viscosity, timeStep, density, cellSize, MaxVelocity);

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
        float xLength = NCols * xSpacing;
        float yLength = NRows * ySpacing;
        Vector2 gridSize = new Vector2(xLength, yLength);
        Vector2 gridOffset = (areaSize - gridSize) / 2;



        float[][] pressureMap = _fluidSim.pressureGrid;
        Vector2[][] velocityMap = _fluidSim.velocityGrid;


        tileGrid = new DisplayTile[pressureMap.Length][];
        vectorGrid = new DisplayVector[velocityMap.Length][];

        //Spawn tiles in a grid pattern
        for (int col = 0; col < pressureMap.Length; col++)
        {
            tileGrid[col] = new DisplayTile[pressureMap[col].Length];
            for (int row = 0; row < pressureMap[col].Length; row++)
            {
                Vector2 position = new Vector2(col * xSpacing, row * ySpacing) + gridOffset;
                var tileInstance = TileScene.Instantiate<DisplayTile>();
                tileInstance.Position = position;
                AddChild(tileInstance);
                tileGrid[col][row] = tileInstance;
                //Set the tile's fluid property to this
                float pressure = pressureMap[col][row];
                tileInstance.UpdateColor(pressure);
            }
        }

        for (int i = 0; i < velocityMap.Length; i++)
        {
            vectorGrid[i] = new DisplayVector[velocityMap[i].Length];
            for (int j = 0; j < velocityMap[i].Length; j++)
            {
                Vector2 velocity = velocityMap[i][j];
                DisplayVector vectorInstance = VectorScene.Instantiate<DisplayVector>();
                if (vectorInstance is not DisplayVector)
                {
                    GD.PrintErr("VectorScene is not a DisplayVector");
                    return;
                }
                DisplayVector displayVector = vectorInstance as DisplayVector;
                displayVector.col_index = i;
                displayVector.row_index = j;

                displayVector.Value = velocity;
                displayVector.OnVectorChanged = setCornerVelocityCallback;
                vectorGrid[i][j] = displayVector;
                AddChild(vectorInstance);

                Vector2 velocityUV = _fluidSim.GetVelocityUV(i, j);
                vectorInstance.Position = new Vector2(velocityUV.X * xLength, velocityUV.Y * yLength) + gridOffset; //Offset to center the vector on the corner
            }
        }

    }
   
}
