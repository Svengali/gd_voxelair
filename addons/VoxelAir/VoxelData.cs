using Godot;




public enum VoxelIndex : int
{
    Invalid = int.MaxValue
}


public struct VoxelArray<T>
{
    private T[] _arr = new T[0];

    public VoxelArray(int maxSize)
    {
        _arr = new T[maxSize];
    }

    public int Length => _arr.Length;

    public VoxelIndex Max => (VoxelIndex)_arr.Length;

    public T this[VoxelIndex index]
    {
        get => _arr[(int)index];
        set => _arr[(int)index] = value;
    }
}



[Tool, GlobalClass]
public partial class VoxelData : Resource
{
    [Export] public Vector3I GridSize { get; set; }
    [Export] public float VoxelSize { get; set; }


    public int Length => ConnectivityData.Length;
    public VoxelIndex Max => (VoxelIndex)ConnectivityData.Length;

    // A flat array representing the 3D grid. Each uint is a 26-bit mask
    // representing connections to neighbors. A value of 0 means the voxel is solid.
    [Export] public int[] ConnectivityData { get; set; }

    public VoxelConnection this[VoxelIndex index]
    {
        get => (VoxelConnection)ConnectivityData[(int)index];
        set => ConnectivityData[(int)index] = (int)value;
    }


    /// <summary>
    /// Converts a 3D grid coordinate to a 1D array index.
    /// </summary>
    public VoxelIndex ToIndex(Vector3I coord)
    {
        var index_i = coord.Z * GridSize.X * GridSize.Y + coord.Y * GridSize.X + coord.X;
        return (VoxelIndex)index_i;
    }

    /// <summary>
    /// Converts a 1D array index back to a 3D grid coordinate.
    /// </summary>
    public Vector3I ToCoord(VoxelIndex index)
    {
        int index_i = (int)index;
        int z = index_i / (GridSize.X * GridSize.Y);
        int y = (index_i - (z * GridSize.X * GridSize.Y)) / GridSize.X;
        int x = index_i % GridSize.X;
        return new Vector3I(x, y, z);
    }
}