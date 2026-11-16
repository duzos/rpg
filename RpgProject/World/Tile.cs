using SummitKit.Graphics;
using SummitKit.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpgProject.World;

public sealed class Tile(string name, Sprite sprite, bool solid = false)
{
    public string Name { get; } = name;
    public Sprite Sprite { get; } = sprite;
    public bool Solid { get; } = solid; 
}