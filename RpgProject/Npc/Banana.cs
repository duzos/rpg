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
            TextBox.Queue("gg");
            return;
        }

        Health = MaxHealth;
        player.Health = player.MaxHealth;

        var battle = new BattleState([player], [this], "black_knife");
        battle.Start();
        TextBox.Queue("potassium.");
    }
}
