using Microsoft.Xna.Framework;
using RpgProject.Dialogue;
using RpgProject.Player;
using RpgProject.World;
using SummitKit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpgProject.Npcs;

public sealed class Banana : Npc
{
    private static readonly string[] Lines =
    [
        "Oh! A visitor!",
        "I'm the town banana. I like to keep moving.",
        "Catch me if you can!",
    ];

    private readonly Room _room;

    public Banana(Room room) : base(CreateSprite())
    {
        _room = room;
    }

    private static Sprite CreateSprite()
    {
        var frames = Enumerable.Range(0, 8).Select(i => MainGame.Atlas.GetRegion($"banana-{i}")).ToList();
        return new AnimatedSprite(new Animation(frames, TimeSpan.FromSeconds(0.1)));
    }

    public override void OnInteract(Character player)
    {
        MainGame.Sounds.TryGetValue("snd_great_shine", out var sound);
        TextBox.Say(Lines, sound, MoveSomewhere);
    }

    private void MoveSomewhere()
    {
        var map = _room.Tilemap;
        var player = MainGame.Player.Position;
        var exitTiles = new HashSet<Point>(_room.Exits.SelectMany(e =>
            Enumerable.Range(e.Region.X, e.Region.Width).SelectMany(x =>
                Enumerable.Range(e.Region.Y, e.Region.Height).Select(y => new Point(x, y)))));

        var candidates = new List<Point>();
        for (int y = 1; y < map.Rows; y++)
        {
            for (int x = 0; x < map.Columns; x++)
            {
                var point = new Point(x, y);
                if (map.GetTile(x, y)?.Solid != false || map.GetTile(x, y - 1)?.Solid != false) continue;
                if (exitTiles.Contains(point)) continue;
                if (Vector2.Distance(map.TileToWorld(x, y), player) < map.TileSize * 4) continue;
                candidates.Add(point);
            }
        }

        if (candidates.Count == 0) return;
        var target = candidates[Random.Shared.Next(candidates.Count)];
        PlaceOnTile(map, target.X, target.Y);
    }
}
