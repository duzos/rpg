using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SummitKit;
using SummitKit.Graphics;
using SummitKit.Physics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RpgProject.World;

public sealed class Tile(string name, Sprite sprite, bool solid = false)
{
    public string Name { get; } = name;
    public Sprite Sprite { get; } = sprite;
    public bool Solid { get; } = solid; 

    public static Tile[] FromFile(ContentManager content, TextureAtlas atlas, string fileName)
    {
        string filePath = Path.Combine(content.RootDirectory, fileName);

        using Stream stream = TitleContainer.OpenStream(filePath);
        using XmlReader reader = XmlReader.Create(stream);
        XDocument doc = XDocument.Load(reader);
        XElement root = doc.Root;
        var tiles = root.Elements("Tile");

        if (tiles == null) return [];

        Dictionary<int, Tile> result = new Dictionary<int, Tile>();

        foreach (var tile in tiles)
        {
            int id = int.Parse(tile.Attribute("id")?.Value ?? throw new ArgumentException("Missing id"));
            string name = tile.Attribute("name")?.Value;
            string regionId = tile.Attribute("region")?.Value;
            bool solid = tile.Attribute("solid")?.Value == "true";

            if (name == null || regionId == null) continue;

            var region = atlas.GetRegion(regionId);
            var sprite = new Sprite(region);

            result.Add(id, new Tile(name, sprite, solid));
        }

        Tile[] output = new Tile[result.Keys.Max() + 1];

        foreach (var kvp in result)
        {
            output[kvp.Key] = kvp.Value;
        }

        return output;
    }
}