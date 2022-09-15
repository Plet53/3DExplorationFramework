using Godot;
using System;

public class CameraControl : Spatial
{
  Spatial player;
  Vector2 inp;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    player = GetParent<Spatial>();
    SetAsToplevel(true);
  }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
  public override void _Input(InputEvent ie) {
    // Discard anything we haven't set an action for
    if (ie.IsActionType()) {
      if (ie.GetActionStrength("cam_reset") > 0f) {
        Rotation = player.Rotation;
      }
    }
  }

  public override void _PhysicsProcess(float d) {
    Translation = player.Translation + Vector3.Up;
    inp = Input.GetVector("cam_right", "cam_left", "cam_down", "cam_up");
    Rotation = Vector3.Left * (Rotation.x + inp.y * d * 8f) + Vector3.Up * (Rotation.y + inp.x * d * 8f);
    //Rotation = Rotation + new Vector3(inp.y * d * 8f,inp.x * d * 8f, 0f);
  }
}
