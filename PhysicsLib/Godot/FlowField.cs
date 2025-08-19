using Godot;

namespace PhysicsLib.Util;

public class FlowField
{
    private static readonly Vector2[] diagonals = [
        Vector2.FromAngle(Rads.Pi45 * 1),
        Vector2.FromAngle(Rads.Pi45 * 3),
    ];

    public int FLOOR = 0;
    public int MaxDistance { get; set; } = 100;
    private Random Rnd { get; set; }

    public Vector2I FloorTileSize = new(16, 16);
    public TableArray<int> FloorGrid { get; }
    public TableArray<int> EnemyGrid { get; }

    public Vector2I FlowTileSize = new(64, 64);
    public TableArray<float> CostGrid { get; private set; }
    public TableArray<Vector2I> FlowGrid { get; private set; }

    public FlowField(TableArray<int> terrain, int floor, int seed = 0)
    {
        this.FLOOR = floor;
        this.FloorGrid = terrain;
        this.EnemyGrid = new(terrain.Width, terrain.Height, 0);
        this.CostGrid = new(terrain.Width, terrain.Height, int.MaxValue);
        this.FlowGrid = new(terrain.Width, terrain.Height, new(0, 0));
        Rnd = new(seed);
    }

    public Vector2I GetFloorGridPos(Vector2 pos) => (Vector2I)pos / FloorTileSize;
    public Vector2I GetFlowGridPos(Vector2 pos) => (Vector2I)pos / FlowTileSize;

    public Vector2 GetFloorToWorldPos(Vector2I pos) => pos * FloorTileSize;
    public Vector2 GetFlowToWorldPos(Vector2I pos) => pos * FlowTileSize;

    public bool IsWorldPosOnFloor(Vector2 pos)
    {
        var gridPos = GetFloorGridPos(pos);
        return FloorGrid.Is(gridPos, FLOOR);
    }

    public int GetTerrain(Vector2I gridPos)
    {
        return FloorGrid[gridPos];
    }
    public Vector2I GetFlowDir(Vector2I gridPos)
    {
        if (FlowGrid.Has(gridPos))
            return FlowGrid[gridPos];
        return FlowGrid.defaultValue;
    }

    public float GetCost(Vector2I gridPos)
    {
        if (CostGrid.Has(gridPos))
            return CostGrid[gridPos];
        return int.MaxValue;
    }

    public float GetCost(Vector2 pos)
    {
        var gridPos = GetFlowGridPos(pos);
        return GetCost(gridPos);
    }

    public void Calculate(Vector2I startPos)
    {
        //EnemyGrid.Clear();
        CalculateCosts(startPos);
        CalculateFlows(startPos);
    }

    public void CalculateCosts(Vector2I startPos)
    {
        TableArray<float> tempCosts = new(CostGrid.Width, CostGrid.Height, CostGrid.defaultValue);

        // Queue for BFS
        Queue<Vector2I> queue = new();
        Queue<Vector2I> wallQueue = new();

        queue.Enqueue(startPos);
        tempCosts[startPos] = 0;

        // Perform BFS on Floors
        while (queue.Count > 0)
        {
            Vector2I currentPos = queue.Dequeue();
            float currentCost = tempCosts[currentPos];
            float nextCost = currentCost + 1;

            // Explore all 4 directions (up, down, left, right)
            foreach (var o in Enum.GetValues<Orientation4>())
            {
                var dir = o.GetVector();
                var neighboor = currentPos + dir;

                if (neighboor == startPos) continue;

                // Check bounds and whether the cell is walkable and unvisited
                if (FloorGrid.Has(neighboor))
                {
                    if (tempCosts[neighboor] == tempCosts.defaultValue)
                    {
                        // dont explore neighboors over the max dist
                        if (neighboor.ManhattahnDistanceTo(startPos) < MaxDistance)
                        {
                            if (FloorGrid[neighboor] == FLOOR)
                            {
                                tempCosts[neighboor] = nextCost;
                                queue.Enqueue(neighboor);
                            }
                            else
                            {
                                // Add to wall queue
                                tempCosts[neighboor] = nextCost + 100;
                                wallQueue.Enqueue(neighboor);
                            }
                        }
                    }
                }
            }
        }

        // Perform BFS on Walls
        while (wallQueue.Count > 0)
        {
            Vector2I currentPos = wallQueue.Dequeue();
            float currentCost = tempCosts[currentPos];

            // Explore all 4 directions (up, down, left, right)
            foreach (var o in Enum.GetValues<Orientation4>())
            {
                var dir = o.GetVector();
                var neighboor = currentPos + dir;
                float nextCost = currentCost + 1;

                if (neighboor == startPos) continue;

                // Check bounds and whether the cell is walkable and unvisited
                if (FloorGrid.Has(neighboor))
                {
                    if (tempCosts[neighboor] == tempCosts.defaultValue)
                    {
                        if (FloorGrid[neighboor] != FLOOR)
                        {
                            // dont explore neighboors over the max dist
                            if (neighboor.ManhattahnDistanceTo(startPos) < MaxDistance)
                            {
                                tempCosts[neighboor] = nextCost;
                                wallQueue.Enqueue(neighboor);
                            }
                        }
                    }
                }
            }
        }

        CostGrid = tempCosts;
    }

    private void CalculateFlows(Vector2I startPos)
    {
        TableArray<Vector2I> temp = new(FlowGrid.Width, FlowGrid.Height)
        {
            defaultValue = FlowGrid.defaultValue
        };

        // Pass 2: Get best paths directions
        Random rnd = Rnd; //Formulas.Rnd; // new(); // ? 
        Vector2I[] neighboorPositions = new Vector2I[8];
        float[] neighboorCosts = new float[8];
        foreach (var p in CostGrid)
        {
            var cell = new Vector2I(p.x, p.y); //pair.Key;
            var currentCost = p.v;

            float minCost = CostGrid.defaultValue;
            Vector2I cheapestNeighboor = neighboorPositions[0];

            // Get neighboors positions and costs
            int i = -1;
            foreach (var o in Enum.GetValues<Orientation8>())
            {
                i++;
                var neighboorPos = cell + o.GetVector();
                neighboorPositions[i] = neighboorPos;
                if (!FloorGrid.Has(neighboorPos))
                {
                    neighboorCosts[i] = CostGrid.defaultValue;
                    continue;
                }

                var cost = CostGrid[neighboorPos];
                var delta = neighboorPos - cell;

                foreach (var diag in diagonals)
                {
                    float dot = diag.X * delta.X + diag.Y * delta.Y;
                    float magA = diag.LengthSquared();
                    float magB = delta.LengthSquared();
                    float cosine = dot / MathF.Sqrt(magA * magB);
                    dot = MathF.Abs(cosine);
                    bool areParallel = dot >= 0.9f;

                    if (areParallel)
                        cost += 1f;
                }

                neighboorCosts[i] = cost; // + EnemyGrid[neighboorPos];

                // Find lowest cost
                if (cost < minCost)
                {
                    minCost = cost;
                    cheapestNeighboor = neighboorPos;
                }
            }

            // Get a random lowest cost neighboor
            for (i = 0; i < neighboorCosts.Length; i++)
            {
                if (neighboorCosts[i] == minCost)
                {
                    if (rnd.NextBool())
                    {
                        cheapestNeighboor = neighboorPositions[i];
                    }
                }
            }

            // Set flow direction
            var dir = cheapestNeighboor - cell;
            temp[cell] = dir;
        }
        FlowGrid = temp;
    }

    // <summary>
    // https://www.youtube.com/watch?v=tVGixG_N_Pg
    // Thought: 
    //     Most likely, anything that is far from the player does not need to be updated. (ex: 30-50cells+)
    //     The paths to reach will probably be the same.
    //     Definitely can thread this and replace the whole flowfield when done.
    //     
    //     Add 8-orientation
    //     Remove(?) weight to cells slightly diagonal to the startPos (ex: close to 45°)
    //     Add weight to cells where enemies already are. (boids-like behaviour) (can have a tablegrid<int> with enemy count in each cell, updated by moveSystem)
    //     Optimize cache
    //     Update only x chunks of flowField per y frame
    //     Update only cells touching a changed cell? -> doesnt work
    // </summary>

}
