using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RpgProject.Battle;
using RpgProject.Dialogue;
using RpgProject.World;
using SummitKit;
using SummitKit.Graphics;
using SummitKit.Input;
using SummitKit.Physics;
using SummitKit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpgProject.Player;
public class Character : Entity, IBattleStats
{
    public float Speed => 60f * (Core.Input.Keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ? 1.5F : 1F) * (BattleState.Current is not null ? 2F : 1F);

    private readonly bool[] inputs = new bool[5];
    private Direction lastFacingDirection = Direction.North;
    private readonly Dictionary<string, Sprite> sprites = [];

    public Character() : base(null)
    {
        Sprite = UpdateSprite();
        Draggable = false;
        Clickable = false;
    }

    public Direction Facing => lastFacingDirection;
    public Color HealthBarColour { get; set; } = Color.Lime;
    public float MaxInvincibility { get; } = 2f;
    public float Invincibility { get; set; }
    private static bool BattleLocksMovement => BattleState.Current is { Phase: not BattlePhase.EnemyAttack };
    public bool CanMove => !(Core.Console.Open || RoomManager.Instance.Transitioning || TextBox.DialogueAtFrameStart || MoveTarget is not null || BattleLocksMovement);
    public bool CanInteract => CanMove && !BattleState.InBattle;

    private bool Blocked(Vector2 position, List<Rectangle> solids)
    {
        var feet = FeetAt(position);
        if (BattleState.Current is null && RoomManager.Instance.CurrentRoom.Tilemap.IsSolid(feet)) return true;
        if (BattleState.Current is not null && !Core.Camera.VisibleArea.Contains(AabbAt(position))) return true;

        foreach (var box in solids)
             if (box.Intersects(feet)) return true;
        return false;
    }

    private void TryInteract()
    {
        if (!CanInteract) return;
        if (!Core.Input.Keyboard.WasKeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Space)) return;

        var box = InteractBox;
        var feet = Feet.Center.ToVector2();
        var target = Core.Entities.Entities
            .OfType<IInteractable>()
            .Where(i => !ReferenceEquals(i, this) && i.InteractArea.Intersects(box))
            .OrderBy(i => Vector2.DistanceSquared(i.InteractArea.Center.ToVector2(), feet))
            .FirstOrDefault();

        target?.OnInteract(this);
    }

    private void UpdateInputs()
    { 
        if (!CanMove)
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i] = false;
            }
            return;
        }

        inputs[0] = Core.Input.Keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W);
        inputs[1] = Core.Input.Keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S);
        inputs[2] = Core.Input.Keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A);
        inputs[3] = Core.Input.Keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D);
        inputs[4] = Core.Input.Keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift);

        lastFacingDirection = inputs[0] ? Direction.North :
                              inputs[1] ? Direction.South :
                              inputs[2] ? Direction.West :
                              inputs[3] ? Direction.East : lastFacingDirection;
    }

    private void Move(GameTime time)
    {
        if (!CanMove) return;

        Vector2 movement = Vector2.Zero;
        if (inputs[0]) movement.Y -= 1; // W
        if (inputs[1]) movement.Y += 1; // S
        if (inputs[2]) movement.X -= 1; // A
        if (inputs[3]) movement.X += 1; // D
        if (movement != Vector2.Zero)
        {
            movement.Normalize();
        }

        Vector2 step = movement * Speed * (float)time.ElapsedGameTime.TotalSeconds;

        var solids = Core.Entities.Entities
            .OfType<ISolid>()
            .Where(s => !ReferenceEquals(s, this))
            .Select(s => s.CollisionBox)
            .Where(box => !box.Intersects(Feet))
            .ToList();

        var nextX = new Vector2(Position.X + step.X, Position.Y);
        if (!Blocked(nextX, solids)) Position = nextX;

        var nextY = new Vector2(Position.X, Position.Y + step.Y);
        if (!Blocked(nextY, solids)) Position = nextY;
    
        var exit = CheckForExit();
        if (exit is not null)
        {
            RoomManager.Instance.Travel(exit);
        }
    }

    private Exit? CheckForExit()
    {
        if (BattleState.Current is not null) return null;

        var room = RoomManager.Instance.CurrentRoom;
        foreach (var exit in room.Exits)
        {
            var exitArea = new Rectangle(exit.Region.X * room.Tilemap.TileSize, exit.Region.Y * room.Tilemap.TileSize, exit.Region.Width * room.Tilemap.TileSize, exit.Region.Height * room.Tilemap.TileSize);
            if (exitArea.Intersects(Feet))
            {
                return exit;
            }
        }
        return null;
    }

    public void SetFacing(Direction direction)
    {
        lastFacingDirection = direction;
        Sprite = UpdateSprite();
    }

    private enum Pose { None, Draw, Idle, Attack, Sheathe }

    private readonly record struct PoseInfo(string Region, int Frames, double Delay, Vector2 Anchor, bool Loop);

    private static readonly Dictionary<Pose, PoseInfo> Poses = new()
    {
        [Pose.Draw] = new("char-battle-attack", 8, 0.07, new Vector2(12, 42), false),
        [Pose.Attack] = new("char-battle-attack", 8, 0.07, new Vector2(12, 42), false),
        [Pose.Idle] = new("char-battle-idle", 6, 0.15, new Vector2(12, 37), true),
        [Pose.Sheathe] = new("char-battle-end", 9, 0.06, new Vector2(10, 36), false),
    };

    private static readonly Vector2 BattleBodySize = new(19, 38);
    private static readonly Vector2 BattleFeet = new(8.5f, 36);

    private Pose _pose = Pose.None;
    private AnimatedSprite? _poseSprite;
    private double _poseTime;

    private bool UseBattleBody => BattleState.Current is not null || _pose != Pose.None;
    public override float Width => UseBattleBody ? BattleBodySize.X : base.Width;
    public override float Height => UseBattleBody ? BattleBodySize.Y : base.Height;

    public void OnPhaseStart(BattleState state, BattlePhase phase)
    {
        switch (phase)
        {
            case BattlePhase.Intro: PlayPose(Pose.Draw); break;
            case BattlePhase.Menu: if (_pose == Pose.None) PlayPose(Pose.Draw); break;
            case BattlePhase.PlayerAction: PlayPose(Pose.Attack); break;
            case BattlePhase.EnemyAttack: PlayPose(Pose.Sheathe); break;
            case BattlePhase.Outro: if (_pose != Pose.None) PlayPose(Pose.Sheathe); break;
            default: StopPose(); break;
        }
    }

    public void OnBattleEnd(BattleState state)
    {
        if (_pose != Pose.Sheathe) StopPose();
    }

    private void PlayPose(Pose pose)
    {
        var info = Poses[pose];
        var frames = Enumerable.Range(0, info.Frames).Select(i => MainGame.Atlas.GetRegion($"{info.Region}-{i}")).ToList();
        _poseSprite = new AnimatedSprite(new Animation(frames, TimeSpan.FromSeconds(info.Delay)))
        {
            PlayType = info.Loop ? AnimatedSprite.Type.LOOP : AnimatedSprite.Type.HOLD,
            Origin = info.Anchor + BattleBodySize / 2f - BattleFeet,
        };
        _poseSprite.Shadow.Enabled = false;
        _pose = pose;
        _poseTime = 0;
        Sprite = _poseSprite;
    }

    private void StopPose()
    {
        _pose = Pose.None;
        _poseSprite = null;
    }

    private void UpdatePose(GameTime time)
    {
        if (_pose == Pose.None) return;
        if (BattleState.Current is null && _pose != Pose.Sheathe)
        {
            StopPose();
            return;
        }

        if (_pose == Pose.Sheathe && inputs.Take(4).Any(v => v))
        {
            StopPose();
            return;
        }

        var info = Poses[_pose];
        _poseTime += time.ElapsedGameTime.TotalSeconds;
        if (info.Loop || _poseTime < info.Frames * info.Delay) return;

        if (_pose == Pose.Sheathe) StopPose();
        else PlayPose(Pose.Idle);
    }

    public override void Update(GameTime deltaTime)
    {
        UpdatePose(deltaTime);
        Sprite = UpdateSprite();
        UpdateInputs();
        Move(deltaTime);
        TryInteract();
        base.Update(deltaTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        bool flicker = Invincibility > 0 && (int)(Invincibility * 8) % 2 == 0;
        if (Sprite is not null) Sprite.Colour = flicker ? Color.White * 0.35f : Color.White;
        base.Draw(spriteBatch);
        if (IUIElement.Debug)
        {
            spriteBatch.Draw(Core.Pixel, Feet, Color.Orange * 0.6f);
            spriteBatch.Draw(Core.Pixel, InteractBox, Color.Blue * 0.5f);
        }
    }

    private Sprite UpdateSprite()
    {
        if (_pose != Pose.None && _poseSprite is not null) return _poseSprite;

        string keyword = GetDirectionKeyword(lastFacingDirection);
        bool idle = !inputs.Take(4).Any(v => v);
        var battle = BattleState.Current is not null ? "-battle" : "";
        string lookup = $"char{battle}-{keyword}-{(idle ? "idle" : "walk")}"; 
        Sprite val = sprites.GetValueOrDefault(lookup, null);
        if (val is not null) return val;

        if (BattleState.Current is null && (keyword.Equals("left") || keyword.Equals("right")))
        {
            keyword = "side";
        }


        if (idle) val = new Sprite(MainGame.Atlas.GetRegion($"char{battle}-{keyword}-idle"));
        else
            val = new AnimatedSprite(new Animation(
            [
                MainGame.Atlas.GetRegion($"char{battle}-{keyword}-walk-1"),
                MainGame.Atlas.GetRegion($"char{battle}-{keyword}-idle"),
                MainGame.Atlas.GetRegion($"char{battle}-{keyword}-walk-2"),
                MainGame.Atlas.GetRegion($"char{battle}-{keyword}-idle"),

            ], TimeSpan.FromSeconds(0.25D)));

        val.Effects = lastFacingDirection == Direction.East && battle == "" ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        val.CenterOrigin();
        val.Shadow.Enabled = false;

        sprites[lookup] = val;

        return val;
    }

    private static string GetDirectionKeyword(Direction direction)
    {
        return direction switch
        {
            Direction.North => "back",
            Direction.South => "forward",
            Direction.East => "right",
            Direction.West => "left",
            _ => "forward"
        };
    }


    public static Rectangle FeetAt(Vector2 position, Rectangle aabb)
    {
        var box = aabb;
        int height = box.Height / 4;
        int inset = box.Width / 4;
        return new Rectangle((int)position.X + inset, (int)position.Y + box.Height - height, box.Width - inset * 2, height);
    }

    private Rectangle FeetAt(Vector2 position) => FeetAt(position, AABB);

    public static Rectangle PlayerFeet => FeetAt(MainGame.Player.Position, MainGame.Player.AABB);
    public static Vector2 PlayerFeetCentre => PlayerFeet.Center.ToVector2();

    private Rectangle AabbAt(Vector2 position)
    {
        var box = AABB;
        return new Rectangle((int)position.X, (int)position.Y, box.Width, box.Height);
    }

    private Rectangle InteractBoxAt(Vector2 position)
    {
        var feet = FeetAt(position);
        int reach = RoomManager.Instance.CurrentRoom?.Tilemap.TileSize ?? 20;
        int across = Math.Max(reach, AABB.Width);
        var centre = feet.Center;
        return lastFacingDirection switch
        {
            Direction.North => new Rectangle(centre.X - across / 2, feet.Top - reach, across, reach),
            Direction.South => new Rectangle(centre.X - across / 2, feet.Bottom, across, reach),
            Direction.West => new Rectangle(feet.Left - reach, centre.Y - across / 2, reach, across),
            _ => new Rectangle(feet.Right, centre.Y - across / 2, reach, across),
        };
    }

    public Rectangle Feet => FeetAt(Position);
    public Rectangle InteractBox => InteractBoxAt(Position);

    public int Health { get; set; } = 20;
    public int MaxHealth { get; set; } = 20;
    public int Attack { get; set; } = 1;

    public void PlaceFeetOnTile(Tilemap map, int x, int y)
    {
        var tileCentre = map.TileToWorld(x, y) + new Vector2(map.TileSize / 2f);
        Position = tileCentre - (Feet.Center.ToVector2() - Position);
    }
}