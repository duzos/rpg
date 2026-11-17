using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SummitKit;
using System;

namespace RpgProject.Battle;

public sealed class BattleBackground
{
    public static BattleBackground Instance { get; } = new();

    private readonly record struct GridLayer(Color Colour, Vector2 Velocity);

    private static readonly GridLayer[] Layers =
    [
        new(new Color(36, 0, 36), new Vector2(50f / 3f, 50f / 3f)),
        new(new Color(66, 0, 66), new Vector2(-100f / 3f, -100f / 3f)),
    ];

    public int Spacing { get; set; } = 50;
    public int LineThickness { get; set; } = 1;
    public TimeSpan FadeDuration { get; set; } = TimeSpan.FromSeconds(0.5);
    public float Opacity { get; private set; }

    private float _targetOpacity;
    private float _elapsed;

    public void Show() => _targetOpacity = 1f;
    public void Hide() => _targetOpacity = 0f;

    public void Update(GameTime time)
    {
        float dt = (float)time.ElapsedGameTime.TotalSeconds;
        if (Opacity <= 0f && _targetOpacity <= 0f) return;

        _elapsed += dt;
        float step = FadeDuration.TotalSeconds <= 0 ? 1f : dt / (float)FadeDuration.TotalSeconds;
        Opacity = _targetOpacity > Opacity
            ? Math.Min(_targetOpacity, Opacity + step)
            : Math.Max(_targetOpacity, Opacity - step);

        if (Opacity <= 0f) _elapsed = 0f;
    }

    public void Draw(SpriteBatch batch, Rectangle view)
    {
        if (Opacity <= 0f) return;

        batch.Draw(Core.Pixel, view, Color.Black * Opacity);

        foreach (var layer in Layers)
        {
            var colour = layer.Colour * Opacity;
            int offsetX = Wrap((int)MathF.Floor(layer.Velocity.X * _elapsed));
            int offsetY = Wrap((int)MathF.Floor(layer.Velocity.Y * _elapsed));

            for (int x = view.Left + offsetX - Spacing; x < view.Right; x += Spacing)
                batch.Draw(Core.Pixel, new Rectangle(x, view.Top, LineThickness, view.Height), colour);

            for (int y = view.Top + offsetY - Spacing; y < view.Bottom; y += Spacing)
                batch.Draw(Core.Pixel, new Rectangle(view.Left, y, view.Width, LineThickness), colour);
        }
    }

    private int Wrap(int value) => ((value % Spacing) + Spacing) % Spacing;
}
