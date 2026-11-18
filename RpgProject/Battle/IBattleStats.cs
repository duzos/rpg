using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SummitKit;
using SummitKit.Graphics;
using SummitKit.Physics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RpgProject.Battle;

public interface IBattleStats {
    int Health { get; set; }
    int MaxHealth { get; set; }
    int Attack { get; set; }
    Color HealthBarColour { get; protected set; }
    void OnBattleStart(BattleState state) { }
    void OnBattleEnd(BattleState state) { }

    void DrawHealthBar(SpriteBatch batch, Color colour, Color bgColour, float offset, float x, float y, float height, float width, float padding = 0f)
    {
        var pixel = Core.Pixel;
        int left = (int)(x - width / 2f);
        int top = (int)(y - offset);
        int pad = (int)padding;
        float fraction = MaxHealth <= 0 ? 0f : Math.Clamp(Health / (float)MaxHealth, 0f, 1f);

        if (pad > 0) batch.Draw(pixel, new Rectangle(left - pad, top - pad, (int)width + pad * 2, (int)height + pad * 2), bgColour);
        batch.Draw(pixel, new Rectangle(left, top, (int)(width * fraction), (int)height), colour);

        var font = Core.ConsoleFont;
        if (font is null) return;
        batch.DrawString(font, $"{Health}/{MaxHealth}", new Vector2(left, top + height + pad + 1), Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
    }

    void DrawHealthBar(SpriteBatch batch, Color? colour = null, Color? bgColour = null, float offset = 10f)
    {
        if (this is IPositioned pos)
        {
            DrawHealthBar(batch, colour ?? HealthBarColour, bgColour ?? Color.White, offset, pos.Position.X + (pos.Width / 2), pos.Position.Y, 3f, 25f);
            return;
        }

        throw new NotImplementedException();
    }
}