using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using RpgProject.Audio;
using RpgProject.Battle;
using RpgProject.Dialogue;
using RpgProject.Player;
using SummitKit;
using SummitKit.Graphics;
using SummitKit.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SummitKit.Util.Scheduler;

namespace RpgProject.Npcs;

public sealed class Banana(Sprite sprite) : Npc(sprite), IBattleStats
{
    public int Health { get; set; } = 5;
    public int MaxHealth { get; set; } = 5;
    public int Attack { get; set; } = 1;
    public Color HealthBarColour { get; set; } = Color.Yellow;
    public float MaxInvincibility { get; } = 0f;
    public float Invincibility { get; set; }

    private Banana? Parent;
    public Banana Clone() => new(Sprite.Clone())
    {
        Position = Position,
        Health = Health,
        MaxHealth = MaxHealth,
        Attack = Attack,
        Parent = this
    };

    public override void OnInteract(Character player) {
        if (BattleState.Current is not null)
        {
            return;
        }

        Health = MaxHealth;
        player.Health = player.MaxHealth;

        var nana2 = this.Clone();
        var nana3 = this.Clone();
        nana2.Add();
        nana3.Add();

        var battle = new BattleState([player], [this, nana2, nana3]);
        battle.Start();
        TextBox.Queue([("potassium.", Sprite, MainGame.Sounds["snd_text"]), "let's do this."]);
    }

    public EnemyAction? ChooseAction(BattleState state) => Random.Shared.Next(3) switch
    {
        0 => Volley(),
        1 => Lob(),
        _ => Volley().Then(Lob()),
    };

    private EnemyAction Volley() => new EnemyAction(8)
        .At(0, c => c.RingNow(new Color(230, 0, 38)))
        .Every(1, 2, 4, c => c.Fire(new AimedBullet(Sprite.Clone()) { Attack = 2 }));

    private EnemyAction Lob() => new EnemyAction(8)
        .At(0, c => c.RingNow(new Color(230, 0, 38)))
        .At(1, c => c.Fire(new ArcBullet(Sprite.Clone())
        {
            Attack = 5,
            Target = Character.PlayerFeetCentre,
            Duration = c.UntilBeatsFromNow(4),
        }));

    public void OnBattleEnd(BattleState state)
    {
        if (Parent is not null)
        {
            Remove();
            return;
        }

        if (state.Outcome == BattleOutcome.Lost)
        {
            TextBox.Queue([("gg.", Sprite, MainGame.Sounds["snd_text"]), "better luck next time"]);
        } else
        {
            TextBox.Queue([("fuuuuuuck man", Sprite, MainGame.Sounds["snd_text"]), "fuuuuuuuck its so over pls pls PPLLS rematch pls pls "]);
        }

        Health = MaxHealth;
    }

    public override void Draw(SpriteBatch batch)
    {
        if (Health <= 0) Sprite.Colour = Color.DarkRed;
        else Sprite.Colour = Color.White;

        base.Draw(batch);
    }
}
