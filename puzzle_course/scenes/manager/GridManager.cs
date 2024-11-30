using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Game.Autoload;
using Game.Component;
using Godot;

namespace Game.Manager;

public partial class GridManager : Node
{
	private const string IS_BUILDABLE = "is_buildable";
	private const string IS_WOOD = "is_wood";

	[Signal]
	public delegate void ResourceTilesUpdatedEventHandler(int collectedTiles);

	private HashSet<Vector2I> validBuildableTiles = new();
	private HashSet<Vector2I> collectedResourceTiles = new();

	[Export]
	private TileMapLayer highlightTileMapLayer;

	[Export]
	private TileMapLayer baseTerrianTileMapLayer;

	private List<TileMapLayer> allTileMapLayers = new ();

    public override void _Ready()
    {
        // var gameEvents = GetNode<GameEvents>("/root/GameEvents");
		GameEvents.Instance.BuildingPlaced += OnBuildingPlaced;
		allTileMapLayers = GetAllTileMapLayers(baseTerrianTileMapLayer);
    }

    public bool TileHasCustomData(Vector2I tilePosition, string dataName)
	{
		foreach(var layer in allTileMapLayers)
		{
			var customData = layer.GetCellTileData(tilePosition);
			if(customData is null) continue;

			return (bool)customData.GetCustomData(dataName);
		}

		return false;
	}

	public bool IsTilePositionBuildable(Vector2I tilePosition) => validBuildableTiles.Contains(tilePosition);

	public void HighlightBuildableTiles()
	{
		foreach(var tilePosition in validBuildableTiles)
		{
			highlightTileMapLayer.SetCell(tilePosition, 0, Vector2I.Zero);
		}
	}

	public void HighlightExpandedBuildableTiles(Vector2I rootCell, int radius)
	{
		HighlightBuildableTiles();

		var validTiles = GetValidTilesInRadius(rootCell, radius).ToHashSet();
		var expandedTiles = validTiles.Except(validBuildableTiles).Except(GetOccupiedTiles());
		var atlasCoords = new Vector2I(1, 0);	//Green tile

		foreach(var tilePosition in expandedTiles)
		{
			highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
		}
	}	

	public void HighlightResourceTiles(Vector2I rootCell, int radius)
	{
		var resourceTiles = GetResourceTilesInRadius(rootCell, radius);
		var atlasCoords = new Vector2I(1, 0);	//Green tile

		foreach(var tilePosition in resourceTiles)
		{
			highlightTileMapLayer.SetCell(tilePosition, 0, atlasCoords);
		}
	}

	public void ClearHighlightedTiles()
	{
		highlightTileMapLayer.Clear();
	}

	public Vector2I GetMouseGridCellPosition()
	{
		var mousePosition = highlightTileMapLayer.GetGlobalMousePosition();
		var gridPosition = (mousePosition / 64).Floor();

		return new Vector2I((int)gridPosition.X, (int)gridPosition.Y);
	}



	// **** Private Methods ****
	private List<TileMapLayer> GetAllTileMapLayers(TileMapLayer rootTileMapLayer)
	{
		var result = new List<TileMapLayer>();
		var children = rootTileMapLayer.GetChildren();
		children.Reverse();
		
		foreach(var child in children)
		{
			if(child is TileMapLayer childLayer)
			{
				result.AddRange(GetAllTileMapLayers(childLayer));
			}
		}

		result.Add(rootTileMapLayer);
		return result;
	}


	private void UpdateValidBuildableTiles(BuildingComponent buildingComponent)
	{
		var rootCell = buildingComponent.GetGridCellPosition();
		var radius = buildingComponent.BuildingResource.BuildableRadius;

		var validTiles = GetValidTilesInRadius(rootCell, buildingComponent.BuildingResource.BuildableRadius);
		
		// Merge buildable tiles and new valid tiles
		validBuildableTiles.UnionWith(validTiles);		
		validBuildableTiles.ExceptWith(GetOccupiedTiles());
	}

	private void UpdateCollectedResourceTiles(BuildingComponent buildingComponent)
	{
		var rootCell = buildingComponent.GetGridCellPosition();
		var resourceTiles = GetResourceTilesInRadius(rootCell, buildingComponent.BuildingResource.ResourceRadius);

		var oldResourceTileCount = collectedResourceTiles.Count();
		collectedResourceTiles.UnionWith(resourceTiles);

		if(oldResourceTileCount != collectedResourceTiles.Count)
		{
			EmitSignal(SignalName.ResourceTilesUpdated, resourceTiles.Count());
		}
	}

	private IEnumerable<Vector2I> GetOccupiedTiles()
	{
		var buildingComponents = GetTree().GetNodesInGroup(nameof(BuildingComponent)).Cast<BuildingComponent>();		
		return buildingComponents.Select(b => b.GetGridCellPosition());
	}

	private List<Vector2I> GetTilesInRadius(Vector2I rootCell, int radius, Func<Vector2I, bool> filterFn)
	{
		var result = new List<Vector2I>();

		for(var x = rootCell.X - radius; x <= rootCell.X + radius; x++)
		{
			for(var y = rootCell.Y - radius; y <= rootCell.Y + radius; y++)
			{
				var tilePosition = new Vector2I(x, y);
				if(filterFn(tilePosition))
				{
					result.Add(tilePosition);
				}
			}
		}

		return result;
	}

	private List<Vector2I> GetValidTilesInRadius(Vector2I rootCell, int radius) => 
		GetTilesInRadius(rootCell, radius, (tilePosition) => {
			return TileHasCustomData(tilePosition, IS_BUILDABLE);
		});
	

	private List<Vector2I> GetResourceTilesInRadius(Vector2I rootCell, int radius) => 
		GetTilesInRadius(rootCell, radius, (tilePosition) => {
			return TileHasCustomData(tilePosition, IS_WOOD);
		});

	private void OnBuildingPlaced(BuildingComponent buildingComponent)
	{
		UpdateValidBuildableTiles(buildingComponent);
		UpdateCollectedResourceTiles(buildingComponent);
	}
}
