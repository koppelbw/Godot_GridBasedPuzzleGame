using Game.Autoload;
using Game.Resources.Building;
using Godot;

namespace Game.Component;

public partial class BuildingComponent : Node2D
{
	[Export(PropertyHint.File, "*.tres")]

	public string buildingResourcePath;
	public BuildingResource BuildingResource {get; private set;}

	public override void _Ready()
	{
		if(buildingResourcePath is not null)
		{
			BuildingResource = GD.Load<BuildingResource>(buildingResourcePath);
		}

		AddToGroup(nameof(BuildingComponent));

		// Lesson 17 @15minutes
		Callable.From(() => GameEvents.EmitBuildingPlaced(this)).CallDeferred();
	}

	public Vector2I GetGridCellPosition()
	{		
		var gridPosition = (GlobalPosition / 64).Floor();

		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}
}
