using Microsoft.Xna.Framework;
using RpgProject.Audio;
using RpgProject.Dialogue;
using RpgProject.Player;
using RpgProject.World;
using SummitKit;
using SummitKit.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SummitKit.Util;
using System.Threading.Tasks;

namespace RpgProject.Battle;

public sealed class BattleState(IEnumerable<IBattleStats> playerTeam, IEnumerable<IBattleStats> enemies, string musicId = "tv_world") : IUpdating {
    public static BattleState? Current = null;
    public static bool InBattle => Current != null;

    private BattlePhase _phase = BattlePhase.Intro;
    private TimeSpan phaseElapsed = TimeSpan.Zero;
    public BattlePhase Phase
    {
        get => _phase;
        private set
        {
            _phase = value;
            phaseElapsed = TimeSpan.Zero;
            OnPhaseStart(value);
        }
    }

    public static readonly Vector2[] PlayerTeamPositions =
    [
        new(-100, -28),
        new(-100, 0),
        new(-100, 28),
    ];

    public static readonly Vector2[] EnemiesPositions =
    [
        new(100, -40),
        new(100, 0),
        new(100, 40),
    ];

    public IBattleStats[] PlayerTeam { get; private set; } = Fill(playerTeam, PlayerTeamPositions.Length, "player team");
    public IBattleStats[] Enemies { get; private set; } = Fill(enemies, EnemiesPositions.Length, "enemy team");
    public IBattleStats[]? Winners => Outcome is null ? null : (Outcome == BattleOutcome.Won ? PlayerTeam : Enemies);
    public BattleOutcome? Outcome { get; private set; }
    public IReadOnlyList<IBattleStats> All => [.. PlayerTeam, .. Enemies];

    public Vector2 Anchor { get; private set; }
    public Vector2 Centre { get; private set; }

    private readonly Dictionary<IBattleStats, Vector2> startingPositions = [];

    public string BattleMusic { get; set; } = musicId;

    private static IBattleStats[] Fill(IEnumerable<IBattleStats> team, int slots, string label)
    {
        var members = team.Where(s => s is not null).ToArray();
        if (members.Length > slots)
            throw new ArgumentException($"The {label} has {members.Length} members but there are only {slots} battle positions.");
        return members;
    }

    public void Advance() => Phase = Phase.Next();
    private void OnPhaseStart(BattlePhase phase)
    {
        switch (phase)
        {
            case BattlePhase.Menu:
                KillBullets();
                MoveIntoPlace(BattlePhase.Intro.Duration() ?? TimeSpan.Zero);
                break;
            case BattlePhase.EnemyAttack:
                StartEnemyActions();
                break;
        }
    }
    public void Start(TimeSpan? moveDuration = null)
    {
        if (Current is not null && Current != this)
            throw new InvalidOperationException("A battle is already in progress.");

        var duration = moveDuration ?? TimeSpan.FromSeconds(0.5);
        Phase = BattlePhase.Intro;

        startingPositions.Clear();
        foreach (var combatant in All)
        {
            if (combatant is IPositioned positioned)
                startingPositions[combatant] = positioned.Position;
        }

        Anchor = PlayerTeam.FirstOrDefault() is IPositioned leader
            ? leader.Position + new Vector2(leader.Width, leader.Height) / 2f
            : Core.Camera.Position;

        var targets = PlayerTeam.Select((_, i) => Anchor + PlayerTeamPositions[i])
            .Concat(Enemies.Select((_, i) => Anchor + EnemiesPositions[i]))
            .ToList();
        Centre = targets.Count == 0
            ? Anchor
            : (targets.Aggregate(Vector2.Min) + targets.Aggregate(Vector2.Max)) / 2f;
        Core.Camera.Bounds = Core.GraphicsDevice.Viewport.Bounds;
        Core.Camera.MoveTo(new FollowTarget(() => Centre, () => Core.Camera.Position, p => Core.Camera.Position = p));

        MoveIntoPlace(duration);


        if (MainGame.Music.TryGetValue(BattleMusic, out var track)) Battle.BattleMusic.Instance.Start(track);
        BattleBackground.Instance.Show();

        Current = this;

        foreach (var combatant in All) combatant.OnBattleStart(this);

    }

    private void MoveIntoPlace(TimeSpan duration)
    {
        MoveIntoPlace(PlayerTeam, PlayerTeamPositions, duration, Direction.East);
        MoveIntoPlace(Enemies, EnemiesPositions, duration, Direction.West);
    }

    private void MoveIntoPlace(IBattleStats[] team, Vector2[] offsets, TimeSpan duration, Direction facing)
    {
        foreach (var character in team.OfType<Character>()) character.SetFacing(facing);

        for (int i = 0; i < team.Length; i++)
        {
            var target = Anchor + offsets[i];

            if (team[i] is Entity entity)
                entity.MoveTo(target, duration, TimeSpan.Zero, centered: true);
            else if (team[i] is IPositioned positioned)
                positioned.Position = target - new Vector2(positioned.Width, positioned.Height) / 2f;
        }
    }

    public void End(TimeSpan? returnDuration = null)
    {
        var duration = returnDuration ?? TimeSpan.FromSeconds(0.5);
        Phase = BattlePhase.Outro;

        foreach (var (combatant, position) in startingPositions)
        {
            if (combatant is Entity entity)
                entity.MoveTo(position, duration, TimeSpan.Zero, centered: false);
            else if (combatant is IPositioned positioned)
                positioned.Position = position;
        }

        startingPositions.Clear();

        Core.Camera.Bounds = RoomManager.Instance.CurrentRoom.Tilemap.Bounds;
        Scheduler.Delay(() => {
            Core.Camera.Follow(MainGame.Player);
        }, duration);
        Battle.BattleMusic.Instance.End();
        BattleBackground.Instance.Hide();

        foreach (var combatant in All) combatant.OnBattleEnd(this);
        KillBullets();
        if (Current == this) Current = null;
    }

    public TimeSpan FallbackAttackLength { get; set; } = TimeSpan.FromSeconds(10);
    public int AttackStagger { get; set; } = 1;

    private BPMSong? attackSong;
    private double? attackEndBeat;

    private void StartEnemyActions()
    {
        attackSong = null;
        attackEndBeat = null;
        if (!BPMSong.Lookup.TryGetValue(BattleMusic, out var song) || !song.IsPlaying) return;

        double start = song.NextBarStart();
        double end = start + song.BeatsPerBar;
        int order = 0;

        foreach (var enemy in Enemies)
        {
            if (enemy.Health <= 0 || enemy is not Entity entity) continue;
            if (enemy.ChooseAction(this) is not { } action) continue;

            double enemyStart = start + order * AttackStagger;
            action.Schedule(new ActionContext(this, song, entity), enemyStart);
            end = Math.Max(end, enemyStart + action.Beats);
            order++;
        }

        attackSong = song;
        attackEndBeat = end;
    }

    private bool EnemyAttackFinished() => attackSong is { } song && attackEndBeat is { } end
        ? song.Beats >= end
        : phaseElapsed >= FallbackAttackLength;

    private void KillBullets()
    {
        foreach (var bullet in Core.Entities.Entities.OfType<Bullet>().ToList()) bullet.Remove();
    }

    public Vector2? StartingPosition(IBattleStats combatant) =>
        startingPositions.TryGetValue(combatant, out var position) ? position : null;

    public void Draw(SpriteBatch batch)
    {
        foreach (var e in All)
        {
            e.DrawHealthBar(batch);
        }


    }

    public void DrawScreen(SpriteBatch batch)
    {
        BattleUI.Instance.Draw(batch);
    }
    public void Update(GameTime deltaTime)
    {
        bool playersDown = PlayerTeam.All(c => c.Health <= 0);
        bool enemiesDown = Enemies.All(c => c.Health <= 0);

        if (playersDown || enemiesDown)
        {
            Outcome = enemiesDown ? BattleOutcome.Won : BattleOutcome.Lost;
            End();
            return;
        }

        if (!TextBox.InDialogue) phaseElapsed += deltaTime.ElapsedGameTime;
        if (Phase.Duration() is { } limit && phaseElapsed >= limit) Advance();
        else if (Phase == BattlePhase.EnemyAttack && EnemyAttackFinished()) Advance();

        BattleUI.Instance.Update(deltaTime);
    }
}

public enum BattleOutcome
{
    Won, Lost
}

public enum BattlePhase
{
    Intro,
    Menu,
    PlayerAction,
    EnemyAttack,
    Outro
}
public static class BattlePhaseExtensions
{
    public static TimeSpan? Duration(this BattlePhase phase) => phase switch
    {
        BattlePhase.Intro => TimeSpan.FromSeconds(0.5),
        BattlePhase.PlayerAction => TimeSpan.FromSeconds(2),
        _ => null,
    };

    public static BattlePhase Next(this BattlePhase phase) => phase switch
    {
        BattlePhase.Intro => BattlePhase.Menu,
        BattlePhase.Menu => BattlePhase.PlayerAction,
        BattlePhase.PlayerAction => BattlePhase.EnemyAttack,
        BattlePhase.EnemyAttack => BattlePhase.Menu,
        _ => phase,
    };
}