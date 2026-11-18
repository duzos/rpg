using Microsoft.Xna.Framework;
using RpgProject.Battle;
using RpgProject.Dialogue;
using RpgProject.Player;
using SummitKit;
using SummitKit.Graphics;
using SummitKit.Util;
using System;
using System.Threading.Tasks;

namespace RpgProject.Npcs;

public sealed class Banana(Sprite sprite) : Npc(sprite), IBattleStats
{
    public int Health { get; set; } = 5;
    public int MaxHealth { get; set; } = 5;
    public int Attack { get; set; } = 1;
    public Color HealthBarColour { get; set; } = Color.Yellow;

    public override void OnInteract(Character player) {
        if (BattleState.Current is not null)
        {
            BattleState.Current.End();
            TextBox.Queue([("gg", Sprite, MainGame.Sounds["snd_text"])]);
            return;
        }

        Health = MaxHealth;
        player.Health = player.MaxHealth;

        var battle = new BattleState([player], [this], "black_knife");
        battle.Start();
        TextBox.Queue([("potassium.", Sprite, MainGame.Sounds["snd_text"]), "let's do this."]);

        Scheduler.Repeat((task) => {
            if (BattleState.Current != battle)
            {
                task.Cancelled = true;
                return;
            }

            var bullet = new AimedBullet(Sprite.Clone())
            {
                Attack = 1
            };
            bullet.Spawn(AABB.Center.ToVector2());
        }, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1.5F));
    }
}
