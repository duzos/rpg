using RpgProject.Battle;
using RpgProject.Dialogue;
using RpgProject.Player;
using SummitKit.Graphics;
using SummitKit.Util;
using System;

namespace RpgProject.Npcs;

public sealed class Banana(Sprite sprite) : Npc(sprite), IBattleStats
{
    public int Health { get; set; } = 20;
    public int MaxHealth { get; set; } = 20;
    public int Attack { get; set; } = 1;
    public override void OnInteract(Character player) {
        var battle = new BattleState([player], [this]);
        battle.Start();
        TextBox.Queue("potassium.");
        Scheduler.Delay(() => battle.End(), TimeSpan.FromSeconds(5));
    }
}
