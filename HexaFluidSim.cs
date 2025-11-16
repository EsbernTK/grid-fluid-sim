using Godot;
using System;
public class HexaFluidSimBaseClass : FluidSimBaseClass<Vector3>
{

    public HexaFluidSimBaseClass(int n_cols, int n_rows, float viscosity, float timeStep, float density, float cellSize, float maxVelocity)
        : base(n_cols, n_rows, viscosity, timeStep, density, cellSize, maxVelocity)
    {
    }

    public override Vector2 GetVelocityUV(int col, int row)
    {
        float u = (float)col / nCols;
        float v = (float)(row) / nRows;
        if (row % 2 == 1)
        {
            // Odd rows are shifted
            u -= 0.5f / nCols;
        }
        return new Vector2(u, v);
    }

        public override Vector2 GetVelocityUV(float col, float row)
    {
        float u = (float)col / nCols;
        float v = (float)(row) / nRows;
        if (row % 2 == 1)
        {
            // Odd rows are shifted
            u -= 0.5f / nCols;
        }
        return new Vector2(u, v);
    }

    public override Vector2 GetTileUV(int col, int row)
    {

        float u = (float)col / (nCols - 1);
        float v = (float)(row) / (nRows - 1);
        if (row % 2 == 1)
        {
            // Odd rows are shifted
            u += 0.5f / (nCols - 1);
        }
        return new Vector2(u, v);
    }

    protected override bool IsCornerValid(int col, int row)
    {
        //In a hex grid, corners are valid if they are not on the left edge of an even row or the right edge of an odd row
        if (col < 0 || col > nCols || row <= 0 || row > nRows)
        {
            return false;
        }

        if (row % 2 == 1 && col == 0)
        {
            return false;
        }
        if (row % 2 == 0 && col == nCols)
        {
            return false;
        }
        return true;
    }

    protected virtual Vector3 GetValidVelocityAxis(int col, int row)
    {
        Vector3 vel = Vector3.One;
        if (row == 0)
        {
            return Vector3.Zero;
        }

        if (col == 0 || col == nCols)
        {
            vel.Y = 0f;
            if (col == 0 || (row % 2) == 0)
            {
                vel.X = 0f;
            }
            if (col == nCols)
            {
                vel.Z = 0f;
            }
        }

        if (row == nRows)
        {
            vel.X = 0f;
            vel.Z = 0f;
        }
        return vel;
    }


    protected override Vector3 GetVelocitySafe(int col, int row)
    {
        if (col < 0 || col > nCols || row < 0 || row > nRows)
        {
            return Vector3.Zero;
        }

        Vector3 vel = velocityGrid[col][row];
        Vector3 validAxis = GetValidVelocityAxis(col, row);
        vel.X *= validAxis.X;
        vel.Y *= validAxis.Y;
        vel.Z *= validAxis.Z;
        return vel;
    }

    public override int[][] GetNeighbourTileInds(int col, int row)
    {
        //Returns the column and row indices of the 6 surrounding tiles in a hex grid
        //The order is: Top-Left, Top-Right, Right, Bottom-Right, Bottom-Left, Left
        if (row % 2 == 0)
        {
            return new int[][]
            {
                new int[] { col - 1, row - 1 }, // Top-Left
                new int[] { col, row - 1 },     // Top-Right
                new int[] { col + 1, row },     // Right
                new int[] { col, row + 1 },     // Bottom-Right
                new int[] { col - 1, row + 1 }, // Bottom-Left
                new int[] { col - 1, row }      // Left
            };
        }
        else
        {
            return new int[][]
            {
                new int[] { col, row - 1 },     // Top-Left
                new int[] { col + 1, row - 1 }, // Top-Right
                new int[] { col + 1, row },     // Right
                new int[] { col + 1, row + 1 }, // Bottom-Right
                new int[] { col, row + 1 },     // Bottom-Left
                new int[] { col - 1, row }      // Left
            };
        }
    }

    private string Vector2ArrayToString(Vector2[] vectors)
    {
        string result = "[";
        for (int i = 0; i < vectors.Length; i++)
        {
            result += "(" + vectors[i].X + ", " + vectors[i].Y + ")";
            if (i < vectors.Length - 1)
            {
                result += ", ";
            }
        }
        result += "]";
        return result;
    }
    public Vector2[] GetTileEdgeVelocities(int col, int row)
    {
        //Returns the velocities at the 6 edges of the tile in a hex grid
        //The order is: Top-Left, Top-Right, Right, Bottom-Right, Bottom-Left, Left
        //Each velocity is stored in a vector3 where:
        // X = velocity along left edge
        // Y = velocity along top edge
        // Z = velocity along right edge

        Vector2[] edgeVelocities = new Vector2[6];
        //Assume we are on an even row
        Vector3 topVelocity = GetVelocitySafe(col, row);
        Vector3 bottomLeftVelocity = GetVelocitySafe(col, row + 1);
        Vector3 bottomRightVelocity = GetVelocitySafe(col + 1, row + 1);


        if (row % 2 == 1)
        {
            topVelocity = GetVelocitySafe(col + 1, row);
            bottomLeftVelocity = GetVelocitySafe(col, row + 1);
            bottomRightVelocity = GetVelocitySafe(col + 1, row + 1);
        }


        float sixtyDegrees = Mathf.DegToRad(60);
        float cos60 = Mathf.Cos(sixtyDegrees);
        float sin60 = Mathf.Sin(sixtyDegrees);

        //The top left vector is rotated 60 degrees clockwise from the vector 0,-1
        Vector2 topLeftVector = new Vector2(-cos60, -sin60);
        //The top right vector is rotated -60 degrees clockwise from the vector 0,-1
        Vector2 topRightVector = new Vector2(cos60, -sin60);

        //The bottom right vector is rotated -60 degrees clockwise from the vector 0,1
        Vector2 bottomRightVector = topLeftVector; //new Vector2(cos60, sin60);
        //The bottom left vector is rotated 60 degrees clockwise from the vector 0,1
        Vector2 bottomLeftVector = topRightVector; //new Vector2(-cos60, sin60);

        Vector2 rightVector = new Vector2(1, 0);
        Vector2 leftVector = rightVector;//new Vector2(-1, 0);

        Vector2 diagonalVector = new Vector2(cos60, sin60);


        //The velocity vectors at the top edge will have a positive component if the flow is going out of the cell (upwards)
        //The velocity vectors at the bottom edge will have a positive component if the flow is going out of the cell (downwards)
        //The velocity vectors at the left edge will have a positive component if the flow is going out of the cell (to the left)
        //The velocity vectors at the right edge will have a positive component if the flow is going into the cell (to the left)

        //edgeVelocities[0] = topVelocity.X * topLeftVector; // Top-Left
        //edgeVelocities[1] = topVelocity.Z * topRightVector; // Top-Right
        //edgeVelocities[2] = bottomRightVelocity.Y * rightVector; // Right
        //edgeVelocities[3] = bottomRightVelocity.X * bottomRightVector; // Bottom-Right
        //edgeVelocities[4] = bottomLeftVelocity.Z * bottomLeftVector; // Bottom-Left
        //edgeVelocities[5] = bottomLeftVelocity.Y * leftVector; // Left

        /* //Working???
        edgeVelocities[0] = topVelocity.X * -1 * diagonalVector; // Top-Left
        edgeVelocities[1] = topVelocity.Z * -1 * diagonalVector; // Top-Right
        edgeVelocities[2] = bottomRightVelocity.Y * rightVector; // Right
        edgeVelocities[3] = bottomRightVelocity.X * diagonalVector; // Bottom-Right
        edgeVelocities[4] = bottomLeftVelocity.Z * diagonalVector; // Bottom-Left
        edgeVelocities[5] = bottomLeftVelocity.Y * -1 * leftVector; // Left
        */
        edgeVelocities[0] = topVelocity.X * -1 * topLeftVector; // Top-Left
        edgeVelocities[1] = topVelocity.Z * -1 * topRightVector; // Top-Right
        edgeVelocities[2] = bottomRightVelocity.Y * -1 * rightVector; // Right
        edgeVelocities[3] = bottomRightVelocity.X * bottomRightVector; // Bottom-Right
        edgeVelocities[4] = bottomLeftVelocity.Z * bottomLeftVector; // Bottom-Left
        edgeVelocities[5] = bottomLeftVelocity.Y * leftVector; // Left

        //GD.Print("Tile (", col, ",", row, ") Edge Velocities: ", Vector2ArrayToString(edgeVelocities));
        return edgeVelocities;
    }

    public float GetTileEdgeVelocitySum(int col, int row)
    {
        Vector3 topVelocity = GetVelocitySafe(col, row);
        Vector3 bottomLeftVelocity = GetVelocitySafe(col, row + 1);
        Vector3 bottomRightVelocity = GetVelocitySafe(col + 1, row + 1);


        if (row % 2 == 1)
        {
            topVelocity = GetVelocitySafe(col + 1, row);
            bottomLeftVelocity = GetVelocitySafe(col, row + 1);
            bottomRightVelocity = GetVelocitySafe(col + 1, row + 1);
        }

        float topLeft = topVelocity.X * -1;
        float topRight = topVelocity.Z * -1;
        float right = bottomRightVelocity.Y * -1;
        float bottomRight = bottomRightVelocity.X;
        float bottomLeft = bottomLeftVelocity.Z;
        float left = bottomLeftVelocity.Y;

        return topLeft + topRight + right + bottomRight + bottomLeft + left;
        

    }

}


class HexaFluidEdgeSim : HexaFluidSimBaseClass
{
    public HexaFluidEdgeSim(int n_cols, int n_rows, float viscosity, float timeStep, float density, float cellSize, float maxVelocity)
        : base(n_cols, n_rows, viscosity, timeStep, density, cellSize, maxVelocity)
    {
    }
    public float GetPressureAtTile(int col, int row)
{        //Calculate the pressure at the center of the tile
        //This is based on the divergence of the velocity field at the edges
        Vector2[] edgeVelocities = GetTileEdgeVelocities(col, row);
        int[][] neighbourInds = GetNeighbourTileInds(col, row);
        float pressureSum = 0f;
        float pressureNum = 0f;
        for (int i = 0; i < 6; i++)
        {
            int n_col = neighbourInds[i][0];
            int n_row = neighbourInds[i][1];
            if(!IsTileValid(n_col, n_row))
            {
                continue;
            }
            float neighbourPressure = GetPressureSafe(n_col, n_row);
            pressureSum += neighbourPressure;
            pressureNum += 1f;
        }

        /*
        Vector2 velocitySum = Vector2.Zero;
        for (int i = 0; i < 6; i++)
        {
            velocitySum += edgeVelocities[i];
        }
        //Calculate divergence
        float divergence = velocitySum.X + velocitySum.Y;
        */
        float divergence = GetTileEdgeVelocitySum(col, row);

        //For now we just update the divergence grid here
        divergenceGrid[col][row] = divergence;


        float pressure = (pressureSum - density * cellSize * divergence / timeStep) / pressureNum;
        //GD.Print("Calculated Pressure at Tile (", col, ",", row, "): ", pressure, " from Divergence: ", divergence, " and Pressure Sum: ", pressureSum);
        return pressure;
    }

    public Vector3 GetVelocityAtEdge(int col, int row)
    {
        //The index is always at the top edge of the tile
        float topLeftPressure = GetPressureSafe(col - 1, row - 1);
        float topRightPressure = GetPressureSafe(col, row - 1);
        float bottomPressure = GetPressureSafe(col, row);
        if (row % 2 == 1)
        {
            bottomPressure = GetPressureSafe(col - 1, row);
        }
        Vector3 currentVelocity = GetVelocitySafe(col, row);
        float K = timeStep / (density * cellSize);
        float topLeftGradient = (bottomPressure - topLeftPressure); //Negative if the bottom pressure is lower than the top left
        float topRightGradient = (bottomPressure - topRightPressure); //Negative if the bottom pressure is lower than the top right
        float topGradient = -(topRightPressure - topLeftPressure); //Negative if the top left pressure is higher than the top right
        Vector3 newVelocity = currentVelocity - K * new Vector3(topLeftGradient, topGradient, topRightGradient);
        newVelocity *= GetValidVelocityAxis(col, row);
        return newVelocity;

    }

    public override Vector3 UpdateVelocity(int col, int row)
    {
        return GetVelocityAtEdge(col, row);
    }

    public override float UpdatePressure(int col, int row)
    {
        //GD.Print("Updating pressure at Tile (", col, ",", row, ")");
        return GetPressureAtTile(col, row);
    }

}

[Tool]
public partial class HexaFluidSim : Node2D
{
}
