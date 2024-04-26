using Godot;
using System;

public partial class StageController : Node
{
	private SceneTree sceneTree;
	private Node3D loadedLevel;
	private Control pauseMenu;
	public override void _Ready()	{
		sceneTree = GetTree();
		loadedLevel = GetNode<Node3D>("LoadedLevel");
		pauseMenu = GetNode<Control>("PauseMenuController");
	}
	public override void _Input(InputEvent ie) {
		if (ie.IsActionType()) {
			if (ie.GetActionStrength("game_pause") > 0f) {
				TogglePause();
			}
		}
	}
	public void TogglePause() {
		sceneTree.Paused = !sceneTree.Paused;
		pauseMenu.Visible = sceneTree.Paused;
	}

	public void ResetLevel() {
		// loadedLevel should always hold exactly the currently loaded level object and nothing else.
		Node3D levelNode = loadedLevel.GetChild<Node3D>(0);
		levelNode.Free();
		Node newLevel = ResourceLoader.Load<PackedScene>("res://maps/TESTMAP.tscn").Instantiate();
		newLevel.Name = "TESTMAP";
		loadedLevel.AddChild(newLevel);
	}

	public void Quit() {
		sceneTree.Quit(0);
	}
}
