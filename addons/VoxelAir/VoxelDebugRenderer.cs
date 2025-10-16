using Godot;
using System.Collections.Generic;

[Tool, GlobalClass]
public partial class VoxelDebugRenderer : Node3D
{
	[Export] public VoxelAirVolume TargetVolume { get; set; }

	[ExportGroup("Display")]
	[Export] public bool DrawSolidVoxels { get; set; } = true;
	[Export] public Color SolidVoxelColor { get; set; } = new(1.0f, 0.2f, 0.2f, 0.2f);
	[Export] public bool DrawClearVoxels { get; set; } = false;
	[Export] public Color ClearVoxelColor { get; set; } = new(0.2f, 0.2f, 1.0f, 0.2f);

	[Export] public bool RedrawDebug = false;

	private MultiMeshInstance3D _meshInstance;

	public override void _EnterTree()
	{
		if (TargetVolume == null)
		{
			TargetVolume = GetParent<VoxelAirVolume>();
		}

		// Automatically connect to the target volume's signal when entering the tree
		if (TargetVolume != null)
		{
			TargetVolume.VoxelizationComplete += OnVoxelizationComplete;
		}
	}

	public override void _ExitTree()
	{
		if (TargetVolume != null)
		{
			TargetVolume.VoxelizationComplete -= OnVoxelizationComplete;
		}
	}

	public override void _Ready()
	{
		_meshInstance = GetNodeOrNull<MultiMeshInstance3D>("VoxelMultiMesh");
		if (_meshInstance == null)
		{
			_meshInstance = new MultiMeshInstance3D { Name = "VoxelMultiMesh" };
			AddChild(_meshInstance);
		}
	}

	private void OnVoxelizationComplete()
	{
		GD.Print("Debug Renderer received VoxelizationComplete signal. Redrawing...");
		Redraw();
	}

	public override void _Process(double delta)
	{
		base._Process(delta);

		if( RedrawDebug )
		{
			Clear();
			Redraw();
			RedrawDebug = false;
		}
	}

	public void Clear()
	{
		_meshInstance.Multimesh = null;
	}

	public void Redraw()
	{
		if (TargetVolume?.Data?.ConnectivityData == null)
		{
			Clear();
			return;
		}

		var data = TargetVolume.Data;
		var multiMesh = new MultiMesh
		{
			Mesh = new BoxMesh { Size = Vector3.One * data.VoxelSize },
			TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
			UseColors = true
		};

		List<Transform3D> transforms = new ();
		List<Color> colors = new ();
 
		for (VoxelIndex i = 0; i < data.Max; i++)
		{
			bool isSolid = data[i] == 0;

			var voxelCoord = data.ToCoord(i);
			Vector3 gPos = TargetVolume.VoxelCoordToGlobal(voxelCoord);
			Vector3 lPos = ToLocal(gPos);

			bool writeLoc = false;

			var gridSizeMax = TargetVolume.Data.GridSize - Vector3I.One;

			writeLoc  = voxelCoord.X == 0 | voxelCoord.Y == 0 | voxelCoord.Z == 0;
			writeLoc |= voxelCoord.X == gridSizeMax.X | voxelCoord.Y == gridSizeMax.Y | voxelCoord.Z == gridSizeMax.Z;

			if( writeLoc )
			{
				log.debug($"{i}: {{ {voxelCoord} }} [{gPos.Log}] ({lPos.Log})");
				GD.Print ($"{i}: {{ {voxelCoord} }} [{gPos.Log}] ({lPos.Log})");
			}

			if ((isSolid & DrawSolidVoxels) | (!isSolid & DrawClearVoxels))
			{
				transforms.Add( new Transform3D(Basis.Identity, lPos ) );
				colors.Add(isSolid ? SolidVoxelColor : ClearVoxelColor);
			}
		}

		multiMesh.InstanceCount = transforms.Count;
		for (int i = 0; i < transforms.Count; i++)
		{
			multiMesh.SetInstanceTransform(i, transforms[i]);
			multiMesh.SetInstanceColor(i, colors[i]);
		}

		var material = new StandardMaterial3D
		{
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			AlbedoColor = Colors.White,
			ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
		};
		_meshInstance.MaterialOverride = material;
		_meshInstance.Multimesh = multiMesh;
	}
}