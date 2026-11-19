using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework;
using SummitKit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RpgProject.Audio;

public sealed class BPMSong(string id, double bpm)
{
    public static readonly Dictionary<string, BPMSong> Lookup = new()
    {
        ["tv_world"] = new BPMSong("tv_world", 145) { Offset = TimeSpan.FromSeconds(0.18), DownbeatOffset = 0 },
    };

    public static BPMSong? Current =>
        Lookup.Values.FirstOrDefault(b => b.Song == MediaPlayer.Queue.ActiveSong);
    public string SongId { get; } = id;
    public double Bpm { get; } = bpm;
    public Song Song => MainGame.Music[SongId];
    public TimeSpan Offset { get; init; } = TimeSpan.Zero;
    public int DownbeatOffset { get; set; }
    public int BeatsPerBar { get; set; } = 4;

    public TimeSpan BeatLength => TimeSpan.FromMinutes(1.0 / Bpm);

    public bool IsPlaying => MediaPlayer.State == MediaState.Playing && MediaPlayer.Queue.ActiveSong == Song;
    public TimeSpan Latency { get; init; } = TimeSpan.Zero;
    public float Correction { get; init; } = 0.1f;
    public TimeSpan Position => IsPlaying ? _clock - Latency : TimeSpan.Zero;

    private TimeSpan _clock;
    private TimeSpan _lastReported;
    private bool _tracking;

    private static readonly TimeSpan SnapThreshold = TimeSpan.FromSeconds(0.5);

    public static void UpdateAll(GameTime time)
    {
        foreach (var song in Lookup.Values) song.Update(time);
    }

    public void Update(GameTime time)
    {
        if (!IsPlaying)
        {
            _tracking = false;
            _nextBeat.Clear();
            _scheduled.Clear();
            _lastBeat = int.MinValue;
            return;
        }

        var reported = MediaPlayer.PlayPosition;

        if (!_tracking || (reported - _clock).Duration() > SnapThreshold)
        {
            _clock = reported;
            _lastReported = reported;
            _tracking = true;
        }
        else
        {
            _clock += time.ElapsedGameTime;

            if (reported != _lastReported)
            {
                _clock += (reported - _clock) * Correction;
                _lastReported = reported;
            }
        }

        CheckScheduled();
        CheckBeat();
    }

    public event Action<int>? Beat;
    private readonly List<Action> _nextBeat = [];
    private int _lastBeat = int.MinValue;

    private readonly List<(double Beat, Action Action)> _scheduled = [];

    public void OnNextBeat(Action action) => _nextBeat.Add(action);

    public void At(double beat, Action action) => _scheduled.Add((beat, action));

    public void InBeats(double beatsFromNow, Action action) => At(CurrentBeat + beatsFromNow, action);

    public void ClearScheduled() => _scheduled.Clear();

    private void CheckScheduled()
    {
        if (_scheduled.Count == 0) return;

        double now = Beats;
        var due = _scheduled.Where(e => e.Beat <= now).OrderBy(e => e.Beat).ToArray();
        if (due.Length == 0) return;

        _scheduled.RemoveAll(e => e.Beat <= now);
        foreach (var (_, action) in due) action();
    }

    public void OnBeats(int count, Action action)
    {
        if (count <= 1) OnNextBeat(action);
        else OnNextBeat(() => OnBeats(count - 1, action));
    }

    private void CheckBeat()
    {
        if (Beats < 0) return;

        int beat = CurrentBeat;
        if (beat == _lastBeat) return;
        _lastBeat = beat;

        var due = _nextBeat.ToArray();
        _nextBeat.Clear();
        foreach (var action in due) action();
        Beat?.Invoke(beat);
    }

    public double Beats => (Position - Offset) / BeatLength;
    public int CurrentBeat => (int)Math.Floor(Beats);
    public int BeatInBar => (((CurrentBeat + DownbeatOffset) % BeatsPerBar) + BeatsPerBar) % BeatsPerBar;
    public bool IsDownbeat => BeatInBar == 0;
    public double BeatProgress => Beats - Math.Floor(Beats);
    public TimeSpan TimeToNextBeat => BeatLength * (1 - BeatProgress);
    public TimeSpan TimeOfBeat(int beat) => Offset + BeatLength * beat;
}
