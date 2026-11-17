using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using SummitKit;
using SummitKit.Audio;
using SummitKit.Graphics;
using SummitKit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RpgProject.World;

public sealed class Room(string name, Dictionary<string, SpawnPoint> spawns, List<Exit> exits, Tilemap tilemap, Song? song) : IDraw {
    public string Name { get; } = name;
    public Dictionary<string, SpawnPoint> Spawns { get; } = spawns;
    public List<Exit> Exits { get; } = exits;
    public Tilemap Tilemap { get; } = tilemap;
    public Song? Song { get; } = song;
    public float LayerDepth { get; set; }

    public void OnLeave() { }
    public void OnEnter(SpawnPoint spawnPoint) {
        if (Song is not null) {
            if (Song == MusicTracker.CurrentMusic) return;

            Core.Audio.Music.FadeInto(Song, TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5), true);
        }
    }
    public void Draw(SpriteBatch spriteBatch)
    {
        Tilemap.Draw(spriteBatch, Core.Camera.VisibleArea);

        if (!IUIElement.Debug) return;

        int ts = Tilemap.TileSize;

        foreach (var exit in Exits)
        {
            var area = new Rectangle(exit.Region.X * ts, exit.Region.Y * ts, exit.Region.Width * ts, exit.Region.Height * ts);
            spriteBatch.Draw(Core.Pixel, area, Color.Magenta * 0.4f);
            if (exit.Facing is Direction facing)
                spriteBatch.Draw(Core.Pixel, FacingEdge(area, facing, 2), Color.Magenta);
        }

        int marker = Math.Max(2, ts / 4);
        foreach (var spawn in Spawns.Values)
        {
            var tile = new Rectangle(spawn.Position.X * ts, spawn.Position.Y * ts, ts, ts);
            spriteBatch.Draw(Core.Pixel, new Rectangle(tile.Center.X - marker / 2, tile.Center.Y - marker / 2, marker, marker), Color.Cyan);
            spriteBatch.Draw(Core.Pixel, FacingEdge(tile, spawn.Facing, 1), Color.Cyan * 0.8f);
        }
    }

    private static Rectangle FacingEdge(Rectangle r, Direction direction, int thickness) => direction switch
    {
        Direction.North => new Rectangle(r.X, r.Y, r.Width, thickness),
        Direction.South => new Rectangle(r.X, r.Bottom - thickness, r.Width, thickness),
        Direction.West => new Rectangle(r.X, r.Y, thickness, r.Height),
        _ => new Rectangle(r.Right - thickness, r.Y, thickness, r.Height),
    };

    public static Dictionary<string, Room> FromFolder(ContentManager content, Tile[] palette, string folderPath = "rooms") {
        string fullFolderPath = Path.Combine(content.RootDirectory, folderPath);
        if (!Directory.Exists(fullFolderPath)) throw new DirectoryNotFoundException($"Folder '{fullFolderPath}' not found.");
        Dictionary<string, Room> rooms = new Dictionary<string, Room>();
        foreach (string file in Directory.GetFiles(fullFolderPath, "*.xml")) {
            string relativeFilePath = Path.Combine(folderPath, Path.GetFileName(file));
            Room room = FromFile(content, palette, relativeFilePath);
            rooms.Add(room.Name, room);
        }
        return rooms;
    }

    public static Room FromFile(ContentManager content, Tile[] palette, string fileName) {
        string filePath = Path.Combine(content.RootDirectory, fileName);

        using Stream stream = TitleContainer.OpenStream(filePath);
        using XmlReader reader = XmlReader.Create(stream);
        XDocument doc = XDocument.Load(reader);
        XElement root = doc.Root;

        string name = root.Attribute("name")?.Value ?? throw new ArgumentException("Missing name"); // todo room manager & auto id
        int tileSize = int.Parse(root.Attribute("tileSize")?.Value ?? throw new ArgumentException("Missing tileSize"));

        var spawnElements = root.Element("Spawns")?.Elements("Spawn");
        Dictionary<string, SpawnPoint> spawns = new Dictionary<string, SpawnPoint>();
        if (spawnElements != null) {
            foreach (var spawn in spawnElements) {
                string spawnName = spawn.Attribute("name")?.Value ?? throw new ArgumentException("Missing spawn name");
                int x = int.Parse(spawn.Attribute("x")?.Value ?? throw new ArgumentException($"Missing x for spawn {spawnName}"));
                int y = int.Parse(spawn.Attribute("y")?.Value ?? throw new ArgumentException($"Missing y for spawn {spawnName}"));
                string facingValue = spawn.Attribute("facing")?.Value ?? nameof(Direction.North);
                if (!Enum.TryParse(facingValue, ignoreCase: true, out Direction facing) || !Enum.IsDefined(facing))
                    throw new FormatException($"Spawn {spawnName}: '{facingValue}' is not a direction.");
                spawns.Add(spawnName, new SpawnPoint(spawnName, new Point(x, y), facing));
            }
        }

        var exitElements = root.Element("Exits")?.Elements("Exit");
        List<Exit> exits = new List<Exit>();
        if (exitElements != null)
        {
            foreach (var exit in exitElements)
            {
                string roomId = exit.Attribute("room")?.Value ?? throw new ArgumentException("Missing room for exit");
                string spawnId = exit.Attribute("spawn")?.Value ?? throw new ArgumentException("Missing spawn for exit");

                int x = int.Parse(exit.Attribute("x")?.Value ?? throw new ArgumentException($"Missing x for exit"));
                int y = int.Parse(exit.Attribute("y")?.Value ?? throw new ArgumentException($"Missing y for exit"));
                int width = int.Parse(exit.Attribute("width")?.Value ?? throw new ArgumentException($"Missing width for exit"));
                int height = int.Parse(exit.Attribute("height")?.Value ?? throw new ArgumentException($"Missing height for exit"));
                string facingValue = exit.Attribute("facing")?.Value ?? nameof(Direction.North);
                if (!Enum.TryParse(facingValue, ignoreCase: true, out Direction facing) || !Enum.IsDefined(facing))
                    throw new FormatException($"Exit: '{facingValue}' is not a direction.");
                
                exits.Add(new Exit(new Rectangle(x, y, width, height), roomId, spawnId, facing));
            }
        }

        var cells = root.Element("Cells");
        string cellsCsv = cells?.Value ?? throw new ArgumentException("Missing cells");

        string[] lines = cellsCsv.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        int rows = lines.Length;
        int cols = lines[0].Split(',').Length;
        int[,] cellArray = new int[rows, cols];

        for (int y = 0; y < rows; y++)
        {
            string[] values = lines[y].Split(',', StringSplitOptions.TrimEntries);
            if (values.Length != cols)
                throw new FormatException($"Row {y} has {values.Length} values, expected {cols}.");

            for (int x = 0; x < cols; x++)
            {
                if (!int.TryParse(values[x], out cellArray[y, x]))
                    throw new FormatException($"Row {y}, column {x}: '{values[x]}' is not a number.");
            
                if (cellArray[y, x] < 0 || cellArray[y, x] >= palette.Length)
                    throw new IndexOutOfRangeException($"Row {y}, column {x}: value {cellArray[y, x]} is out of range for the palette.");
            }
        }


        string? song = root.Attribute("music")?.Value;
        Song? songAsset = null;
        if (song is not null && !MainGame.Music.TryGetValue(song, out songAsset))
            throw new InvalidDataException($"Room '{name}': music '{song}' is not loaded. Available: {string.Join(", ", MainGame.Music.Keys.Order())}");

        return new Room(name, spawns, exits, new Tilemap(palette, cellArray, tileSize), songAsset);
    }
}

public sealed class SpawnPoint(string name, Point position, Direction facing) { 
    public string Name { get; } = name;
    public Point Position { get; } = position;
    public Direction Facing { get; } = facing;
}

public sealed class Exit(Rectangle region, string roomId, string spawnId, Direction? facing)
{
    public Rectangle Region { get; } = region;
    public string RoomId { get; } = roomId;
    public string SpawnId { get; } = spawnId;
    public Direction? Facing { get; } = facing;

    public Room Room => RoomManager.Instance.Rooms.TryGetValue(RoomId, out var room) ? room : throw new InvalidOperationException($"Room '{RoomId}' not found.");
    public SpawnPoint SpawnPoint => Room.Spawns.TryGetValue(SpawnId, out var spawn) ? spawn : throw new InvalidOperationException($"Spawn '{SpawnId}' not found in room '{RoomId}'.");
}