using Microsoft.Xna.Framework;
using RpgProject.Player;
using RpgProject.World;
using SummitKit;
using SummitKit.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using SummitKit.Util;

namespace RpgProject.Battle;

public sealed class BattleState(IEnumerable<IBattleStats> playerTeam, IEnumerable<IBattleStats> enemies, string musicId = "rudebuster_boss") : IUpdating {
    public static BattleState? Current = null;
    public static bool InBattle => Current != null;

    public static readonly Vector2[] PlayerTeamPositions =
    [
        new(-100, -28),
        new(-100, 0),
        new(-100, 28),
    ];

    public static readonly Vector2[] EnemiesPositions =
    [
        new(100, -28),
        new(100, 0),
        new(100, 28),
    ];

    public IBattleStats[] PlayerTeam { get; private set; } = Fill(playerTeam, PlayerTeamPositions.Length, "player team");
    public IBattleStats[] Enemies { get; private set; } = Fill(enemies, EnemiesPositions.Length, "enemy team");

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

    public void Start(TimeSpan? moveDuration = null)
    {
        if (Current is not null && Current != this)
            throw new InvalidOperationException("A battle is already in progress.");

        var duration = moveDuration ?? TimeSpan.FromSeconds(0.5);

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
        Centre = targets.Count == 0 ? Anchor : targets.Aggregate(Vector2.Zero, (sum, t) => sum + t) / targets.Count;
        Core.Camera.Bounds = Core.GraphicsDevice.Viewport.Bounds;
        Core.Camera.MoveTo(new FollowTarget(() => Centre, () => Core.Camera.Position, p => Core.Camera.Position = p));

        MoveIntoPlace(PlayerTeam, PlayerTeamPositions, duration);
        MoveIntoPlace(Enemies, EnemiesPositions, duration);

        foreach (var character in PlayerTeam.OfType<Character>()) character.SetFacing(Direction.East);
        foreach (var character in Enemies.OfType<Character>()) character.SetFacing(Direction.West);

        if (MainGame.Sounds.TryGetValue(BattleMusic, out var track)) Battle.BattleMusic.Instance.Start(track);
        BattleBackground.Instance.Show();

        Current = this;

        foreach (var combatant in All) combatant.OnBattleStart(this);
    }


    private void MoveIntoPlace(IBattleStats[] team, Vector2[] offsets, TimeSpan duration)
    {
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
        foreach (var bullet in Core.Entities.Entities.OfType<Bullet>().ToList()) bullet.Remove();

        if (Current == this) Current = null;
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

    public void Update(GameTime deltaTime)
    {
        if (All.Any(b => b.Health <= 0))
        {
            End();
            return;
        }
    }
}
