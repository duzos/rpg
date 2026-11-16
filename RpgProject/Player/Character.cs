using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
public class Character : Entity
{
    public float Speed { get; set; } = 1f;

    private bool[] inputs = new bool[4];
    private int lastFacingDirection = 0; // 0: Up, 1: Down, 2: Left, 3: Right
    private Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public Character() : base(null)
    {
        Sprite = UpdateSprite();
    }

    private void UpdateInputs()
    {
        if (Core.Console.Open)
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

        lastFacingDirection = inputs[0] ? 0 : inputs[1] ? 1 : inputs[2] ? 2 : inputs[3] ? 3 : lastFacingDirection;
    }

    private void Move()
    {
        Vector2 movement = Vector2.Zero;
        if (inputs[0]) movement.Y -= 1; // W
        if (inputs[1]) movement.Y += 1; // S
        if (inputs[2]) movement.X -= 1; // A
        if (inputs[3]) movement.X += 1; // D
        if (movement != Vector2.Zero)
        {
            movement.Normalize();
        }

        Vector2 step = movement * Speed;

        var nextX = new Vector2(Position.X + step.X, Position.Y);
        if (!MainGame.Map.IsSolid(FeetAt(nextX))) Position = nextX;

        var nextY = new Vector2(Position.X, Position.Y + step.Y);
        if (!MainGame.Map.IsSolid(FeetAt(nextY)) && nextY.Y >= MainGame.Map.Bounds.Top) Position = nextY;
    }

    public override void Update(GameTime deltaTime)
    {
        Sprite = UpdateSprite();
        UpdateInputs();
        Move();
        base.Update(deltaTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        if (IUIElement.Debug) spriteBatch.Draw(Core.Pixel, Feet, Color.Orange * 0.6f);
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

        val.Effects = lastFacingDirection == 3 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        val.CenterOrigin();
        val.Shadow.Enabled = false;

        sprites[lookup] = val;

        return val;
    }

    private static string GetDirectionKeyword(int direction)
    {
        return direction switch
        {
            0 => "back",
            1 => "forward",
            2 => "left",
            3 => "right",
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

    public Rectangle Feet => FeetAt(Position);

    public void PlaceFeetOnTile(Tilemap map, int x, int y)
    {
        var tileCentre = map.TileToWorld(x, y) + new Vector2(map.TileSize / 2f);
        Position = tileCentre - (Feet.Center.ToVector2() - Position);
    }
}