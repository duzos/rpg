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
    public float Speed { get; set; } = 1f;

    private bool[] inputs = new bool[5];
    private Direction lastFacingDirection = Direction.North;
    private Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public Character() : base(null)
    {
        Sprite = UpdateSprite();
        Draggable = false;
        Clickable = false;
    }

    public Direction Facing => lastFacingDirection;

    private static bool Blocked(Rectangle feet, List<Rectangle> solids)
    {
        if (RoomManager.Instance.CurrentRoom.Tilemap.IsSolid(feet)) return true;
        foreach (var box in solids)
            if (box.Intersects(feet)) return true;
        return false;
    }

    private void TryInteract()
    {
        if (Core.Console.Open || RoomManager.Instance.Transitioning || TextBox.DialogueAtFrameStart || BattleState.InBattle) return;
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
        if (Core.Console.Open || RoomManager.Instance.Transitioning || TextBox.InDialogue || BattleState.InBattle)
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

    private void Move()
    {
        if (RoomManager.Instance.Transitioning || TextBox.InDialogue || BattleState.InBattle) return;

        Vector2 movement = Vector2.Zero;
        if (inputs[0]) movement.Y -= 1; // W
        if (inputs[1]) movement.Y += 1; // S
        if (inputs[2]) movement.X -= 1; // A
        if (inputs[3]) movement.X += 1; // D
        if (movement != Vector2.Zero)
        {
            movement.Normalize();
        }

        Vector2 step = movement * (Speed * (inputs[4] ? 1.5F : 1F));

        var solids = Core.Entities.Entities
            .OfType<ISolid>()
            .Where(s => !ReferenceEquals(s, this))
            .Select(s => s.CollisionBox)
            .Where(box => !box.Intersects(Feet))
            .ToList();

        var nextX = new Vector2(Position.X + step.X, Position.Y);
        if (!Blocked(FeetAt(nextX), solids)) Position = nextX;

        var nextY = new Vector2(Position.X, Position.Y + step.Y);
        if (!Blocked(FeetAt(nextY), solids)) Position = nextY;
    
        var exit = CheckForExit();
        if (exit is not null)
        {
            RoomManager.Instance.Travel(exit);
        }
    }

    private Exit? CheckForExit()
    {
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

    public override void Update(GameTime deltaTime)
    {
        Sprite = UpdateSprite();
        UpdateInputs();
        Move();
        TryInteract();
        base.Update(deltaTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        if (IUIElement.Debug)
        {
            spriteBatch.Draw(Core.Pixel, Feet, Color.Orange * 0.6f);
            spriteBatch.Draw(Core.Pixel, InteractBox, Color.Blue * 0.5f);
        }
    }

    private Sprite UpdateSprite()
    {
        string keyword = GetDirectionKeyword(lastFacingDirection);
        bool idle = inputs.All(v => v == false);
        string lookup = $"char-{keyword}-{(idle ? "idle" : "walk")}"; 
        Sprite val = sprites.GetValueOrDefault(lookup, null);
        if (val is not null) return val;

        if (keyword.Equals("left") || keyword.Equals("right"))
        {
            keyword = "side";
        }

        if (idle) val = new Sprite(MainGame.Atlas.GetRegion($"char-{keyword}-idle"));
        else
            val = new AnimatedSprite(new Animation(
            [
                MainGame.Atlas.GetRegion($"char-{keyword}-walk-1"),
                MainGame.Atlas.GetRegion($"char-{keyword}-idle"),
                MainGame.Atlas.GetRegion($"char-{keyword}-walk-2"),
                MainGame.Atlas.GetRegion($"char-{keyword}-idle"),

            ], TimeSpan.FromSeconds(0.25D)));

        val.Effects = lastFacingDirection == Direction.East ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
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


    private Rectangle FeetAt(Vector2 position)
    {
        var box = AABB;
        int height = box.Height / 4;
        int inset = box.Width / 4;
        return new Rectangle((int)position.X + inset, (int)position.Y + box.Height - height, box.Width - inset * 2, height);
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