using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using SummitKit;
using System;

namespace RpgProject.Battle;

public sealed class BattleMusic
{
    public static BattleMusic Instance { get; } = new();

    public TimeSpan FadeDuration { get; set; } = TimeSpan.FromSeconds(0.5);

    private SoundEffectInstance? _battle;
    private float _battleLevel;
    private float _battleTarget;
    private float _overworldLevel = 1f;
    private float _overworldTarget = 1f;
    private bool _active;

    public void Start(SoundEffect track)
    {
        _active = true;

        if (_battle is null || _battle.IsDisposed)
        {
            _battle = track.CreateInstance();
            _battle.IsLooped = true;
            _battle.Volume = 0f;
            _battle.Play();
            _battleLevel = 0f;
        }

        _battleTarget = 1f;
        _overworldTarget = 0f;
    }

    public void End()
    {
        if (!_active) return;
        _active = false;

        _battleTarget = 0f;
        _overworldTarget = 1f;
        if (MediaPlayer.State == MediaState.Paused) MediaPlayer.Resume();
    }

    public void Update(GameTime time)
    {
        float step = FadeDuration.TotalSeconds <= 0 ? 1f : (float)(time.ElapsedGameTime.TotalSeconds / FadeDuration.TotalSeconds);

        if (_battle is not null)
        {
            _battleLevel = MoveTowards(_battleLevel, _battleTarget, step);
            _battle.Volume = Math.Clamp(_battleLevel * Core.Audio.MusicVolume, 0f, 1f);

            if (_battleLevel <= 0f && _battleTarget <= 0f)
            {
                _battle.Stop();
                _battle.Dispose();
                _battle = null;
            }
        }

        if (_overworldLevel != _overworldTarget)
        {
            _overworldLevel = MoveTowards(_overworldLevel, _overworldTarget, step);
            Core.Audio.MusicLevel = _overworldLevel;

            if (_overworldLevel <= 0f && _active && MediaPlayer.State == MediaState.Playing)
                MediaPlayer.Pause();
        }
    }

    private static float MoveTowards(float value, float target, float step) =>
        value < target ? Math.Min(target, value + step) : Math.Max(target, value - step);
}
