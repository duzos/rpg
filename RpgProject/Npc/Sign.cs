using Microsoft.Xna.Framework;
using RpgProject.World;
using SummitKit.Graphics;
using System.Collections.Generic;

namespace RpgProject.Npcs;

public sealed class Sign(Room room, IReadOnlyList<string> lines)
    : TalkingNpc(room, new Sprite(MainGame.Atlas.GetRegion("sign")), lines, null)
{
    public override Rectangle CollisionBox
    {
        get
        {
            var box = AABB;
            int height = box.Height / 4;
            int inset = box.Width / 3;
            return new Rectangle(box.X + inset, box.Bottom - height, box.Width - inset * 2, height);
        }
    }
}
