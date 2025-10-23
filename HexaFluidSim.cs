using Godot;
using System;
public class HexaFluidSimBaseClass: FluidSimBaseClass
{
    public new Vector3[][] velocityGrid;
    public new Vector3[][] tempVelocityGrid;

    public HexaFluidSimBaseClass(int n_cols, int n_rows, float viscosity, float timeStep, float density, float cellSize, float maxVelocity)
        : base(n_cols, n_rows, viscosity, timeStep, density, cellSize, maxVelocity)
    {
    }
    public new virtual Vector3 getZeroVector()
    {
        return Vector3.Zero;
    }

    public override void CreateGrid()
    {   
        GD.Print("Creating Hexagonal Fluid Simulation Grid");
        //Initialize the tile corner velocity grid
        velocityGrid = new Vector3[nCols + 1][];
        tempVelocityGrid = new Vector3[nCols + 1][];
        pressureGrid = new float[nCols][];
        tempPressureGrid = new float[nCols][];
        for (int i = 0; i <= nCols; i++)
        {
            velocityGrid[i] = new Vector3[nRows + 1];
            tempVelocityGrid[i] = new Vector3[nRows + 1];
            if (i < nCols)
            {
                pressureGrid[i] = new float[nRows];
                tempPressureGrid[i] = new float[nRows];
            }
            for (int j = 0; j <= nRows; j++)
            {

                Vector3 velocity = Vector3.Zero;
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
        GD.Print("Hexagonal Fluid Simulation Grid Created, ", velocityGrid, " length: ", velocityGrid.Length);
    }

    public virtual new Vector3 GetRandomVelocity()
    {
        return new Vector3(
            (float)GD.RandRange(-100, 100) / 100 * maxVelocity,
            (float)GD.RandRange(-100, 100) / 100 * maxVelocity,
            (float)GD.RandRange(-100, 100) / 100 * maxVelocity
        );
    }

    public override Vector2 GetVelocityUV(int col, int row)
    {
        float u = (float)col / nCols;
        float v = (float)(row) / nRows;
        if (row % 2 == 1)
        {
            // Odd rows are shifted
            u += 0.5f / nCols;
        }
        return new Vector2(u, v);
    }

    public override Vector2 GetTileUV(int col, int row)
    {

        float u = (float)col / nCols;
        float v = (float)(row) / nRows;
        if (row % 2 == 1)
        {
            // Odd rows are shifted
            u += 0.5f / nCols;
        }
        return new Vector2(u, v);
    }

    protected new virtual Vector3 GetVelocitySafe(int col, int row)
    {
        if (!IsCornerValid(col, row))
        {
            return getZeroVector();
        }
        return velocityGrid[col][row];
    }

    public new virtual Vector3 UpdateVelocity(int col, int row)
    {
        return GetVelocitySafe(col, row);
    }

    public new virtual Vector3[][] UpdateAndGetVelocityMap()
    {
        UpdateVelocityMap();
        return velocityGrid;
    }

}

[Tool]
public partial class HexaFluidSim : Node2D
{
}
