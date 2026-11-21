using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RpgProject.Dialogue;
using SummitKit;
using SummitKit.Graphics;
using SummitKit.Physics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpgProject.Battle;

public sealed class BattleUI : IDraw, IUpdating
{
    public static readonly BattleUI Instance = new();
    private float _scale = 4f;
    public float Scale
    {
        get => _scale;
        set
        {
            if (_scale == value) return;
            _scale = value;
            Width = 0;
        }
    }
    public float LayerDepth { get; set; }
    public bool Visible => BattleState.Current?.Phase == BattlePhase.Menu && !TextBox.DialogueAtFrameStart;
    public BattleAction Selected { get; set; } = BattleAction.Fight;

    private readonly Color UnselectedColour = new(255, 127, 39);
    private readonly Color SelectedColour = Color.Yellow;
    private float Width { get; set; } = 0F;
    private static readonly BattleAction[] Actions = Enum.GetValues<BattleAction>();
    private static Vector2 BottomCentre
    {
        get
        {
            var viewport = Core.GraphicsDevice.Viewport;
            return new Vector2(viewport.Width / 2f, viewport.Height);
        }
    }
    private float CalculateWidth()
    {
        float width = 0;
        foreach (var action in Actions)
        {
            width += action.Sprite().Region.Width * Scale;
        }
        return width;
    }

    public void Draw(SpriteBatch batch)
    {
        if (!Visible) return;

        if (Width == 0) Width = CalculateWidth();

        float cursor = BottomCentre.X - Width / 2F;
        foreach (var action in Actions)
        {
            var sprite = action.Sprite();

            bool selected = Selected == action;
            var colour = selected ? SelectedColour : UnselectedColour;
            sprite.Colour = colour;
            sprite.Scale = new Vector2(Scale);
            sprite.Draw(batch, new Vector2(cursor, BottomCentre.Y - sprite.Height));

            cursor += sprite.Width;
        }
    }

    public void Update(GameTime deltaTime)
    {
        if (!Visible) return;

        if (Core.Input.Keyboard.WasKeyJustPressed(Microsoft.Xna.Framework.Input.Keys.A)) Selected = Selected.Previous();
        else if (Core.Input.Keyboard.WasKeyJustPressed(Microsoft.Xna.Framework.Input.Keys.D)) Selected = Selected.Next();
        else if (Core.Input.Keyboard.WasKeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Space)) OnInteract(deltaTime);
    }

    private void OnInteract(GameTime deltaTime)
    {
        if (BattleState.Current is not { } battle) return;

        switch (Selected)
        {
            case BattleAction.Fight:
                var living = battle.Enemies.Where(e => e.Health > 0).ToArray();
                if (living.Length > 0)
                {
                    Random.Shared.Shuffle(living);
                    int total = battle.PlayerTeam.Where(p => p.Health > 0).Sum(p => p.Attack);
                    int share = total / living.Length;
                    int remainder = total % living.Length;

                    for (int i = 0; i < living.Length; i++)
                    {
                        int damage = share + (i < remainder ? 1 : 0);
                        living[i].Damage(damage);
                    }
                }

                battle.Advance(); break;
            case BattleAction.Spare:
                battle.End(); break;
        }
    }
}

public enum BattleAction { 
    Fight,
    Spare
}

public static class BattleActionExtensions
{
    private static readonly Dictionary<BattleAction, Sprite> sprites = [];
    private static readonly BattleAction[] all = Enum.GetValues<BattleAction>();

    public static BattleAction Next(this BattleAction action) =>
        all[(Array.IndexOf(all, action) + 1) % all.Length];

    public static BattleAction Previous(this BattleAction action) =>
        all[(Array.IndexOf(all, action) - 1 + all.Length) % all.Length];

    public static Sprite Sprite(this BattleAction action)
    {
        if (!sprites.TryGetValue(action, out var sprite))
        {
            sprite = new Sprite(MainGame.Atlas.GetRegion(action.ToString().ToLowerInvariant()));
            sprites[action] = sprite;
        }
        return sprite;
    }
}