using Microsoft.Xna.Framework;

namespace RpgProject.World;

public interface ISolid
{
    Rectangle CollisionBox { get; }
}
