using Godot;

[Tool]
public partial class VoxelAir_EditorPlugin : EditorPlugin
{
    private HBoxContainer _toolbar;
    private VoxelAirVolume _voxelAirInstance;

    public override void _EnterTree()
    {
        _toolbar = new HBoxContainer();
        var voxelizeButton = new Button { Text = "Voxelize World" };
        voxelizeButton.Pressed += OnVoxelizePressed;
        _toolbar.AddChild(voxelizeButton);

        AddControlToContainer(CustomControlContainer.SpatialEditorMenu, _toolbar);
        _toolbar.Visible = false;
    }

    public override void _ExitTree()
    {
        if (_toolbar != null)
        {
            RemoveControlFromContainer(CustomControlContainer.SpatialEditorMenu, _toolbar);
            _toolbar.QueueFree();
        }
    }

    public override bool _Handles(GodotObject @object) => @object is VoxelAirVolume;

    public override void _Edit(GodotObject @object)
    {
        _voxelAirInstance = @object as VoxelAirVolume;
        _toolbar.Visible = _voxelAirInstance != null;
    }

    private void OnVoxelizePressed()
    {
        _voxelAirInstance?.Voxelize();
    }
}