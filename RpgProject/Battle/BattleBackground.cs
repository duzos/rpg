using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RpgProject.Audio;
using SummitKit;
using SummitKit.Physics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpgProject.Battle;

public sealed class BattleBackground : IUpdating
{
    public static BattleBackground Instance { get; } = new();

    public TimeSpan FadeDuration { get; set; } = TimeSpan.FromSeconds(0.5);
    public float Opacity { get; private set; }

    public int AccentBeat { get; set; } = 3;
    public Color FallbackColour { get; set; } = new(150, 50, 200);
    public float BackgroundPulse { get; set; } = 0.15f;
    public float BeatDecay { get; set; } = 5f;
    public float BeatPulse { get; private set; }
    public Color ThemeColour => MainGame.Player is IBattleStats stats ? stats.HealthBarColour : FallbackColour;

    public float RingStep { get; set; } = 40f;
    public float RingLifetimeBeats { get; set; } = 4f;
    public float StepEase { get; set; } = 3f;
    public int StartRadius { get; set; } = 6;
    public float FadeIn { get; set; } = 0.1f;
    public float GlowStrength { get; set; } = 0.25f;

    public float RingBrightness { get; set; } = 0.45f;
    public int RingThickness { get; set; } = 1;
    public float EnemyRingBrightness { get; set; } = 0.5f;

    public float AccentWhiten { get; set; } = 0.4f;
    public float AccentSpeedMultiplier { get; set; } = 1.4f;
    public int AccentThickness { get; set; } = 3;
    public float AccentEchoDelay { get; set; } = 0.08f;

    private enum RingKind { Beat, Accent }

    private sealed class Ring(float age, RingKind kind, Vector2 centre, float beatLength, Color colour, float brightness = 1f)
    {
        public readonly Color Colour = colour * brightness;
        public float Age = age;
        public readonly float BeatLength = beatLength;
        public readonly RingKind Kind = kind;
        public readonly Vector2 Centre = centre;
    }

    private readonly List<Ring> _rings = [];
    private readonly List<(Func<Vector2> Centre, Color Colour, float Brightness)> _queued = [];
    private int _lastBeat = int.MinValue;
    private float _targetOpacity;

    public void Show() => _targetOpacity = 1f;
    public void Hide() => _targetOpacity = 0f;

    public void Update(GameTime time)
    {
        float dt = (float)time.ElapsedGameTime.TotalSeconds;
        if (Opacity <= 0f && _targetOpacity <= 0f)
        {
            _rings.Clear();
            _queued.Clear();
            _lastBeat = int.MinValue;
            return;
        }

        float step = FadeDuration.TotalSeconds <= 0 ? 1f : dt / (float)FadeDuration.TotalSeconds;
        Opacity = _targetOpacity > Opacity
            ? Math.Min(_targetOpacity, Opacity + step)
            : Math.Max(_targetOpacity, Opacity - step);

        foreach (var ring in _rings) ring.Age += dt;
        _rings.RemoveAll(r => r.Age >= r.BeatLength * RingLifetimeBeats);

        SpawnOnBeat();
        BeatPulse = CalculatePulse();
    }

    private float CalculatePulse()
    {
        BPMSong? song = BPMSong.Current;
        if (song is null || song.Beats < 0) return 0f;

        float strength = song.BeatInBar == AccentBeat ? 1f : 0.5f;
        return strength * MathF.Exp(-(float)song.BeatProgress * BeatDecay);
    }

    public void QueueRing(Func<Vector2> centre, Color colour, float brightness = 1f) =>
        _queued.Add((centre, colour, brightness));

    public void SpawnRing(Vector2 centre, Color colour, float brightness = 1f, bool accent = false)
    {
        float beatLength = BPMSong.Current is { } song ? (float)song.BeatLength.TotalSeconds : 0.5f;
        _rings.Add(new Ring(0f, accent ? RingKind.Accent : RingKind.Beat, centre, beatLength, colour, brightness));
    }

    private void SpawnOnBeat()
    {
        BPMSong? song = BPMSong.Current;
        if (song is null || song.Beats < 0)
        {
            _lastBeat = int.MinValue;
            return;
        }

        int beat = song.CurrentBeat;
        if (beat == _lastBeat) return;
        _lastBeat = beat;

        float age = (float)(song.BeatProgress * song.BeatLength.TotalSeconds);
        var kind = song.BeatInBar == AccentBeat ? RingKind.Accent : RingKind.Beat;
        float beatLength = (float)song.BeatLength.TotalSeconds;
        _rings.Add(new Ring(age, kind, MainGame.Player.AABB.Center.ToVector2(), beatLength, ThemeColour));

        if (kind == RingKind.Accent && BattleState.Current is { } battle)
        {
            var living = battle.Enemies.Where(e => e.Health > 0).OfType<Entity>().ToArray();
            if (living.Length > 0)
            {
                var source = living[Random.Shared.Next(living.Length)];
                var colour = ((IBattleStats)source).HealthBarColour;
                _rings.Add(new Ring(age, kind, source.AABB.Center.ToVector2(), beatLength, colour, EnemyRingBrightness));
            }
        }

        foreach (var (centre, colour, brightness) in _queued)
            _rings.Add(new Ring(age, kind, centre(), beatLength, colour, brightness));
        _queued.Clear();
    }

    public void Draw(SpriteBatch batch, Rectangle view)
    {
        if (Opacity <= 0f) return;

        var theme = ThemeColour;
        batch.Draw(Core.Pixel, view, Color.Lerp(Color.Black, theme, BeatPulse * BackgroundPulse) * Opacity);

        foreach (var ring in _rings)
        {
            float progress = ring.Age / (ring.BeatLength * RingLifetimeBeats);
            float life = 1f - progress;
            float appear = FadeIn <= 0f ? 1f : MathF.Min(1f, progress / FadeIn);

            if (ring.Kind == RingKind.Beat)
            {
                int radius = Radius(ring.Age, ring.BeatLength, RingStep);
                DrawRing(batch, view, ring.Centre, radius, RingThickness, ring.Colour * (life * appear * RingBrightness * Opacity));
                continue;
            }

            var accentColour = Color.Lerp(ring.Colour, Color.White * (ring.Colour.A / 255f), AccentWhiten);
            float step = RingStep * AccentSpeedMultiplier;
            float fade = life * life * appear;
            DrawRing(batch, view, ring.Centre, Radius(ring.Age, ring.BeatLength, step), AccentThickness, accentColour * (fade * Opacity));

            float echoAge = ring.Age - AccentEchoDelay;
            if (echoAge > 0f)
                DrawRing(batch, view, ring.Centre, Radius(echoAge, ring.BeatLength, step), 1, accentColour * (fade * 0.5f * Opacity));
        }
    }

    private int Radius(float age, float beatLength, float step)
    {
        float beats = beatLength <= 0f ? 0f : age / beatLength;
        float whole = MathF.Floor(beats);
        float eased = 1f - MathF.Pow(1f - (beats - whole), StepEase);
        return StartRadius + (int)MathF.Round(step * (whole + eased));
    }

    private void DrawRing(SpriteBatch batch, Rectangle view, Vector2 centre, int radius, int thickness, Color colour)
    {
        if (colour.A == 0) return;
        DrawAnnulus(batch, view, centre, radius + 1, thickness + 2, colour * GlowStrength);
        DrawAnnulus(batch, view, centre, radius, thickness, colour);
    }

    private static void DrawAnnulus(SpriteBatch batch, Rectangle view, Vector2 centre, float outer, float thickness, Color colour)
    {
        if (outer < 0.5f) return;
        float inner = MathF.Max(0f, outer - thickness);
        int cx = (int)MathF.Round(centre.X);
        int cy = (int)MathF.Round(centre.Y);
        int reach = (int)MathF.Ceiling(outer);

        for (int dy = -reach; dy <= reach; dy++)
        {
            int y = cy + dy;
            if (y < view.Top - 1 || y > view.Bottom + 1) continue;

            float d2 = dy * dy;
            if (d2 > outer * outer) continue;

            int outerX = (int)MathF.Round(MathF.Sqrt(outer * outer - d2));
            int innerX = d2 < inner * inner ? (int)MathF.Round(MathF.Sqrt(inner * inner - d2)) : 0;

            if (innerX == 0)
            {
                batch.Draw(Core.Pixel, new Rectangle(cx - outerX, y, outerX * 2 + 1, 1), colour);
                continue;
            }

            int width = Math.Max(1, outerX - innerX);
            batch.Draw(Core.Pixel, new Rectangle(cx - outerX, y, width, 1), colour);
            batch.Draw(Core.Pixel, new Rectangle(cx + outerX - width + 1, y, width, 1), colour);
        }
    }
}
