using Godot;
using System;

public partial class PauseMenuController : Control
{
	private StageController stageController;
  public override void _Ready() {
    stageController = GetParent<StageController>();
  }

  public void Resume() {
    stageController.TogglePause();
  }

  public void Reset() {
    stageController.ResetLevel();
  }

  public void Quit() {
    stageController.Quit();
  } 
}
