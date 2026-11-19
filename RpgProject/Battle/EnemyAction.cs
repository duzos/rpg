using Microsoft.Xna.Framework;
using RpgProject.Audio;
using RpgProject.Dialogue;
using SummitKit.Physics;
using System;
using System.Collections.Generic;

namespace RpgProject.Battle;

public sealed class EnemyAction(double beats)
{
    public double Beats { get; private set; } = beats;
    private readonly List<(double Beat, Action<ActionContext> Action)> _steps = [];

    public EnemyAction At(double beat, Action<ActionContext> action)
    {
        _steps.Add((beat, action));
        return this;
    }

    public EnemyAction Every(double start, double interval, int count, Action<ActionContext> action)
    {
        for (int i = 0; i < count; i++) At(start + interval * i, action);
        return this;
    }

    public EnemyAction Then(EnemyAction next)
    {
        double offset = Beats;
        foreach (var (beat, action) in next._steps) _steps.Add((offset + beat, action));
        Beats += next.Beats;
        return this;
    }

    public void Schedule(ActionContext ctx, double start)
    {
        foreach (var (beat, action) in _steps)
        {
            ctx.Song.At(start + beat, () => { if (ctx.Active) action(ctx); });
        }
    }
}

public sealed class ActionContext(BattleState state, BPMSong song, Entity source)
{
    public BattleState State { get; } = state;
    public BPMSong Song { get; } = song;
    public Entity Source { get; } = source;
    public bool Active => BattleState.Current == State && State.Phase == BattlePhase.EnemyAttack;

    public void Say(IEnumerable<DialogueLine> lines, string region = "textbox") => TextBox.Queue(lines, region);

    public void Ring(Color? colour = null, Func<Vector2>? centre = null, float brightness = 1f)
    {
        colour ??= Source is IBattleStats stats ? stats.HealthBarColour : Color.White;
        centre ??= () => Source.Centre;
        BattleBackground.Instance.QueueRing(centre, colour.Value, brightness);
    }

    public void RingNow(Color? colour = null, float brightness = 1f, bool accent = false)
    {
        var c = colour ?? (Source is IBattleStats stats ? stats.HealthBarColour : Color.White);
        BattleBackground.Instance.SpawnRing(Source.Centre, c, brightness, accent);
    }

    public T Fire<T>(T bullet) where T : Bullet
    {
        bullet.Spawn(Source.Centre);
        return bullet;
    }

    public TimeSpan UntilBeatsFromNow(double beats) => Song.TimeOfBeat(Song.CurrentBeat) + Song.BeatLength * beats - Song.Position;
}
