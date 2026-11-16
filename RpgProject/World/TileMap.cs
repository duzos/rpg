using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SummitKit;
using SummitKit.Physics;
using SummitKit.UI;
using System;

namespace RpgProject.World;

public class Tilemap : IUpdating
{
    private readonly Tile[] _palette;
    private readonly int[,] _cells;

    public int TileSize { get; }
    public int Rows => _cells.GetLength(0);
    public int Columns => _cells.GetLength(1);
    public Rectangle Bounds => new(0, 0, Columns * TileSize, Rows * TileSize);

    public Tilemap(Tile[] palette, int[,] cells, int tileSize)
    {
        _palette = palette;
        _cells = cells;
        TileSize = tileSize;

        foreach (var tile in _palette)
        {
            tile.Sprite.Shadow = null;
            tile.Sprite.Origin = Vector2.Zero;
        }
    }

    public Tile? GetTile(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Columns || y >= Rows) return null;
        int id = _cells[y, x];
        return id < 0 ? null : _palette[id];
    }

    public void SetTile(int x, int y, int id)
    {
        if (x < 0 || y < 0 || x >= Columns || y >= Rows) return;
        _cells[y, x] = id;
    }

    public Point WorldToTile(Vector2 world) =>
        new((int)MathF.Floor(world.X / TileSize), (int)MathF.Floor(world.Y / TileSize));

    public Vector2 TileToWorld(int x, int y) => new(x * TileSize, y * TileSize);

    public void Update(GameTime time)
    {
        foreach (var tile in _palette) tile.Sprite.Update(time);
    }

    public void Draw(SpriteBatch batch, Rectangle visible)
    {
        int x0 = Math.Max(0, (int)MathF.Floor((float)visible.Left / TileSize));
        int y0 = Math.Max(0, (int)MathF.Floor((float)visible.Top / TileSize));
        int x1 = Math.Min(Columns - 1, visible.Right / TileSize);
        int y1 = Math.Min(Rows - 1, visible.Bottom / TileSize);

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                GetTile(x, y)?.Sprite.Draw(batch, TileToWorld(x, y));

        if (!IUIElement.Debug) return;

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (GetTile(x, y)?.Solid == true)
                    batch.Draw(Core.Pixel, new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize), Color.Blue * 0.4f);

        var drawn = new Rectangle(x0 * TileSize, y0 * TileSize, (x1 - x0 + 1) * TileSize, (y1 - y0 + 1) * TileSize);
        DrawOutline(batch, drawn, Color.Yellow);
        DrawOutline(batch, visible, Color.Lime);
    }

    private static void DrawOutline(SpriteBatch batch, Rectangle r, Color colour, int t = 1)
    {
        batch.Draw(Core.Pixel, new Rectangle(r.X, r.Y, r.Width, t), colour);
        batch.Draw(Core.Pixel, new Rectangle(r.X, r.Bottom - t, r.Width, t), colour);
        batch.Draw(Core.Pixel, new Rectangle(r.X, r.Y, t, r.Height), colour);
        batch.Draw(Core.Pixel, new Rectangle(r.Right - t, r.Y, t, r.Height), colour);
    }

    public bool IsSolid(Rectangle area)
    {
        if (!Bounds.Contains(area)) return true;

        int x0 = area.Left / TileSize;
        int y0 = area.Top / TileSize;
        int x1 = (area.Right - 1) / TileSize;
        int y1 = (area.Bottom - 1) / TileSize;

        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                if (GetTile(x, y)?.Solid == true) return true;

        return false;
    }
}