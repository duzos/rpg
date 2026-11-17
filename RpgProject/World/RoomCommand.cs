using SummitKit.Command;
using System;
using System.Linq;

namespace RpgProject.World;

public sealed class RoomCommand : ICommand
{
    public string Name => "room";
    public string? Description => "List, inspect and travel between rooms.";
    public string Usage => "room list | room info [room] | room goto <room> [spawn]";

    public void Execute(CommandContext context, string[] args)
    {
        if (args.Length < 1)
        {
            context.Error("Usage: " + Usage);
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "list":
                List(context);
                break;
            case "info":
                Info(context, args.Length > 1 ? args[1] : null);
                break;
            case "goto":
                Goto(context, args);
                break;
            default:
                context.Error($"Unknown subcommand '{args[0]}'. Usage: {Usage}");
                break;
        }
    }

    private static void List(CommandContext context)
    {
        var manager = RoomManager.Instance;
        if (manager.Rooms.Count == 0)
        {
            context.Error("No rooms loaded.");
            return;
        }

        foreach (var name in manager.Rooms.Keys.Order())
        {
            context.Reply(manager.CurrentRoom?.Name == name ? $"* {name}" : $"  {name}");
        }
    }

    private static void Info(CommandContext context, string? roomName)
    {
        Room? room = roomName is null ? RoomManager.Instance.CurrentRoom : Find(roomName);
        if (room is null)
        {
            context.Error(roomName is null ? "Not in a room." : $"Room '{roomName}' does not exist.");
            return;
        }

        var map = room.Tilemap;
        context.Success($"{room.Name}: {map.Columns}x{map.Rows} tiles, tile size {map.TileSize}");

        context.Reply($"Spawns ({room.Spawns.Count}):");
        foreach (var spawn in room.Spawns.Values)
        {
            context.Reply($"  {spawn.Name} at {spawn.Position.X},{spawn.Position.Y} facing {spawn.Facing}");
        }

        context.Reply($"Exits ({room.Exits.Count}):");
        foreach (var exit in room.Exits)
        {
            var r = exit.Region;
            string facing = exit.Facing is Direction d ? $" facing {d}" : "";
            context.Reply($"  {r.X},{r.Y} {r.Width}x{r.Height} -> {exit.RoomId}/{exit.SpawnId}{facing}");
        }
    }

    private static void Goto(CommandContext context, string[] args)
    {
        if (args.Length < 2)
        {
            context.Error("Usage: room goto <room> [spawn]");
            return;
        }

        var manager = RoomManager.Instance;
        if (manager.Transitioning)
        {
            context.Error("Already travelling.");
            return;
        }

        Room? room = Find(args[1]);
        if (room is null)
        {
            context.Error($"Room '{args[1]}' does not exist. Try 'room list'.");
            return;
        }

        string? spawn = null;
        if (args.Length > 2)
        {
            spawn = room.Spawns.Keys.FirstOrDefault(k => k.Equals(args[2], StringComparison.OrdinalIgnoreCase));
            if (spawn is null)
            {
                context.Error($"Spawn '{args[2]}' does not exist in '{room.Name}'. Spawns: {string.Join(", ", room.Spawns.Keys)}");
                return;
            }
        }
        else if (room.Spawns.Count == 0)
        {
            context.Error($"Room '{room.Name}' has no spawns.");
            return;
        }

        manager.Travel(room.Name, spawn);
        context.Success($"Travelling to {room.Name}" + (spawn is null ? "" : $" ({spawn})"));
    }

    private static Room? Find(string name) =>
        RoomManager.Instance.Rooms.Values.FirstOrDefault(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
