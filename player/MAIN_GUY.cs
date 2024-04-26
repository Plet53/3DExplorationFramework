using Godot;
using System;

public partial class MAIN_GUY : CharacterBody3D {
  [Export(PropertyHint.Range, "1,100,or_greater")]
  private float _accel, max_speed, gravity, max_rot, jump_vol;
  [Export(PropertyHint.Range, "-100,-1,or_lesser")]
  private float term_vol;
  private float speed;
	private string action;
  [Export]
  private Vector3 vel, dir, wall;
  private Vector2 inp;
  private Node3D cam;
	[Export]
	private Node3D spawnPoint;
  private KinematicCollision3D last_col;
  [Export]
  private bool climbing = false, grounded = true, mobile = true;
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
		action = "mobile";
  }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta) {
//    
//  }

  // Input stuff
  public override void _Input(InputEvent ie) {
	// Discard anything we haven't set an action for
		if (ie.IsActionType()) {
			if (ie.GetActionStrength("game_jump") > 0f) {
				if (grounded) {
					vel.Y += jump_vol;
					grounded = false; 
				} else if (climbing) {
					vel.Y += jump_vol;
					GlobalRotate(Vector3.Up, (float)Math.PI);
					vel.X += wall.X * Accel * 5;
					vel.Z += wall.Y * Accel * 5;
					Velocity = vel;
					MoveAndSlide();
					speed = 4 * Accel;
					climbing = false;
					wall = Vector3.Zero;
				}
			}
			if (ie.GetActionStrength("cam_first_person") > 0f && action == "mobile") {
				action = "looking";
			}
		}
  }

  public override void _PhysicsProcess(double d) {
		// Pre-Limited, Thanks Godot
		inp = Input.GetVector("game_right", "game_left", "game_backward", "game_forward");
		if (action == "mobile") {
			HandleMovementInput(inp);
		}
		// This check needs to happen after movement so jumping is a thing that always happens.
		// The collision engine *should* take care of the rest.
		if (IsOnFloor()) {
			vel.Y = 0;
			grounded = true;
			climbing = false;
		} else { CheckForClimbing(); }
  }

	bool HandleMovementInput(Vector2 input) {
		last_col = GetLastSlideCollision();
		if (last_col != null && IsOnWall()) {
			wall = last_col.GetNormal();
		} else {
			wall = Vector3.Zero;
		}
		if (inp == Vector2.Zero) {
			speed = TowardZero(speed, Accel);
		} else {
			// Capping Max Speed to input strength allows for Walking with analog inputs.
			speed = Math.Min(speed + (Accel * inp.Length()), max_speed * inp.Length());
			Rotation = climbing
				? Vector3.Right * (float)Math.Asin(wall.Y) + Vector3.Up * (float)(Math.Atan2(wall.X, wall.Z) - Math.PI)
				: Vector3.Down * (inp.Angle() - ((float)Math.PI / 2) - cam.Rotation.Y);
		}
		if (!climbing) {
			inp = inp.Rotated(-cam.Rotation.Y);
			inp *= speed;
			vel.X = inp.X; vel.Z = inp.Y;
		} else {
			vel.X = (speed * inp.X * (float)Math.Cos(Math.Atan2(wall.Z, wall.X) + Math.PI / 2)) - (2 * wall.X);
			vel.Z = (speed * inp.X * (float)Math.Sin(Math.Atan2(wall.Z, wall.X) + Math.PI / 2)) - (2 * wall.Z);
			vel.Y = speed * inp.Y;
		}
		// This is ultimately what I still want
		if (climbing) {
			// Push movement vector into wall while climbing
			vel += new Vector3(wall.X * -3f, 0f, wall.Y * -3f);
			// Angle is 5PI / 12, or, 75 Degrees.
			// MoveAndSlideWithSnap(vel, new Vector3(-wall.X * 3f, 0f, -wall.Y * 3f), Vector3.Up, true, 4, 1.309f);
		}
		Velocity = vel;
		dir = vel.Normalized();
		return MoveAndSlide();
	}
	public void OnDeathPlaneEntered(Node3D body) {
		// Argument is discarded as it doesn't tell us anything special
		GD.Print("ded");
		Position = spawnPoint.Position;
		vel = Vector3.Zero;
	}

	public void ResetAction() {
		action = "mobile";
	}

	void CheckForClimbing() {
		if (!climbing) {
			if (wall != Vector3.Zero) {
				GD.Print(wall);
				if ((wall.X + dir.X + wall.Z + dir.Z) < .3f) {
					vel.Y = 0;
					speed = 0;
					climbing = true;
				}
			} else {
				vel.Y = Math.Max(vel.Y - gravity, TerminalVelocity);
				if (vel.Y == TerminalVelocity) {
					grounded = false;
				}
			}
		} else {
			if (!IsOnWall() && inp != Vector2.Zero) {
				climbing = false;
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
