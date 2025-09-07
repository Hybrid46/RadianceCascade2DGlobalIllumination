using Raylib_cs;
using System;
using System.Numerics;
using System.Collections.Generic;

class Program
{
    const int screenWidth = 1280;
    const int screenHeight = 720;

    static void Main()
    {
        Raylib.InitWindow(screenWidth, screenHeight, "Radiance Cascade GI 2‑D");
        Raylib.SetTargetFPS(60);

        // ---- Demo geometry: a few rectangles acting as walls ----
        var walls = new List<Rectangle>
        {
            new Rectangle(100, 100, 200, 20),   // horizontal
            new Rectangle(300, 300, 20, 200),   // vertical
            new Rectangle(500, 100, 150, 150),  // block
            new Rectangle(800, 400, 200, 20)
        };

        // ---- GI Context (cascades + shaders) ----
        using var gi = new GIContext(walls);

        // Main loop
        while (!Raylib.WindowShouldClose())
        {
            // 1) Update GI (raymarch + merge)
            gi.Update();

            // Optional: visualize the SDF
            //gi.DrawSDF();

            // Optional: visualize cascade
            //gi.DrawRayMarch(0);
            //gi.DrawRayMarch(1);
            //gi.DrawRayMarch(2);

            // 2) Render
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            // ---- Main scene (demo sprites) ----
            DrawDemoScene(walls);

            // ---- Add GI on top ----
            Raylib.BeginBlendMode(BlendMode.Additive);
            Raylib.DrawTextureRec(gi.FinalGI.Texture,
                new Rectangle(0, 0, gi.FinalGI.Texture.Width, -gi.FinalGI.Texture.Height),
                Vector2.Zero, Color.White);
            Raylib.EndBlendMode();

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    static void DrawDemoScene(List<Rectangle> walls)
    {
        // Draw walls (simple white rectangles)
        foreach (var r in walls)
        {
            Raylib.DrawRectangleRec(r, Color.White);
        }

        // Draw a moving sprite (circle) to show indirect light
        Vector2 pos = new Vector2(
            (float)(Raylib.GetTime() * 100f % (float)screenWidth),
            (float)(Raylib.GetTime() * 75f % (float)screenHeight));
        Raylib.DrawCircleV(pos, 20, Color.Lime);
    }
}
