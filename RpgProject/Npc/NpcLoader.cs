using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using RpgProject.World;
using SummitKit.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RpgProject.Npcs;

public static class NpcLoader
{
    public static void Load(XElement root, Room room)
    {
        foreach (var element in root.Element("Npcs")?.Elements("Npc") ?? [])
            Add(room, element, CreateNpc(room, element));

        foreach (var element in root.Element("Signs")?.Elements("Sign") ?? [])
            Add(room, element, new Sign(room, ReadLines(room, element)));
    }

    private static void Add(Room room, XElement element, Npc npc)
    {
        var tile = ReadTile(room, element);
        npc.PlaceOnTile(room.Tilemap, tile.X, tile.Y);
        room.Npcs.Add(npc);
    }

    private static Npc CreateNpc(Room room, XElement element)
    {
        string type = element.Attribute("type")?.Value ?? "talker";
        var sound = ReadSound(room, element);
        var lines = ReadLines(room, element, allowEmpty: type == "banana");

        return type switch
        {
            "banana" => new Banana(room, ReadSprite(room, element, "banana"), lines.Count > 0 ? lines : Banana.DefaultLines, sound),
            "talker" => new TalkingNpc(room, ReadSprite(room, element, null), lines, sound),
            _ => throw new InvalidDataException($"Room '{room.Name}': unknown npc type '{type}'."),
        };
    }

    private static Point ReadTile(Room room, XElement element)
    {
        int x = ReadInt(room, element, "x");
        int y = ReadInt(room, element, "y");
        var map = room.Tilemap;
        if (x < 0 || y < 0 || x >= map.Columns || y >= map.Rows)
            throw new InvalidDataException($"Room '{room.Name}': {element.Name} at {x},{y} is outside the map.");
        if (map.GetTile(x, y)?.Solid == true)
            throw new InvalidDataException($"Room '{room.Name}': {element.Name} at {x},{y} is on a solid tile.");
        return new Point(x, y);
    }

    private static int ReadInt(Room room, XElement element, string attribute)
    {
        string? value = element.Attribute(attribute)?.Value;
        if (value is null || !int.TryParse(value, out int result))
            throw new InvalidDataException($"Room '{room.Name}': {element.Name} needs a whole-number '{attribute}'.");
        return result;
    }

    private static List<string> ReadLines(Room room, XElement element, bool allowEmpty = false)
    {
        var lines = element.Elements("Line").Select(l => l.Value.Trim()).Where(l => l.Length > 0).ToList();
        if (lines.Count == 0 && !allowEmpty)
            throw new InvalidDataException($"Room '{room.Name}': {element.Name} at {element.Attribute("x")?.Value},{element.Attribute("y")?.Value} has no <Line> elements.");
        return lines;
    }

    private static SoundEffect? ReadSound(Room room, XElement element)
    {
        string? name = element.Attribute("sound")?.Value;
        if (name is null) return null;
        if (!MainGame.Sounds.TryGetValue(name, out var sound))
            throw new InvalidDataException($"Room '{room.Name}': sound '{name}' is not loaded. Available: {string.Join(", ", MainGame.Sounds.Keys.Order())}");
        return sound;
    }

    private static Sprite ReadSprite(Room room, XElement element, string? defaultName)
    {
        string name = element.Attribute("sprite")?.Value ?? defaultName
            ?? throw new InvalidDataException($"Room '{room.Name}': npc needs a 'sprite' attribute.");

        if (TryRegion(name, out var single)) return new Sprite(single);

        var frames = new List<TextureRegion>();
        while (TryRegion($"{name}-{frames.Count}", out var frame)) frames.Add(frame);
        if (frames.Count == 0)
            throw new InvalidDataException($"Room '{room.Name}': no atlas region '{name}' or '{name}-0'.");

        string? frameTime = element.Attribute("frameTime")?.Value;
        double seconds = frameTime is null ? 0.1 : double.Parse(frameTime, CultureInfo.InvariantCulture);
        return new AnimatedSprite(new Animation(frames, TimeSpan.FromSeconds(seconds)));
    }

    private static bool TryRegion(string name, out TextureRegion region)
    {
        try
        {
            region = MainGame.Atlas.GetRegion(name);
            return true;
        }
        catch (KeyNotFoundException)
        {
            region = null!;
            return false;
        }
    }
}
