using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SummitKit;
using SummitKit.Graphics;
using SummitKit.Physics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RpgProject.World;

public sealed class Tile(string name, Sprite[] sprites, bool solid = false, int priority = 0)
{
    public string Name { get; } = name;
    public int Priority { get; } = priority;
    public Sprite Sprite { get; } = sprites.Length == 9 ? sprites[4] : sprites[0];
    public bool Solid { get; } = solid;
    public Sprite[] Sprites { get; } = sprites;
    public Sprite[][] SpriteVariants { get; private set; } = [];
    public float VariantChance { get; private set; }
    public bool IsAutotile => Sprites.Length == 9;

    public Sprite PickSprite(int index, uint hash)
    {
        var sprite = Sprites[index];
        if (index >= SpriteVariants.Length || SpriteVariants[index].Length == 0) return sprite;
        if ((hash & 0xFFFF) / 65536f >= VariantChance) return sprite;

        var options = SpriteVariants[index];
        return options[(hash >> 16) % (uint)options.Length];
    }

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
            string? priorityValue = tile.Attribute("priority")?.Value;
            int priority = 0;
            if (priorityValue is not null && !int.TryParse(priorityValue, out priority))
                throw new FormatException($"Tile {id}: priority '{priorityValue}' is not a number.");

            if (name == null || regionId == null) continue;

            var region = atlas.GetRegion(regionId);
            Sprite[] sprites;

            if (tile.Attribute("autotile") is not null) // todo actually read the value
            {
                sprites = new Sprite[9];

                var source = region.SourceRectangle;
                var xThirds = source.Width / 3;
                var yThirds = source.Height / 3;

                for (int y = 0; y < 3; y++)
                {
                    for (int x = 0; x < 3; x++)
                    {
                        var subRegion = new TextureRegion(atlas.Texture, source.X + x * xThirds, source.Y + y * yThirds, xThirds, yThirds);
                        var subSprite = new Sprite(subRegion)
                        {
                            Shadow = null,
                            Origin = Vector2.Zero
                        };
                        sprites[y * 3 + x] = subSprite;
                    }
                }
            }
            else
            {
                sprites = [new Sprite(region) { Shadow = null, Origin = Vector2.Zero }];
            }

            Sprite[][] variantArray = new Sprite[sprites.Length][];
            for (int i = 0; i < variantArray.Length; i++) variantArray[i] = [];
            float variantChance = 0f;

            var variants = tile.Attribute("variants");
            if (variants is not null)
            {
                var variantsSource = atlas.GetRegion(variants.Value).SourceRectangle;
                var pieceSource = sprites[0].Region.SourceRectangle;
                int pieceWidth = pieceSource.Width;
                int pieceHeight = pieceSource.Height;

                if (variantsSource.Height != pieceHeight || variantsSource.Width % pieceWidth != 0)
                    throw new InvalidDataException($"Tile {id}: variants region '{variants.Value}' must be a row of {pieceWidth}x{pieceHeight} pieces.");

                int variantCount = variantsSource.Width / pieceWidth;
                var variantSprites = new Sprite[variantCount];
                for (int i = 0; i < variantCount; i++)
                {
                    var subRegion = new TextureRegion(atlas.Texture, variantsSource.X + i * pieceWidth, variantsSource.Y, pieceWidth, pieceHeight);
                    variantSprites[i] = new Sprite(subRegion) { Shadow = null, Origin = Vector2.Zero };
                }

                variantArray[sprites.Length == 9 ? 4 : 0] = variantSprites;

                string? chanceValue = tile.Attribute("variantChance")?.Value;
                if (chanceValue is not null && (!float.TryParse(chanceValue, NumberStyles.Float, CultureInfo.InvariantCulture, out variantChance) || variantChance < 0f || variantChance > 1f))
                    throw new FormatException($"Tile {id}: variantChance '{chanceValue}' must be a number from 0 to 1.");
            }

            result.Add(id, new Tile(name, sprites, solid, priority) { SpriteVariants = variantArray, VariantChance = variantChance });
        }

        Tile[] output = new Tile[result.Keys.Max() + 1];

        foreach (var kvp in result)
        {
            output[kvp.Key] = kvp.Value;
        }

        return output;
    }
}