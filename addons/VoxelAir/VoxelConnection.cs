using System;
using Godot;

/// <summary>
/// A flags enum representing the 26 possible connections from a voxel to its neighbors.
/// Each flag corresponds to a specific bit in the connectivity mask.
/// </summary>
[Flags]
public enum VoxelConnection : int // Use uint for the underlying type
{
    None = 0,
    
    // Z = -1 (Back Layer)
    Back_BottomLeft  = 1 << 0,  // (-1, -1, -1)
    Back_Bottom      = 1 << 1,  // ( 0, -1, -1)
    Back_BottomRight = 1 << 2,  // ( 1, -1, -1)
    Back_Left        = 1 << 3,  // (-1,  0, -1)
    Back_Center      = 1 << 4,  // ( 0,  0, -1)
    Back_Right       = 1 << 5,  // ( 1,  0, -1)
    Back_TopLeft     = 1 << 6,  // (-1,  1, -1)
    Back_Top         = 1 << 7,  // ( 0,  1, -1)
    Back_TopRight    = 1 << 8,  // ( 1,  1, -1)
    
    // Z = 0 (Middle Layer)
    Middle_BottomLeft  = 1 << 9,  // (-1, -1,  0)
    Middle_Bottom      = 1 << 10, // ( 0, -1,  0)
    Middle_BottomRight = 1 << 11, // ( 1, -1,  0)
    Middle_Left        = 1 << 12, // (-1,  0,  0)
    // No Middle_Center (self)
    Middle_Right       = 1 << 13, // ( 1,  0,  0)
    Middle_TopLeft     = 1 << 14, // (-1,  1,  0)
    Middle_Top         = 1 << 15, // ( 0,  1,  0)
    Middle_TopRight    = 1 << 16, // ( 1,  1,  0)

    // Z = 1 (Front Layer)
    Front_BottomLeft  = 1 << 17, // (-1, -1,  1)
    Front_Bottom      = 1 << 18, // ( 0, -1,  1)
    Front_BottomRight = 1 << 19, // ( 1, -1,  1)
    Front_Left        = 1 << 20, // (-1,  0,  1)
    Front_Center      = 1 << 21, // ( 0,  0,  1)
    Front_Right       = 1 << 22, // ( 1,  0,  1)
    Front_TopLeft     = 1 << 23, // (-1,  1,  1)
    Front_Top         = 1 << 24, // ( 0,  1,  1)
    Front_TopRight    = 1 << 25,  // ( 1,  1,  1)

    MaxBits           = 26,
}
