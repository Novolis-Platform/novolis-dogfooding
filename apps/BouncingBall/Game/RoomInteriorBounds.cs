namespace BouncingBall.Game;

/// <summary>Axis-aligned interior for sphere centers (inside wall columns, floor, ceiling).</summary>
internal readonly struct RoomInteriorBounds
{
    public float MinX { get; init; }
    public float MaxX { get; init; }
    public float MinY { get; init; }
    public float MaxY { get; init; }
    public float MinZ { get; init; }
    public float MaxZ { get; init; }

    public static RoomInteriorBounds ForRoom(uint gridSize, float cellSize, float wallHeight, float ballRadius)
    {
        var wallInset = cellSize + ballRadius + 0.04f;
        var maxHorizontal = (gridSize - 1) * cellSize - ballRadius - 0.04f;
        return new RoomInteriorBounds
        {
            MinX = wallInset,
            MaxX = maxHorizontal,
            MinY = ballRadius,
            MaxY = wallHeight - ballRadius,
            MinZ = wallInset,
            MaxZ = maxHorizontal,
        };
    }
}
