using Godot;
using System;
using System.Runtime;

public partial class CameraControl : Node3D
{
  MAIN_GUY player;
	Camera3D playerCamera;
  Vector2 inp;
	Vector3 targetRotation = Vector3.Zero;
	Vector3 initialPosition, initialRotation;
	Timer moveTimer;
	bool fullControl = false;
	[Export]
	string intent = "";
	[Export]
	int angleBound;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
		player = GetParent<MAIN_GUY>();
		playerCamera = GetNode<Camera3D>(new NodePath("Camera3D"));
		moveTimer = GetNode<Timer>(new NodePath("MoveTimer"));
		initialPosition = playerCamera.Position; // Set in Editor
		initialRotation = Rotation;
		playerCamera.Current = true;
		TopLevel = true;
  }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) {
		if (moveTimer.TimeLeft > 0) {
			float adjustment = (float)delta * ProjectSettings.GetSetting("physics/common/physics_ticks_per_second",  60).As<int>();
			switch (intent)
			{
				case "reset":
					RotateToTarget(targetRotation, 0.25f * adjustment);
					break;
				case "into_first":
					TranslateCameraToTarget(Vector3.Zero, 0.25f * adjustment);
					break;
				case "into_third":
					TranslateCameraToTarget(initialPosition, 0.25f * adjustment);
					break;
				default:
					GD.PrintErr("Unhandled Camera Intent: " + intent);
					moveTimer.Stop();
					break;
			}
		} 
	}

  public void _ToggleFirstPerson() {
		if (moveTimer.TimeLeft == 0f) {
			moveTimer.Start(1.0d);
			intent = fullControl ? "into_third" : "into_first";
			fullControl = !fullControl;
		}
  }

	public void _CameraReset() {
		if (moveTimer.TimeLeft == 0f) {
			moveTimer.Start(1.0d);
			intent = "reset";
			targetRotation = player.Rotation + initialRotation;
		}
	}

	// Called once per Physics Tickrate per second
  public override void _PhysicsProcess(double d) {
		Position = player.Position;
		if (moveTimer.TimeLeft == 0f) {
			inp = Input.GetVector("cam_right", "cam_left", "cam_up", "cam_down");
			Rotation = 
				Vector3.Right * Math.Clamp(
					Rotation.X + inp.Y * (float)d * 8f,
					DegreesToRadians(-75f),
					DegreesToRadians(75f)
				) +
				Vector3.Up * (Rotation.Y + inp.X * (float)d * 8f);
		}
  }

	public static float DegreesToRadians(float degrees) {
		return degrees * (Mathf.Pi / 180f);
	}

	void RotateToTarget(Vector3 target, float delta) {
		Rotation = Rotation.Lerp(target, delta);
		if (Math.Abs(Rotation.Y - target.Y) < .001f) {
			Rotation = target;
			moveTimer.Stop();
			intent = "";
		}
	}

	void TranslateCameraToTarget(Vector3 target, float delta) {
		playerCamera.Position = playerCamera.Position.Lerp(target, delta);
		if (playerCamera.Position.DistanceSquaredTo(target) < .001f) {
			playerCamera.Position = target;
			moveTimer.Stop();
			intent = "";
		}
	}
}
