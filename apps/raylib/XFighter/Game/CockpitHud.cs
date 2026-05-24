using System.Drawing;
using Novolis.Raylib.Game;
using Novolis.Raylib.Rendering;

namespace XFighter.Game;

internal sealed class CockpitHud
{
    private static readonly Color HudGreen = Color.FromArgb(255, 80, 255, 120);
    private static readonly Color HudAmber = Color.FromArgb(255, 255, 190, 60);
    private static readonly Color PanelDark = Color.FromArgb(240, 18, 22, 32);
    private static readonly Color PanelMid = Color.FromArgb(255, 35, 42, 58);

    private Texture _cockpitTexture;
    private bool _hasTexture;

    public void Initialize(RayGameContext ctx)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "cockpit_overlay.png");
        if (File.Exists(path))
        {
            var tex = Textures.Load(path);
            if (Textures.IsValid(tex))
            {
                _cockpitTexture = tex;
                _hasTexture = true;
            }
        }
    }

    public void Draw(RayGameContext ctx, PlayerFlight player, int score, int enemies, float shield)
    {
        if (_hasTexture)
        {
            ctx.DrawHudTexture(
                _cockpitTexture,
                new RectangleF(0, 0, ctx.Width, ctx.Height),
                Color.White);
        }
        else
            DrawProceduralFrame(ctx);

        var cx = ctx.Width / 2;
        var cy = ctx.Height / 2;
        var reticle = 18;
        ctx.HudLine(cx - reticle, cy, cx + reticle, cy, HudGreen);
        ctx.HudLine(cx, cy - reticle, cx, cy + reticle, HudGreen);
        ctx.HudLine(cx - 6, cy - 6, cx + 6, cy + 6, HudGreen);
        ctx.HudLine(cx + 6, cy - 6, cx - 6, cy + 6, HudGreen);

        ctx.HudText($"SCORE {score}", 48, ctx.Height - 72, 22, HudGreen);
        ctx.HudText($"H-FIGHTERS {enemies}", 48, ctx.Height - 44, 18, HudAmber);
        ctx.HudText($"SPD {(int)player.Speed}", ctx.Width - 180, ctx.Height - 72, 20, HudGreen);
        ctx.HudText($"SHLD {(int)(shield * 100)}%", ctx.Width - 180, ctx.Height - 44, 18, HudAmber);

        DrawRadar(ctx, player);
        DrawShieldBar(ctx, shield);
        ctx.HudText("MOUSE: AIM  |  W/S: THROTTLE  |  SPACE/LMB: FIRE", 48, 24, 16, Color.FromArgb(255, 160, 170, 190));
    }

    private static void DrawProceduralFrame(RayGameContext ctx)
    {
        var w = ctx.Width;
        var h = ctx.Height;
        var margin = 42;
        var viewL = (int)(w * 0.14f);
        var viewR = (int)(w * 0.86f);
        var viewT = (int)(h * 0.12f);
        var viewB = (int)(h * 0.88f);

        ctx.HudRect(0, 0, w, margin, PanelDark);
        ctx.HudRect(0, h - margin, w, margin, PanelDark);
        ctx.HudRect(0, 0, viewL, h, PanelDark);
        ctx.HudRect(viewR, 0, w - viewR, h, PanelDark);

        ctx.HudRect(viewL, margin, 6, viewB - margin, PanelMid);
        ctx.HudRect(viewR - 6, margin, 6, viewB - margin, PanelMid);
        ctx.HudRect(viewL, viewT - 6, viewR - viewL, 6, PanelMid);
        ctx.HudRect(viewL, viewB, viewR - viewL, 6, PanelMid);

        for (var i = 0; i < 8; i++)
        {
            var y = margin + i * 48;
            ctx.HudRect(viewL + 24, y, 120, 28, Color.FromArgb(200, 25, 30, 45));
            ctx.HudRect(viewR - 144, y, 120, 28, Color.FromArgb(200, 25, 30, 45));
        }
    }

    private static void DrawRadar(RayGameContext ctx, PlayerFlight player)
    {
        var rx = ctx.Width - 200;
        var ry = 80;
        var size = 140;
        ctx.HudRect(rx, ry, size, size, Color.FromArgb(180, 10, 20, 15));
        ctx.HudLine(rx, ry + size / 2, rx + size, ry + size / 2, HudGreen);
        ctx.HudLine(rx + size / 2, ry, rx + size / 2, ry + size, HudGreen);
        ctx.HudText("RADAR", rx + 36, ry + 8, 14, HudGreen);
        _ = player;
    }

    private static void DrawShieldBar(RayGameContext ctx, float shield)
    {
        var x = 48;
        var y = 80;
        var w = 200;
        var h = 16;
        ctx.HudRect(x, y, w, h, Color.FromArgb(255, 30, 30, 40));
        ctx.HudRect(x, y, (int)(w * shield), h, HudAmber);
        ctx.HudText("SHIELDS", x, y - 22, 14, HudAmber);
    }
}
