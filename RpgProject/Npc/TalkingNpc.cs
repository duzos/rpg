using Microsoft.Xna.Framework.Audio;
using RpgProject.Dialogue;
using RpgProject.Player;
using RpgProject.World;
using SummitKit.Graphics;
using System.Collections.Generic;

namespace RpgProject.Npcs;

public class TalkingNpc(Room room, Sprite sprite, IReadOnlyList<string> lines, SoundEffect? sound) : Npc(sprite)
{
    protected Room Room { get; } = room;
    public IReadOnlyList<string> Lines { get; } = lines;
    public SoundEffect? Sound { get; } = sound;

    public override void OnInteract(Character player)
    {
        TextBox.Say(Lines, Sound, OnConversationEnd);
    }

    protected virtual void OnConversationEnd() { }
}
