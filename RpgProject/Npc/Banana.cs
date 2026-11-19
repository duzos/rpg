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


        if (!BPMSong.Lookup.TryGetValue(battle.BattleMusic, out var song)) return;

        void OnBeat(int beat)
        {
            if (BattleState.Current != battle)
            {
                song.Beat -= OnBeat;
                return;
            }

            if (battle.Phase != BattlePhase.EnemyAttack || song.BeatInBar != BattleBackground.Instance.AccentBeat) return;

            var nanas = battle.Enemies.OfType<Banana>().Where(b => b.Health > 0).ToArray();
            if (nanas.Length == 0) return;
            var shooter = nanas[Random.Shared.Next(nanas.Length)];

            shooter.Fire(Random.Shared.Next(2) == 0);
            BattleBackground.Instance.QueueRing(() => shooter.AABB.Center.ToVector2(), Color.Red, 0.5F);

        }

        song.Beat += OnBeat;
    }

    private void Fire(bool lob)
    {
        if (!BPMSong.Lookup.TryGetValue(BattleState.Current.BattleMusic, out var song)) return;
        var landsAt = song.TimeOfBeat(song.CurrentBeat + 8);

        Bullet bullet = lob
            ? new ArcBullet(Sprite.Clone()) { Attack = 10, Target = Character.PlayerFeetCentre, Duration = landsAt - song.Position }
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
