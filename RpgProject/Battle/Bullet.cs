using Microsoft.Xna.Framework;
using RpgProject.Player;
using SummitKit;
using SummitKit.Graphics;
using SummitKit.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpgProject.Battle;

public abstract class Bullet : Entity
{
    public int Attack { get; set; }
    public float Speed { get; set; } = 80f;


    public Bullet(Sprite sprite) : base(sprite)
    {
        sprite.CenterOrigin();
        sprite.Shadow = null;
        Clickable = false;
        Draggable = false;
        CollidesWithWindowEdges = false;
        Friction = Vector2.One;
    }

    public override void Update(GameTime time)
    {
        base.Update(time);

        // check if we collided w the player
        if (AABB.Intersects(PlayerFeet) && MainGame.Player is IBattleStats stats)
        {
            stats.Health -= Attack;
            Remove();
            return;
        }

        // check if we're out of bounds
        if (!Core.Camera.VisibleArea.Intersects(AABB))
        {
            Remove();
            return;
        }
    }


    protected static Rectangle PlayerFeet => Character.FeetAt(MainGame.Player.Position, MainGame.Player.AABB);
    private Vector2 HalfSize => AABB.Size.ToVector2() / 2f;

    public virtual void Spawn(Vector2 centre)
    {
        Position = centre - HalfSize;
        Add();
    }
}

public class AimedBullet(Sprite sprite, Func<Vector2>? target = null) : Bullet(sprite)
{
    public Func<Vector2> Target { get; protected set; } = target ?? (() => PlayerFeet.Center.ToVector2());

    public override void Spawn(Vector2 centre)
    {
        base.Spawn(centre);
        var aim = Target.Invoke() - centre;
        Velocity = aim == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(aim) * Speed;
    }
}