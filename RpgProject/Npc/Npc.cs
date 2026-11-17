using Microsoft.Xna.Framework;
using RpgProject.Player;
using RpgProject.World;
using SummitKit.Graphics;
using SummitKit.Physics;

namespace RpgProject.Npcs;

public abstract class Npc : Entity, IInteractable, ISolid
{
    public virtual Rectangle CollisionBox
    {
        get
        {
            var box = AABB;
            int height = box.Height / 3;
            int inset = box.Width / 5;
            return new Rectangle(box.X + inset, box.Bottom - height, box.Width - inset * 2, height);
        }
    }

    protected Npc(Sprite sprite) : base(sprite)
    {
        sprite.CenterOrigin();
        sprite.Shadow = null;
        Clickable = false;
        Draggable = false;
        CollidesWithWindowEdges = false;
    }

    public virtual Rectangle InteractArea => AABB;

    public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch batch)
    {
        base.Draw(batch);
        if (SummitKit.UI.IUIElement.Debug) batch.Draw(SummitKit.Core.Pixel, CollisionBox, Color.Orange * 0.6f);
    }

    public abstract void OnInteract(Character player);

    public void PlaceOnTile(Tilemap map, int x, int y)
    {
        var tile = map.TileToWorld(x, y);
        Position = new Vector2(
            tile.X + map.TileSize / 2f - AABB.Width / 2f,
            tile.Y + map.TileSize - AABB.Height);
    }
}
