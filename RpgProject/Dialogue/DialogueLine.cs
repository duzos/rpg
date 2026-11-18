using Microsoft.Xna.Framework.Audio;
using SummitKit.Graphics;

namespace RpgProject.Dialogue;

public sealed record DialogueLine(string Text, Sprite? Sprite = null, SoundEffect? Sound = null)
{
    public bool NoSprite { get; init; }
    public bool NoSound { get; init; }

    public static implicit operator DialogueLine(string text) => new(text);
    public static implicit operator DialogueLine((string Text, Sprite Sprite) line) => new(line.Text, line.Sprite);
    public static implicit operator DialogueLine((string Text, SoundEffect Sound) line) => new(line.Text, Sound: line.Sound);
    public static implicit operator DialogueLine((string Text, Sprite Sprite, SoundEffect Sound) line) => new(line.Text, line.Sprite, line.Sound);
}
