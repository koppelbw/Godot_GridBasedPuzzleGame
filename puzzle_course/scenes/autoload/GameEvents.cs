using Game.Component;
using Godot;

namespace Game.Autoload;

public partial class GameEvents : Node
{
	public static GameEvents Instance {get; private set;}

	// Ending with EventHandler is required for Godot to detect the Signal in C#
	[Signal]
	public delegate void BuildingPlacedEventHandler(BuildingComponent buildingComponent);


    public override void _Notification(int what)
    {
        if(what == NotificationSceneInstantiated)
		{
			Instance = this;
		}
    }

	public static void EmitBuildingPlaced(BuildingComponent buildingComponent)
	{
		Instance.EmitSignal(SignalName.BuildingPlaced, buildingComponent);
	}
}
