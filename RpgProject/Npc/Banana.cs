using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RpgProject.Battle;
using RpgProject.Dialogue;
using RpgProject.Player;
using SummitKit;
using SummitKit.Graphics;
using SummitKit.Util;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RpgProject.Npcs;

public sealed class Banana(Sprite sprite) : Npc(sprite), IBattleStats
{
    public int Health { get; set; } = 5;
    public int MaxHealth { get; set; } = 5;
    public int Attack { get; set; } = 1;
    public Color HealthBarColour { get; set; } = Color.Yellow;

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

        var battle = new BattleState([player], [this, nana2, nana3], "black_knife");
        battle.Start();
        TextBox.Queue([("potassium.", Sprite, MainGame.Sounds["snd_text"]), "let's do this."]);


        Scheduler.Repeat((task) => {
            if (BattleState.Current != battle)
            {
                task.Cancelled = true;
                return;
            }

            if (BattleState.Current.Phase != BattlePhase.EnemyAttack) return;

            var shooters = battle.Enemies.OfType<Banana>().Where(b => b.Health > 0).ToList();
            for (int i = 0; i < shooters.Count; i++)
            {
                var shooter = shooters[i];
                bool throwLob = Random.Shared.Next(2) == 0;
                Scheduler.Delay(() =>
                {
                    if (BattleState.Current != battle || battle.Phase != BattlePhase.EnemyAttack) return;
                    shooter.Fire(throwLob);
                }, TimeSpan.FromSeconds(0.3 * i));
            }

        }, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2.5F));
    }

    private void Fire(bool lob)
    {
        Bullet bullet = lob
            ? new ArcBullet(Sprite.Clone()) { Attack = 10, Target = Character.PlayerFeetCentre }
            : new AimedBullet(Sprite.Clone()) { Attack = 5 };
        bullet.Spawn(AABB.Center.ToVector2());
    }

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
