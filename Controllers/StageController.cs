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
		// TODO
	}

	public void Quit() {
		sceneTree.Quit(0);
	}
}
