using Godot;
using System;







[Tool, GlobalClass]
public partial class VoxelAirVolume : Node3D
{
    [Signal]
    public delegate void VoxelizationCompleteEventHandler();



    [Export] public VoxelData Data { get; set; }
    [Export] public Vector3 Size { get; set; } = new(10, 10, 10);

    [Export(PropertyHint.Layers3DPhysics)]
    public uint CollisionMask { get; set; } = 1;

    [ExportGroup("Baking Performance")]
    [Export(PropertyHint.Range, "1,10000,100")] 
    public int VoxelsPerTick { get; set; } = 2000;

    [Export]
    public bool IsVoxelizing
    {
        get => _isVoxelizing;
        set
        {

        }
    }

    [Export]
    public VoxelIndex Index
    {
        get => _currentIndex;
        set
        {

        }
    }


    [Export]
    private Aabb _bounds;

    // State for incremental voxelization
    private bool _isVoxelizing = false;
    private VoxelArray<bool> _solidMap;
    private VoxelIndex _currentIndex = 0;


	public override void _Ready()
	{
        base._Ready();
	}

    public override void _Process(double delta)
	{
		if( !Engine.IsEditorHint() )
		{
            //if (Data.Max > 0)
            {
                var min = new Vector3I(0, 0, 0);

                var max = Data.GridSize - Vector3I.One;

                var minPos = VoxelCoordToGlobal(min) - Vector3.One * Data.VoxelSize;

                var maxPos = VoxelCoordToGlobal(max) + Vector3.One * Data.VoxelSize;

                DebugDraw3D.DrawAabbAb(minPos, maxPos, Colors.Orange);

			}
		}
	}

    public override void _PhysicsProcess(double delta)
    {
        // This runs in the editor because of the [Tool] attribute
        if (Engine.IsEditorHint() && _isVoxelizing)
        {
            ProcessVoxelizationTick();
        }
    }

    // Add these two methods inside your existing VoxelAirVolume.cs class

    /// <summary>
    /// Converts a world-space position to a voxel grid coordinate.
    /// </summary>
    public Vector3I GlobalToVoxelCoord(Vector3 gPos)
    {
        var lPos = ToLocal(gPos);
        Vector3 voxelLocalPos = lPos - ( _bounds.Position );
        return new Vector3I(
        (int)Math.Floor(voxelLocalPos.X / Data.VoxelSize),
        (int)Math.Floor(voxelLocalPos.Y / Data.VoxelSize),
        (int)Math.Floor(voxelLocalPos.Z / Data.VoxelSize)
        );
    }

    /// <summary>
    /// Converts a voxel grid coordinate to the center of that voxel in world-space.
    /// </summary>
    public Vector3 VoxelCoordToGlobal(Vector3I localCoord)
    {
        var voxelLocalPos = new Vector3(localCoord.X, localCoord.Y, localCoord.Z) * Data.VoxelSize +
        (Vector3.One * Data.VoxelSize * 0.5f);

        var voxelBoundsLocalPos = voxelLocalPos + _bounds.Position;

        var gPos = ToGlobal(voxelBoundsLocalPos);

        return gPos;
    }

    VoxelIndex Max => (VoxelIndex)_solidMap.Length;

    /// <summary>
    /// Initiates the voxelization process.
    /// </summary>
    public void Voxelize()
    {
        if (Data == null)
        {
            GD.PrintErr("VoxelData resource is not assigned.");
            return;
        }

        _bounds = new Aabb(GlobalPosition - Size / 2, Size);
        Data.GridSize = new Vector3I(
        (int)Math.Ceiling(Size.X / Data.VoxelSize),
        (int)Math.Ceiling(Size.Y / Data.VoxelSize),
        (int)Math.Ceiling(Size.Z / Data.VoxelSize)
        );
        Data.VoxelSize = Data.VoxelSize;

        int totalVoxels = Data.GridSize.X * Data.GridSize.Y * Data.GridSize.Z;
        _solidMap = new VoxelArray<bool>(totalVoxels);
        Data.ConnectivityData = new int[totalVoxels];

        _currentIndex = 0;
        _isVoxelizing = true;

        GD.Print($"Starting voxelization of {Data.GridSize} grid ({totalVoxels} voxels)...");
    }

    /// <summary>
    /// Processes a batch of voxels each frame.
    /// </summary>
    private void ProcessVoxelizationTick()
    {
        var spaceState = GetWorld3D().DirectSpaceState;
        if (spaceState == null)
        {
            GD.PushWarning("Could not access DirectSpaceState. Voxelization paused.");
            return;
        }

        var shape = new BoxShape3D { Size = Vector3.One * Data.VoxelSize };
        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = shape,
            CollisionMask = CollisionMask
        };

        for (int i = 0; i < VoxelsPerTick; i++)
        {
            if (((int)_currentIndex & 0xff) == 0x00)
            {
                GD.Print($"Current index {_currentIndex}");
            }

            if (_currentIndex >= Max)
            {
                FinishVoxelization();
                return;
            }

            Vector3I coord = Data.ToCoord(_currentIndex);

            Vector3 gPos = VoxelCoordToGlobal( coord );
            //Vector3 localPos = _bounds.Position + new Vector3(coord.X, coord.Y, coord.Z) * Data.VoxelSize + (Vector3.One * Data.VoxelSize * 0.5f);
            //var gPos = ToGlobal(localPos);

            query.Transform = new Transform3D(Basis.Identity, gPos);

            if (spaceState.IntersectShape(query).Count > 0)
            {
                _solidMap[_currentIndex] = true;
            }

            _currentIndex++;
        }
    }

    private void FinishVoxelization()
    {
        _isVoxelizing = false;
        GD.Print("Solid geometry check complete. Building connectivity...");

        BuildConnectivity(_solidMap); // This method is now updated

        GD.PrintRich("[color=green]Voxelization complete! Saving resource...[/color]");

        if( !string.IsNullOrWhiteSpace(Data.ResourcePath) )
        {
            ResourceSaver.Save(Data, Data.ResourcePath);
        }
        else
        {
            GD.PushWarning($"VoxelData: {Data.ResourcePath}");
            GD.PushWarning($"VoxelData: Not saved to a file. Please save the resource to persist changes.");
        }

        _solidMap = new VoxelArray<bool>( 0 );
    }

	bool Valid<T>(VoxelArray<T> map, VoxelIndex index) => (index >= 0) & (index < (VoxelIndex)map.Length);

	/// <summary>
	/// Builds the 26-bit connectivity mask for each clear voxel.
	/// Updated to handle diagonal movement rules.
	/// </summary>
	private void BuildConnectivity(VoxelArray<bool> solidMap)
    {
        for (VoxelIndex i = 0; i < solidMap.Max; i++)
        {
            if (solidMap[i]) continue; // Skip solid voxels

            Vector3I coord = Data.ToCoord(i);
            int mask = 0;
            int bit = 0;

            for (int z = -1; z <= 1; z++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;

                        Vector3I neighborCoord = coord + new Vector3I(x, y, z);

                        // Default to not connected
                        bool canConnect = false;

                        var neighborIndex = Data.ToIndex(neighborCoord);

                        /*
                        if (neighborCoord.X >= 0 && neighborCoord.X < Data.GridSize.X &&
                        neighborCoord.Y >= 0 && neighborCoord.Y < Data.GridSize.Y &&
                        neighborCoord.Z >= 0 && neighborCoord.Z < Data.GridSize.Z)
                        */
                        if (neighborIndex >= 0 && neighborIndex < solidMap.Max)
                        {
                            // Neighbor must not be solid
                            if (!solidMap[neighborIndex])
                            {
                                int manhattanDist = Math.Abs(x) + Math.Abs(y) + Math.Abs(z);

                                if (manhattanDist == 1) // Cardinal move (N, S, E, W, Up, Down)
                                {
                                    canConnect = true;
                                }
                                else if (manhattanDist == 2) // Face-diagonal move
                                {
                                    // Must check the two cardinal neighbors that form the face
                                    VoxelIndex c1_idx = Data.ToIndex(coord + new Vector3I(x, 0, 0));
                                    VoxelIndex c2_idx = Data.ToIndex(coord + new Vector3I(0, y, 0));
                                    VoxelIndex c3_idx = Data.ToIndex(coord + new Vector3I(0, 0, z));



                                    // Check the two relevant cardinal directions
                                    if (Valid(solidMap, c1_idx) && Valid(solidMap, c2_idx) && x != 0 && y != 0) canConnect = !solidMap[c1_idx] && !solidMap[c2_idx]; // XY plane
                                    else if (Valid(solidMap, c1_idx) && Valid(solidMap, c3_idx) && x != 0 && z != 0) canConnect = !solidMap[c1_idx] && !solidMap[c3_idx]; // XZ plane
                                    else if (Valid(solidMap, c2_idx) && Valid(solidMap, c3_idx)) canConnect = !solidMap[c2_idx] && !solidMap[c3_idx]; // YZ plane
                                }
                                else if (manhattanDist == 3) // Corner-diagonal move
                                {
                                    // Must check the three cardinal neighbors
                                    VoxelIndex c1_idx = Data.ToIndex(coord + new Vector3I(x, 0, 0));
                                    VoxelIndex c2_idx = Data.ToIndex(coord + new Vector3I(0, y, 0));
                                    VoxelIndex c3_idx = Data.ToIndex(coord + new Vector3I(0, 0, z));
                                    if (Valid(solidMap, c1_idx) && Valid(solidMap, c2_idx) && Valid(solidMap, c3_idx))
                                    {
                                        canConnect = !solidMap[c1_idx] && !solidMap[c2_idx] && !solidMap[c3_idx];
                                    }
                                    ;
                                }
                            }
                        }

                        if (canConnect)
                        {
                            mask |= (1 << bit);
                        }
                        bit++;
                    }
                }
            }
            Data[i] = (VoxelConnection)mask;
        }
    }



}