using Godot;
using System;

public class MAIN_GUY : KinematicBody {
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";
  [Export(PropertyHint.Range, "1,100,or_greater")]
  private float _accel, max_speed, gravity, max_rot;
  [Export(PropertyHint.Range, "-100,-1,or_lesser")]
  private float term_vol;
  private float speed;
  [Export]
  private Vector3 vel, dir, wall;
  private Vector2 inp;
  private Spatial cam;
  private KinematicCollision last_col;
  [Export]
  private bool climbing = false, grounded = true;
  public bool Grounded {get => grounded; set => grounded = value;}

  public float Accel { get => _accel; set => _accel = Math.Max(value, 1.0f); }
  public float MaxSpeed { get => max_speed; set => max_speed = Math.Max(value, 1.0f); }
  public float MaxRotation {get => max_rot; set => max_rot = Math.Max(value, 1.0f);}
  public float Gravity { get => gravity; set => gravity = Math.Max(value, 1.0f); }
  public float TerminalVelocity { get => term_vol; set => term_vol = Math.Min(value, -1.0f); }

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    vel = Vector3.Zero;
    wall = Vector3.Zero;
    cam = GetNode<Spatial>(new NodePath("CamPoint"));
  }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta) {
//      
//  }

  // Input stuff
  public override void _Input(InputEvent ie) {
    // Discard anything we haven't set an action for
    if (ie.IsActionType()) {
      if (ie.GetActionStrength("game_jump") > 0) {
        if (grounded) {
          vel.y += 30f;
          grounded = false; 
        } else if (climbing) {
          vel.y += 30f;
          GlobalRotate(Vector3.Up, (float)Math.PI);
          vel.x += wall.x * Accel * 5;
          vel.z += wall.y * Accel * 5;
          speed = 4 * Accel;
          climbing = false;
          wall = Vector3.Zero;
        }
      }
    }
  }

  // TODO: ...Now, we need to factor in the Camera Angle to movement.
  public override void _PhysicsProcess(float d) {
    // Pre-Limited, Thanks Godot
    inp = Input.GetVector("game_right", "game_left", "game_backward", "game_forward");
    last_col = GetLastSlideCollision();
    if (last_col != null && IsOnWall()) {
      wall = last_col.Normal;
    } else {
      wall = Vector3.Zero;
    }
    if (inp == Vector2.Zero) {
      speed = TowardZero(speed, Accel);
    } else {
      // Capping Max Speed to input strength allows for Walking with analog inputs.
      speed = Math.Min(speed + (Accel * inp.Length()), max_speed * inp.Length());
      Rotation = climbing
        ? Vector3.Right * (float)Math.Asin(wall.y) + Vector3.Up * (float)(Math.Atan2(wall.x, wall.z) - (Math.PI))
        : Vector3.Down * (inp.Angle() - ((float)Math.PI / 2) - cam.Rotation.y);
    }
    if (!climbing) {
      inp = inp.Rotated(-cam.Rotation.y);
      cam.Rotation += Vector3.Up * inp.x * d * 4f;
      inp *= speed;
      vel.x = inp.x; vel.z = inp.y;
    } else {
      vel.x = (speed * inp.x * (float)Math.Cos(Math.Atan2(wall.z, wall.x) + Math.PI / 2)) - (2 * wall.x);
      vel.z = (speed * inp.x * (float)Math.Sin(Math.Atan2(wall.z, wall.x) + Math.PI / 2)) - (2 * wall.z);
      vel.y = speed * inp.y;
    }
    dir = vel.Normalized();
    // This is ultimately what I still want
    if (climbing) {
      MoveAndSlideWithSnap(vel, new Vector3(-wall.x * 3f, 0f, -wall.y * 3f), Vector3.Up, true, 4, 1.309f);
    } else {
      // Angle is 5PI / 12, or, 75 Degrees.
      MoveAndSlide(vel, Vector3.Up, true, 4, 1.309f);
    }
    // This check needs to happen after movement so jumping is a thing that always happens.
    // The collision engine *should* take care of the rest.
    if (IsOnFloor()) {
      vel.y = 0;
      grounded = true;
      climbing = false;
    } else {
      if (!climbing) {
        if (wall != Vector3.Zero) {
          GD.Print(wall);
          if ((wall.x + dir.x) + (wall.z + dir.z) < .3f) {
            vel.y = 0;
            speed = 0;
            climbing = true;
          }
        } else {
        vel.y = Math.Max(vel.y - gravity, TerminalVelocity);
        if (vel.y == TerminalVelocity) {
          grounded = false;
          }
        }
      } else {
        if (!IsOnWall() && inp != Vector2.Zero) {
          climbing = false;
        }
      }
    }
  }

  float TowardZero(float q, float a) {
    float r = q - (a * Math.Sign(q));
    if (Math.Sign(r) != Math.Sign(q)) {
      r = 0.0f;
    }
    return r;
  }
}
