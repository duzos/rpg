using RpgProject.Battle;
using SummitKit.Command;

namespace RpgProject.Player;

public sealed class StatsCommand : ICommand
{
    public string Name => "stats";
    public string? Description => "Show or change the player's battle stats.";
    public string Usage => "stats | stats maxhealth <value> | stats attack <value>";

    public void Execute(CommandContext context, string[] args)
    {
        if (MainGame.Player is not IBattleStats stats)
        {
            context.Error("The player has no battle stats.");
            return;
        }

        if (args.Length == 0)
        {
            context.Reply($"Health {stats.Health}/{stats.MaxHealth}, attack {stats.Attack}");
            return;
        }

        if (args.Length != 2 || !int.TryParse(args[1], out int value) || value < 1)
        {
            context.Error("Usage: " + Usage + " (value must be a whole number of at least 1)");
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "maxhealth":
                stats.MaxHealth = value;
                stats.Health = value;
                context.Success($"Max health set to {value} and health refilled.");
                break;
            case "attack":
                stats.Attack = value;
                context.Success($"Attack set to {value}.");
                break;
            default:
                context.Error($"Unknown stat '{args[0]}'. Usage: {Usage}");
                break;
        }
    }
}
