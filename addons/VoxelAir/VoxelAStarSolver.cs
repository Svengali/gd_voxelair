using Godot;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class VoxelAStarSolver
{
	// Pre-calculated offsets for all 26 neighbors in the same order as the bitmask.
	private static readonly Vector3I[] NeighborOffsets;
	private static readonly float[] NeighborCosts;

	static VoxelAStarSolver()
	{
		var offsets = new List<Vector3I>();
		for (int z = -1; z <= 1; z++)
			for (int y = -1; y <= 1; y++)
				for (int x = -1; x <= 1; x++)
				{
					//Skip the center one
					if (x == 0 && y == 0 && z == 0) continue;

					offsets.Add(new Vector3I(x, y, z));
				}

		NeighborOffsets = offsets.ToArray();

		NeighborCosts = new float[26];
		for (int i = 0; i < 26; i++)
		{
			var offset = NeighborOffsets[i];

			var offsetVec = new Vector3(offset.X * 0.95f, offset.Y * 1.1f, offset.Z * 0.95f);

			NeighborCosts[i] = offsetVec.Length();
		}
	}

	public struct DebugInfo
	{
		Vector3 gStart;
		Vector3 gEnd;

		Vector3I startCoord;
		Vector3I endCoord;


	}

	public static Vector3[] FindPath(VoxelAirVolume volume, Vector3 gStart, Vector3 gEnd,
	[CallerFilePath] string dbgPath = "", [CallerLineNumber] int dbgLine = -1, [CallerMemberName] string dbgName = "")
	{
		//log.debug($"Finding path: {gStart.Log} -> {gEnd.Log}", path: dbgPath, line: dbgLine, member: dbgName);

		VoxelData data = volume.Data;
		if (data?.ConnectivityData == null || data.Length == 0)
		{
			log.warn($"No data loaded.");
			return new Vector3[0];
		}

		Vector3I startCoord = volume.GlobalToVoxelCoord(gStart);//, true);
		Vector3I endCoord = volume.GlobalToVoxelCoord(gEnd);//, true);
		
		//log.debug($"Finding path: {startCoord} -> {endCoord}", path: dbgPath, line: dbgLine, member: dbgName);

		var startIndex = data.ToIndex(startCoord);
		var endIndex   = data.ToIndex(endCoord);

		//log.debug($"Finding path: {startIndex} -> {endIndex}", path: dbgPath, line: dbgLine, member: dbgName);

		// Ensure start and end are valid, walkable nodes
		if (startIndex < 0 || startIndex >= data.Max || data[startIndex] == 0)
		{
			log.warn($"{log.var(startIndex)}");
			return new Vector3[0];
		}
	
		if (endIndex < 0 || endIndex >= data.Max || data[endIndex] == 0)
		{
			log.warn($"{log.var(endIndex)}");
			return new Vector3[0];
		}
	
		var openSet = new PriorityQueue<VoxelIndex, float>();
		int nodeCount = (int)data.Length;

		var cameFrom = new VoxelArray<VoxelIndex>(nodeCount);
		var gScore = new VoxelArray<float>(nodeCount);

		for (VoxelIndex i = 0; i < data.Max; i++)
		{
			gScore[i] = float.MaxValue;
			cameFrom[i] = VoxelIndex.Invalid; // Use -1 to indicate no parent
		}

		gScore[startIndex] = 0;
		float fScore = Heuristic(startCoord, endCoord);
		openSet.Enqueue(startIndex, fScore);

		while (openSet.Count > 0)
		{
			var currentIndex = openSet.Dequeue();
			if (currentIndex == endIndex)
			{
				//log.debug($"Found path");
				return ReconstructPath(volume, gStart, gEnd, cameFrom, currentIndex);
			}

			var mask = data[currentIndex];
			Vector3I currentCoord = data.ToCoord(currentIndex);

			for (int bit = 0; bit < (int)VoxelConnection.MaxBits; bit++)
			{
				// Check if the connection exists by checking the bit
				if (((int)mask & (1 << bit)) != 0)
				{
					Vector3I neighborCoord = currentCoord + NeighborOffsets[(int)bit];
					var neighborIndex = data.ToIndex(neighborCoord);

					float tentativeGScore = gScore[currentIndex] + NeighborCosts[(int)bit] * data.VoxelSize;

					if (tentativeGScore < gScore[neighborIndex])
					{
						cameFrom[neighborIndex] = currentIndex;
						gScore[neighborIndex] = tentativeGScore;
						fScore = tentativeGScore + Heuristic(neighborCoord, endCoord);
						openSet.Enqueue(neighborIndex, fScore);
					}
				}
			}
		}

		return new Vector3[0]; // No path found
	}

	private static float Heuristic(Vector3I a, Vector3I b)
	{
		return ((Vector3)(a - b)).Length();
	}

	private static Vector3[] ReconstructPath(VoxelAirVolume volume, Vector3 start, Vector3 end, VoxelArray<VoxelIndex> cameFrom, VoxelIndex currentIndex)
	{
		List<Vector3> path = new ();
		while (currentIndex != VoxelIndex.Invalid)
		{
			path.Add(volume.VoxelCoordToGlobal(volume.Data.ToCoord(currentIndex)));
			currentIndex = cameFrom[currentIndex];
		}
		path.Reverse();
		path.Add(end);
		return path.ToArray();
	}
}