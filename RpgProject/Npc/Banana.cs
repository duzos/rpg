using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using RpgProject.World;
using SummitKit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpgProject.Npcs;

public sealed class Banana(Room room, Sprite sprite, IReadOnlyList<string> lines, SoundEffect? sound)
    : TalkingNpc(room, sprite, lines, sound)
{
    public static readonly string[] DefaultLines =
    [
        "Oh! A visitor!",
        "I'm the town banana. I like to keep moving.",
        "Catch me if you can!",
    ];

    protected override void OnConversationEnd() => MoveSomewhere();

    private void MoveSomewhere()
    {
        var map = Room.Tilemap;
        var player = MainGame.Player.Position;
        var exitTiles = new HashSet<Point>(Room.Exits.SelectMany(e =>
            Enumerable.Range(e.Region.X, e.Region.Width).SelectMany(x =>
                Enumerable.Range(e.Region.Y, e.Region.Height).Select(y => new Point(x, y)))));
        var solids = Room.Npcs.OfType<ISolid>().Where(s => !ReferenceEquals(s, this)).Select(s => s.CollisionBox).ToList();

        var candidates = new List<Point>();
        for (int y = 1; y < map.Rows; y++)
        {
            for (int x = 0; x < map.Columns; x++)
            {
                var point = new Point(x, y);
                if (map.GetTile(x, y)?.Solid != false || map.GetTile(x, y - 1)?.Solid != false) continue;
                if (exitTiles.Contains(point)) continue;
                if (Vector2.Distance(map.TileToWorld(x, y), player) < map.TileSize * 4) continue;
                var cell = new Rectangle(x * map.TileSize, y * map.TileSize, map.TileSize, map.TileSize);
                if (solids.Any(s => s.Intersects(cell))) continue;
                candidates.Add(point);
            }
        }

        if (candidates.Count == 0) return;
        var target = candidates[Random.Shared.Next(candidates.Count)];
        PlaceOnTile(map, target.X, target.Y);
    }
}
