using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RpgProject.Player;
using SummitKit;
using SummitKit.Graphics;
using SummitKit.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpgProject.World;

public sealed class RoomManager : IDraw, IUpdating
{
    public Dictionary<string, Room> Rooms { get; } = new Dictionary<string, Room>();
    public Room CurrentRoom { get; private set; }

    private Room? _targetRoom;
    public Room? TargetRoom
    {
        get => _targetRoom;
        set
        {
            if (value == null || !Rooms.ContainsValue(value))
            {
                throw new ArgumentException("Target room must be a valid room in the Rooms collection.", nameof(value));
            }
            Travel(value.Name, null);
        }
    }

    public static RoomManager Instance { get; } = new RoomManager();
    public float LayerDepth { get; set; }

    private ITarget<float>? fadeTarget;
    private float opacity = 0f;

    public bool Transitioning => fadeTarget != null || opacity != 0;

    public void LoadContent(ContentManager content, Tile[] palette, string folderPath = "rooms")
    {
        Rooms.Clear();
        var loadedRooms = Room.FromFolder(content, palette, folderPath);
        foreach (var room in loadedRooms)
        {
            Rooms[room.Key] = room.Value;
        }

        Validate();
    }

    private void Validate()
    {
        foreach (var room in Rooms.Values)
        {
            foreach (var exit in room.Exits)
            {
                if (!Rooms.ContainsKey(exit.RoomId))
                {
                    throw new Exception($"Room '{room.Name}' has an exit to non-existent room '{exit.RoomId}'.");
                }
                if (!Rooms[exit.RoomId].Spawns.ContainsKey(exit.SpawnId))
                {
                    throw new Exception($"Room '{room.Name}' has an exit to room '{exit.RoomId}' with non-existent spawn '{exit.SpawnId}'.");
                }
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        CurrentRoom?.Draw(spriteBatch);
    }

    // todo add DrawScreen & DrawWorld to IDraw
    public void DrawScreen(SpriteBatch spriteBatch)
    {
        if (opacity > 0f)
        {
            spriteBatch.Draw(Core.Pixel, Core.GraphicsDevice.Viewport.Bounds, Color.Black * opacity);
        }
    }

    public void Update(GameTime deltaTime)
    {
        fadeTarget?.Update(deltaTime);
        if (fadeTarget?.IsComplete == true)
        {
            fadeTarget.Callback?.Invoke(fadeTarget);
            fadeTarget = null;
        }

        CurrentRoom?.Tilemap.Update(deltaTime);

        if (CurrentRoom == null && Rooms.Count > 0)
        {
            CurrentRoom = Rooms.Values.First();
        }
    }

    public void Travel(Exit exit)
    {
        Travel(exit.RoomId, exit.SpawnId);
    }
    public void Travel(string roomId, string? spawnId)
    {
        if (Transitioning) return;

        if (!Rooms.TryGetValue(roomId, out Room targetRoom)) throw new ArgumentException($"Room '{roomId}' does not exist.", nameof(roomId));
        spawnId ??= targetRoom.Spawns.Keys.FirstOrDefault();
        if (!targetRoom.Spawns.TryGetValue(spawnId, out SpawnPoint spawnPoint)) throw new ArgumentException($"Spawn '{spawnId}' does not exist in room '{roomId}'.", nameof(spawnId));
        _targetRoom = targetRoom;

        fadeTarget = new InterpolatedTarget<float>(1f, opacity, v => opacity = v, TimeSpan.FromSeconds(0.15F), TimeSpan.Zero, MathHelper.Lerp)
        {
            Callback = (target) =>
            {
                if (targetRoom != null)
                {
                    CurrentRoom.OnLeave();
                    CurrentRoom = targetRoom;
                    CurrentRoom.OnEnter(spawnPoint);
                    var player = MainGame.Player;
                    if (player != null) { 
                    
                        if (player is Character chara)
                        {
                            chara.PlaceFeetOnTile(targetRoom.Tilemap, spawnPoint.Position.X, spawnPoint.Position.Y);
                            chara.SetFacing(spawnPoint.Facing);
                        } else
                        {
                            player.Position = new Vector2(spawnPoint.Position.X * targetRoom.Tilemap.TileSize, spawnPoint.Position.Y * targetRoom.Tilemap.TileSize);
                        }
                        Core.Camera.Position = player.Centre;
                    }

                    Core.Camera.Bounds = CurrentRoom.Tilemap.Bounds;

                    _targetRoom = null;
                }

                fadeTarget = new InterpolatedTarget<float>(0f, opacity, v => opacity = v, TimeSpan.FromSeconds(0.15F), TimeSpan.Zero, MathHelper.Lerp);
            }
        };
    }
}