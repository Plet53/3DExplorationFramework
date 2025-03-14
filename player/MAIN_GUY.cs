using Godot;
using System;

public partial class MAIN_GUY : CharacterBody3D {
	enum ActionState {
		Standing,
		Walking,
		Running,
		Looking,
		GroundCharging,
		GroundBoosting,
		SpinAttack,
		Falling,
		Jumping,
		AirCharging,
		AirBoosting,
		Gliding,
		Climbing,
		Knockback
	}

	[Signal]
	delegate void CameraResetEventHandler();
	[Signal]
	delegate void ToggleFirstPersonEventHandler();

  [Export(PropertyHint.Range, "1,100,or_greater")]
  private float _accel, max_speed, gravity, max_rot, jump_vol;
  [Export(PropertyHint.Range, "-100,-1,or_lesser")]
  private float term_vol;
	private float max_speed_modifier = 1.0f;
  private float speed;
	[Export]
	private ActionState action_state;
  [Export]
  private Vector3 vel, dir, wall;
  private Vector2 inp;
  private Node3D cam;
	[Export]
	private Node3D spawnPoint;
  private KinematicCollision3D last_col;
  [Export]
  private bool grounded = true;
  public bool Grounded {get => grounded; set => grounded = value;}	
  public float Accel { get => _accel; set => _accel = Math.Max(value, 1.0f); }
  public float MaxSpeed { get => max_speed; set => max_speed = Math.Max(value, 1.0f); }
  public float MaxRotation {get => max_rot; set => max_rot = Math.Max(value, 1.0f);}
  public float Gravity { get => gravity; set => gravity = Math.Max(value, 1.0f); }
	public float JumpVelocity { get => jump_vol; set => jump_vol = Math.Max(value, 1.0f); }
  public float TerminalVelocity { get => term_vol; set => term_vol = Math.Min(value, -1.0f); }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
		vel = Vector3.Zero;
		wall = Vector3.Zero;
		cam = GetNode<Node3D>(new NodePath("CamPoint"));
		action_state = ActionState.Standing;
  }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta) {
//    
//  }

  // Input stuff
  public override void _UnhandledInput(InputEvent ie) {
	// Discard anything we haven't set an action for
		if (ie.IsActionType()) {
			if (ie.GetActionStrength("game_jump") > 0f) {
				if (grounded) {
					vel.Y += jump_vol;
					grounded = false; 
					action_state = ActionState.Jumping;
				} else if (action_state == ActionState.Climbing) {
					vel.Y += jump_vol;
					GlobalRotate(Vector3.Up, (float)Math.PI);
					vel.X += wall.X * max_speed * 0.8f;
					vel.Z += wall.Y * max_speed * 0.8f;
					Velocity = vel;
					MoveAndSlide();
					speed = Mathf.Sqrt((vel.X * vel.X) + (vel.Z * vel.Z));
					action_state = ActionState.Jumping;
					wall = Vector3.Zero;
				}
			}
			if (ie.GetActionStrength("cam_first_person") > 0f) {
				if (action_state == ActionState.Standing) {
					action_state = ActionState.Looking;
					GD.Print("Into First " + action_state);
					EmitSignal("ToggleFirstPerson");
				} else if (action_state == ActionState.Looking) {
					action_state = ActionState.Standing;
					GD.Print("Into Third " + action_state);
					EmitSignal("ToggleFirstPerson");
				}
			}
			if (ie.GetActionStrength("cam_reset") > 0f) {
				EmitSignal("CameraReset");
			}
		}
  }

  public override void _PhysicsProcess(double d) {
		// Pre-Limited, Thanks Godot
		inp = Input.GetVector("game_right", "game_left", "game_backward", "game_forward");
		if (!(action_state == ActionState.Knockback || action_state == ActionState.Looking)) {
			HandleMovementInput(inp);
		}
		// This check needs to happen after movement so jumping is a thing that always happens.
		// The collision engine *should* take care of the rest.
		if (action_state != ActionState.Looking) {
			if (IsOnFloor()) {
				vel.Y = 0;
				grounded = true;
				SetGroundActionFromSpeed();
			} else {
				CheckForClimbing();
			}
		}
  }

	bool HandleMovementInput(Vector2 input) {
		last_col = GetLastSlideCollision();
		if (last_col != null && IsOnWall()) {
			wall = last_col.GetNormal();
		} else {
			wall = Vector3.Zero;
		}
		if (input == Vector2.Zero) {
			speed = TowardZero(speed, Accel);
		} else {
			// Capping Max Speed to input strength allows for Walking with analog inputs.
			speed = Math.Min(speed + (Accel * inp.Length()), max_speed * input.Length());
			Rotation = action_state == ActionState.Climbing
				? Vector3.Right * (float)Math.Asin(wall.Y) + Vector3.Up * (float)(Math.Atan2(wall.X, wall.Z) - Math.PI)
				: Vector3.Down * (inp.Angle() - ((float)Math.PI / 2) - cam.Rotation.Y);
		}
		if (!(action_state == ActionState.Climbing)) {
			inp = input.Rotated(-cam.Rotation.Y);
			inp *= speed;
			vel.X = inp.X; vel.Z = inp.Y;
		} else {
			vel.X = speed * inp.X * (float)Math.Cos(Math.Atan2(wall.Z, wall.X) + Math.PI / 2);
			vel.Z = speed * inp.X * (float)Math.Sin(Math.Atan2(wall.Z, wall.X) + Math.PI / 2);
			vel.Y = speed * inp.Y;
		}
		// This is ultimately what I still want
		if (action_state == ActionState.Climbing) {
			// Push movement vector into wall while climbing
			vel += wall * -5f;
			// Angle is 5PI / 12, or, 75 Degrees.
			// MoveAndSlideWithSnap(vel, new Vector3(-wall.X * 3f, 0f, -wall.Y * 3f), Vector3.Up, true, 4, 1.309f);
		}
		Velocity = vel;
		dir = vel.Normalized();
		return MoveAndSlide();
	}

	// Thinking about a refactor, effect areas send signals to a controller that sends down to relevant nodes
	public void OnDeathPlaneEntered(Node3D body) {
		GD.Print(body.Name);
		if (body == this) {
			Position = spawnPoint.Position;
			vel = Vector3.Zero;
		}
	}

	public void ResetAction() {
		action_state = ActionState.Standing;
	}

	public void SetGroundActionFromSpeed() {
		if (speed == 0f) {
			action_state = ActionState.Standing;
		} else if (speed > max_speed * .66f) {
			action_state = ActionState.Running;
		} else {
			action_state = ActionState.Walking;
		}
	}

	void CheckForClimbing() {
		if (!(action_state == ActionState.Climbing)) {
			if (wall != Vector3.Zero) {
				// GD.Print(wall);
				if ((wall.X + dir.X + wall.Z + dir.Z) < .3f) {
					vel.Y = 0;
					speed = 0;
					action_state = ActionState.Climbing;
				}
			} else {
				vel.Y = Math.Max(vel.Y - gravity, TerminalVelocity);
				if (vel.Y == TerminalVelocity) {
					grounded = false;
				}
			}
		} else {
			if (!IsOnWall() && inp != Vector2.Zero) {
				SetGroundActionFromSpeed();
			}
		}
	}


  static float TowardZero(float q, float a) {
		float r = q - (a * Math.Sign(q));
		if (Math.Sign(r) != Math.Sign(q)) {
			r = 0.0f;
		}
		return r;
  }
}
