using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;
using SummitKit;
using System;

namespace RpgProject.Battle;

public sealed class BattleMusic
{
    public static BattleMusic Instance { get; } = new();

    public TimeSpan FadeDuration { get; set; } = TimeSpan.FromSeconds(0.5);
    public bool Active { get; private set; }

    private Song? _overworld;
    private TimeSpan _overworldPosition;
    private bool _overworldRepeating;

    private float _level = 1f;
    private float _target = 1f;
    private Action? _onSilent;

    public void Start(Song track)
    {
        if (Active) return;
        Active = true;

        FadeOutThen(() =>
        {
            _overworld = MediaPlayer.Queue.ActiveSong;
            _overworldPosition = MediaPlayer.PlayPosition;
            _overworldRepeating = MediaPlayer.IsRepeating;

            Core.Audio.PlayMusic(track, true);
            _target = 1f;
        });
    }

    public void End()
    {
        if (!Active) return;
        Active = false;

        FadeOutThen(() =>
        {
            MediaPlayer.Stop();
            if (_overworld is not null)
            {
                MediaPlayer.Play(_overworld, _overworldPosition);
                MediaPlayer.IsRepeating = _overworldRepeating;
            }

            _overworld = null;
            _target = 1f;
        });
    }

    private void FadeOutThen(Action onSilent)
    {
        _onSilent = onSilent;
        _target = 0f;
    }

    public void Update(GameTime time)
    {
        if (_level == _target && _onSilent is null) return;

        float step = FadeDuration.TotalSeconds <= 0 ? 1f : (float)(time.ElapsedGameTime.TotalSeconds / FadeDuration.TotalSeconds);
        _level = _level < _target ? Math.Min(_target, _level + step) : Math.Max(_target, _level - step);
        Core.Audio.MusicLevel = _level;

        if (_level <= 0f && _onSilent is not null)
        {
            var action = _onSilent;
            _onSilent = null;
            action();
        }
    }
}
