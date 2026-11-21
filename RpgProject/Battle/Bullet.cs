using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using RpgProject.Player;
using SummitKit;
using SummitKit.Graphics;
using SummitKit.Physics;
using SummitKit.UI;
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
    protected virtual bool CanHit => true;

    public Bullet(Sprite sprite) : base(sprite)
    {
        sprite.CenterOrigin();
        sprite.Shadow = null;
        Clickable = false;
        Draggable = false;
        CollidesWithWindowEdges = false;
        Friction = Vector2.One;
    }

    public bool IsRemoved { get; private set; }

    public override void OnRemove()
    {
        base.OnRemove();
        IsRemoved = true;
    }

    public override void Update(GameTime time)
    {
        base.Update(time);

        // check if we collided w the player
        if (CanHit && AABB.Intersects(Character.PlayerFeet) && MainGame.Player is IBattleStats stats && stats.Damage(Attack))
        {
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


    protected Vector2 HalfSize => AABB.Size.ToVector2() / 2f;

    public virtual void Spawn(Vector2 centre)
    {
        Position = centre - HalfSize;
        Add();
    }
}

public class AimedBullet(Sprite sprite, Func<Vector2>? target = null) : Bullet(sprite)
{
    public Func<Vector2> Target { get; protected set; } = target ?? (() => Character.PlayerFeetCentre);

    public override void Spawn(Vector2 centre)
    {
        base.Spawn(centre);
        var aim = Target.Invoke() - centre;
        Velocity = aim == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(aim) * Speed;
    }
}

public class ArcBullet(Sprite sprite) : Bullet(sprite)
{
    public Vector2 Start { get; private set; }
    public required Vector2 Target { get; init; }
    public float? ArcHeight { get; init; }
    private float arcHeight;
    public TimeSpan? Duration { get; init; }
    public Color ShadowColour { get; init; } = new Color(150, 90, 170) * 0.5f;
    private TimeSpan elapsed = TimeSpan.Zero;
    private TimeSpan duration = TimeSpan.FromSeconds(0.1);
    private float Progress => Math.Min(1f, (float)(elapsed / duration));
    private float CurrentHeight => 4 * arcHeight * Progress * (1 - Progress);
    protected override bool CanHit => Progress > 0.5F && CurrentHeight < 4F;

    public override void Spawn(Vector2 centre)
    {
        base.Spawn(centre);
        Start = centre;

        var requested = Duration ?? TimeSpan.FromSeconds(Vector2.Distance(Start, Target) / Speed);
        duration = requested < TimeSpan.FromSeconds(0.1) ? TimeSpan.FromSeconds(0.1) : requested;
        arcHeight = ArcHeight ?? Math.Clamp(Vector2.Distance(Start, Target) * 0.4f, 32f, 64f);
    }

    public override void Update(GameTime time)
    {
        elapsed += time.ElapsedGameTime;

        var ground = Vector2.Lerp(Start, Target, Progress);
        Position = ground - HalfSize;

        base.Update(time);

        if (!IsRemoved && Progress >= 1f) Remove();
    }

    public override void Draw(SpriteBatch batch)
    {
        if (Sprite is null) return;

        var ground = AABB.Center.ToVector2();
        var region = Sprite.Region;
        float lift = arcHeight <= 0 ? 0 : CurrentHeight / arcHeight;
        float shadowWidth = 1f - 0.4f * lift;
        var shadowScale = new Vector2(shadowWidth, shadowWidth * 0.35f);
        var shadowCentre = new Vector2(ground.X, AABB.Bottom - region.Height * shadowScale.Y / 2f);
        region.Draw(batch, shadowCentre, ShadowColour, 0f, new Vector2(region.Width, region.Height) / 2f, shadowScale, SpriteEffects.None, 0f);

        Sprite.Colour = Color.White * MathHelper.Lerp(1f, 0.6f, lift);
        Sprite.Draw(batch, ground - new Vector2(0, CurrentHeight));

        if (IUIElement.Debug) batch.Draw(Core.Pixel, AABB, Color.Red * 0.5F);
    }
}