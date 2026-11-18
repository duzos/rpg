using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using SummitKit;
using SummitKit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpgProject.Dialogue;

public sealed class TextBox(string content, string region = "textbox", Sprite? talkSprite = null, SoundEffect? talkSfx = null)
{
    private static readonly Dictionary<string, Sprite[]> BoxSpriteLookup = new Dictionary<string, Sprite[]>();
    public static readonly Queue<TextBox> Upcoming = new();

    public static TextBox? Current = null;
    public static bool InDialogue => Current != null;
    public static bool DialogueAtFrameStart { get; private set; }

    public SoundEffect? Sound { get; set; }
    public Action? OnClose { get; set; }
    public static bool SpacePressed => Core.Input.Keyboard.WasKeyJustPressed(Microsoft.Xna.Framework.Input.Keys.Space);

    public string Content { get; private set; } = content;
    private readonly string regionId = region;
    public Sprite[] Sprites => GetTextBoxSprite(regionId);
    public SpriteFont Font { get; private set; } = Core.ConsoleFont;
    public Sprite? TalkSprite { get; private set; } = talkSprite;
    public SoundEffect? TalkSound { get; private set; } = talkSfx;

    public int Scale { get; set; } = 3;
    public int Margin { get; set; } = 24;
    public int Height { get; set; } = 180;
    public int Padding { get; set; } = 8;
    public float TextScale { get; set; } = 3f;
    public Color TextColour { get; set; } = Color.White;

    public Rectangle Bounds
    {
        get
        {
            var viewport = Core.GraphicsDevice.Viewport;
            return new Rectangle(Margin, viewport.Height - Margin - Height, viewport.Width - Margin * 2, Height);
        }
    }

    public void DrawScreen(SpriteBatch batch)
    {
        var bounds = Bounds;
        var sprites = Sprites;
        int cornerWidth = sprites[0].Region.Width * Scale;
        int cornerHeight = sprites[0].Region.Height * Scale;

        DrawFrame(batch, sprites, bounds, cornerWidth, cornerHeight);

        var inner = new Rectangle(
            bounds.X + cornerWidth + Padding,
            bounds.Y + cornerHeight + Padding,
            bounds.Width - (cornerWidth + Padding) * 2,
            bounds.Height - (cornerHeight + Padding) * 2);

        int portraitWidth = TalkSprite is null ? 0 : DrawPortrait(batch, TalkSprite, inner) + PortraitGap;

        if (Font is null || string.IsNullOrEmpty(Content)) return;

        var textArea = new Rectangle(inner.X + portraitWidth, inner.Y, inner.Width - portraitWidth, inner.Height);

        float lineHeight = Font.LineSpacing * TextScale;
        float y = textArea.Y;
        int remaining = VisibleCharacters;
        foreach (var line in Wrap(Font, Content, textArea.Width / TextScale))
        {
            if (remaining <= 0 || y + lineHeight > textArea.Bottom + Padding) break;
            string shown = remaining >= line.Length ? line : line[..remaining];
            batch.DrawString(Font, shown, new Vector2(textArea.X, y), TextColour, 0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
            remaining -= line.Length + 1;
            y += lineHeight;
        }
    }

    public int PortraitGap { get; set; } = 12;

    private static int DrawPortrait(SpriteBatch batch, Sprite sprite, Rectangle inner)
    {
        int side = inner.Height;
        var region = sprite.Region;
        int scale = Math.Max(1, side / Math.Max(region.Width, region.Height));
        var size = new Vector2(region.Width, region.Height) * scale;
        var square = new Rectangle(inner.X, inner.Y, side, side);
        var position = square.Center.ToVector2() - size / 2f;
        position = new Vector2(MathF.Floor(position.X), MathF.Floor(position.Y));
        region.Draw(batch, position, sprite.Colour, 0f, Vector2.Zero, scale, sprite.Effects, 0f);
        return side;
    }

    public float CharactersPerSecond { get; set; } = 20f;
    public int VisibleCharacters => Math.Min((int)_typed, Content.Length);
    public bool IsTyping => VisibleCharacters < Content.Length;

    private float _typed;
    private bool _justOpened;

    public static void Show(TextBox box)
    {
        box._typed = 0f;
        box._justOpened = true;
        Current = box;
        if (box.Sound is not null) Core.Audio.PlaySound(box.Sound);
    }

    public static void Queue(TextBox box)
    {
        if (Current == null) Show(box);
        else Upcoming.Enqueue(box);
    }

    public static void Queue(string list, string region="textbox", Sprite[]? talkSprites = null, SoundEffect[]? talkSounds = null)
    {
        string[] lines = list.Split("\n");
        talkSprites ??= [];
        talkSounds ??= [];
        int i = 0;
        foreach (string line in lines)
        {
            Sprite? sprite = talkSprites.Length == 0 ? null : talkSprites[Math.Min(i, talkSprites.Length - 1)].Clone();
            SoundEffect? sound = talkSounds.Length == 0 ? null : talkSounds[Math.Min(i, talkSounds.Length - 1)];

            Queue(new(line, region, sprite, sound));
            i++;
        }
    }

    public void Close()
    {
        if (Current == this) Current = null;
        OnClose?.Invoke();
        if (Current == null && Upcoming.Count > 0)
        {
            Show(Upcoming.Dequeue());
        }
    }

    public static void UpdateCurrent(GameTime time)
    {
        DialogueAtFrameStart = InDialogue;
        Current?.Update(time);
    }

    public static void Say(IEnumerable<string> lines, SoundEffect? sound = null, Action? onFinished = null, string region = "textbox")
    {
        var boxes = lines.Select(line => new TextBox(line, region)).ToList();
        if (boxes.Count == 0)
        {
            onFinished?.Invoke();
            return;
        }

        boxes[0].Sound = sound;
        boxes[^1].OnClose = onFinished;
        foreach (var box in boxes) Queue(box);
    }

    public void Update(GameTime time)
    {
        if (_justOpened)
        {
            _justOpened = false;
            return;
        }

        if (!Core.Console.Open && SpacePressed)
        {
            if (IsTyping) _typed = Content.Length;
            else Close();
            return;
        }

        TalkSprite?.Update(time);

        int before = VisibleCharacters;
        _typed += (float)time.ElapsedGameTime.TotalSeconds * CharactersPerSecond;
        if (VisibleCharacters > before) OnCharacterTyped();
    }

    private void OnCharacterTyped()
    {
        if (TalkSound is null) return;
        if (!char.IsLetterOrDigit(Content[VisibleCharacters - 1])) return;
        Core.Audio.PlaySound(TalkSound);
    }

    private static void DrawFrame(SpriteBatch batch, Sprite[] sprites, Rectangle bounds, int cornerWidth, int cornerHeight)
    {
        int[] xs = [bounds.Left, bounds.Left + cornerWidth, bounds.Right - cornerWidth];
        int[] widths = [cornerWidth, bounds.Width - cornerWidth * 2, cornerWidth];
        int[] ys = [bounds.Top, bounds.Top + cornerHeight, bounds.Bottom - cornerHeight];
        int[] heights = [cornerHeight, bounds.Height - cornerHeight * 2, cornerHeight];

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                if (widths[column] <= 0 || heights[row] <= 0) continue;
                var region = sprites[row * 3 + column].Region;
                batch.Draw(region.Texture, new Rectangle(xs[column], ys[row], widths[column], heights[row]), region.SourceRectangle, Color.White);
            }
        }
    }

    private static IEnumerable<string> Wrap(SpriteFont font, string text, float maxWidth)
    {
        foreach (var paragraph in text.Replace("\r", "").Split('\n'))
        {
            string line = "";
            foreach (var word in paragraph.Split(' '))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && font.MeasureString(candidate).X > maxWidth)
                {
                    yield return line;
                    line = word;
                }
                else
                {
                    line = candidate;
                }
            }
            yield return line;
        }
    }

    private static Sprite[] GetTextBoxSprite(string regionId)
    {
        if (BoxSpriteLookup.TryGetValue(regionId, out Sprite[] sprites)) return sprites;

        var atlas = MainGame.Atlas;
        var region = atlas.GetRegion(regionId) ?? throw new ArgumentException($"Unknown texture region {regionId}");

        sprites = new Sprite[9];

        var source = region.SourceRectangle;
        var xThirds = source.Width / 3;
        var yThirds = source.Height / 3;

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                var subRegion = new TextureRegion(atlas.Texture, source.X + x * xThirds, source.Y + y * yThirds, xThirds, yThirds);
                var subSprite = new Sprite(subRegion)
                {
                    Shadow = null,
                    Origin = Vector2.Zero
                };
                sprites[y * 3 + x] = subSprite;
            }
        }

        BoxSpriteLookup.Add(regionId, sprites);
        return sprites;
    }
}