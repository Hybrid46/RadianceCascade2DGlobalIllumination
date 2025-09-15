﻿using Raylib_cs;
using System;
using System.Numerics;

class Program
{
    // ---------- Config ----------
    const int screenWidth = 1200;
    const int screenHeight = 900;

    // ---------- Shaders ----------
    static Shader screenUV_shader;
    static Shader jumpFlood_shader;
    static Shader distanceField_shader;
    static Shader GI_shader;
    static Shader GIBlitter_shader;

    // ---------- Render textures ----------
    static RenderTexture2D colorRT;
    static RenderTexture2D emissiveRT;
    static RenderTexture2D jumpRT1, jumpRT2;
    static RenderTexture2D distRT;
    static RenderTexture2D giRT1, giRT2;
    static RenderTexture2D tempRT;

    // GI Config
    static int cascadeCount;
    static float renderScale;
    static float rayRange;
    static Vector2Int cascadeResolution;

    static void Main()
    {
        Raylib.InitWindow(screenWidth, screenHeight, "RC2DGI in Raylib");
        Raylib.SetTargetFPS(60);

        // Load shaders
        screenUV_shader = Raylib.LoadShader(null, "shaders/ScreenUV.fs");
        jumpFlood_shader = Raylib.LoadShader(null, "shaders/JumpFlood.fs");
        distanceField_shader = Raylib.LoadShader(null, "shaders/DistanceField.fs");
        GI_shader = Raylib.LoadShader(null, "shaders/RadianceCascades.fs");
        GIBlitter_shader = Raylib.LoadShader(null, "shaders/Merge.fs");

        // Setup GI config
        cascadeCount = 6;
        renderScale = 1.0f;
        rayRange = 2.0f;

        //int cascadeWidth = Mathf.CeilToInt((screenWidth * renderScale) / Math.Pow(2, cascadeCount)) * (int)Math.Pow(2, cascadeCount);
        //int cascadeHeight = Mathf.CeilToInt((screenHeight * renderScale) / Math.Pow(2, cascadeCount)) * (int)Math.Pow(2, cascadeCount);
        double powVal = Math.Pow(2, cascadeCount);
        int cascadeWidth = (int)Math.Ceiling((screenWidth * renderScale) / powVal) * (int)powVal;
        int cascadeHeight = (int)Math.Ceiling((screenHeight * renderScale) / powVal) * (int)powVal;

        cascadeResolution = new Vector2Int(cascadeWidth, cascadeHeight);

        emissiveRT = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        colorRT = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        distRT = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        jumpRT1 = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        jumpRT2 = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        giRT1 = Raylib.LoadRenderTexture(cascadeResolution.x, cascadeResolution.y);
        giRT2 = Raylib.LoadRenderTexture(cascadeResolution.x, cascadeResolution.y);
        tempRT = Raylib.LoadRenderTexture(screenWidth, screenHeight);

        Raylib.SetTextureFilter(emissiveRT.Texture, TextureFilter.Point);
        Raylib.SetTextureFilter(colorRT.Texture, TextureFilter.Point);
        Raylib.SetTextureFilter(distRT.Texture, TextureFilter.Point);
        Raylib.SetTextureFilter(jumpRT1.Texture, TextureFilter.Point);
        Raylib.SetTextureFilter(jumpRT2.Texture, TextureFilter.Point);
        Raylib.SetTextureFilter(tempRT.Texture, TextureFilter.Point);

        Raylib.SetTextureFilter(giRT1.Texture, TextureFilter.Bilinear);
        Raylib.SetTextureFilter(giRT2.Texture, TextureFilter.Bilinear);

        //emissiveRT.Texture.Format = PixelFormat.UncompressedR32G32B32A32;
        //colorRT.Texture.Format = PixelFormat.UncompressedR32G32B32A32;
        //distRT.Texture.Format = PixelFormat.UncompressedR16;
        //jumpRT1.Texture.Format = PixelFormat.UncompressedR16G16B16;
        //jumpRT2.Texture.Format = PixelFormat.UncompressedR16G16B16;
        //giRT1.Texture.Format = PixelFormat.UncompressedR16G16B16A16;
        //giRT2.Texture.Format = PixelFormat.UncompressedR16G16B16A16;
        //tempRT.Texture.Format = PixelFormat.UncompressedR32G32B32A32;

        ClearAllRTs();

        // Demo geometry (simple walls)
        List<Rectangle> walls = new List<Rectangle>
        {
            new Rectangle(100, 100, 200, 20),
            new Rectangle(300, 300, 20, 200),
            new Rectangle(500, 100, 150, 150),
            new Rectangle(800, 400, 200, 20)
        };

        while (!Raylib.WindowShouldClose())
        {
            ClearAllRTs();

            // 1. Scene render
            RenderScene(walls);

            // 2. RC2DGI pipeline
            DoRC2DGI();

            // 3. Display final
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            // Draw main scene
            Raylib.DrawTextureRec(colorRT.Texture,
                new Rectangle(0, 0, colorRT.Texture.Width, -colorRT.Texture.Height),
                Vector2.Zero, Color.White);

            // Draw debug textures
            int debugSize = 100;
            int padding = 10;
            int startX = screenWidth - debugSize - padding;

            DrawDebugTexture(colorRT, new Vector2(startX, padding), debugSize, "Scene");
            DrawDebugTexture(emissiveRT, new Vector2(startX, padding * 2 + debugSize), debugSize, "Emissive");
            //DrawDebugTexture(jumpRT2, new Vector2(startX, padding * 3 + debugSize * 2), debugSize, "Jump2");
            DrawDebugTexture(jumpRT2, new Vector2(startX, padding * 3 + debugSize * 2), debugSize, "Jump2");
            DrawDebugTexture(distRT, new Vector2(startX, padding * 4 + debugSize * 3), debugSize, "Distance");
            DrawDebugTexture(giRT1, new Vector2(startX, padding * 5 + debugSize * 4), debugSize, "GI1");
            DrawDebugTexture(giRT2, new Vector2(startX, padding * 6 + debugSize * 5), debugSize, "GI2");
            DrawDebugTexture(tempRT, new Vector2(startX, padding * 7 + debugSize * 6), debugSize, "temp");

            Raylib.EndDrawing();
        }

        // cleanup
        Raylib.UnloadRenderTexture(colorRT);
        Raylib.UnloadRenderTexture(emissiveRT);
        Raylib.UnloadRenderTexture(jumpRT1);
        Raylib.UnloadRenderTexture(jumpRT2);
        Raylib.UnloadRenderTexture(distRT);
        Raylib.UnloadRenderTexture(giRT1);
        Raylib.UnloadRenderTexture(giRT2);
        Raylib.UnloadRenderTexture(tempRT);

        Raylib.UnloadShader(screenUV_shader);
        Raylib.UnloadShader(jumpFlood_shader);
        Raylib.UnloadShader(distanceField_shader);
        Raylib.UnloadShader(GI_shader);
        Raylib.UnloadShader(GIBlitter_shader);

        Raylib.CloseWindow();
    }

    // ---------- Render helpers ----------
    static void RenderScene(List<Rectangle> walls)
    {
        Raylib.BeginTextureMode(colorRT);
        Raylib.ClearBackground(Color.Black);

        // draw walls (white)
        foreach (var r in walls)
            Raylib.DrawRectangleRec(r, Color.White);

        // moving sprite
        Vector2 pos = new Vector2(
            (float)(Raylib.GetTime() * 100f % screenWidth),
            (float)(Raylib.GetTime() * 75f % screenHeight));
        Raylib.DrawCircleV(pos, 20f, Color.Lime);

        // moving sprite
        for (int i = 1; i < 2; i++)
        {
            Vector2 emissivePos = new Vector2(
                (float)(Raylib.GetTime() * i * 66f % screenWidth),
                (float)(Raylib.GetTime() * i * 46f % screenHeight));
            Raylib.DrawCircleV(emissivePos, 80f, Color.Orange);
        }

        Raylib.EndTextureMode();

        //emission
        Raylib.BeginTextureMode(emissiveRT);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));

        // moving sprite
        for (int i = 1; i < 2; i++)
        {
            Vector2 emissivePos = new Vector2(
                (float)(Raylib.GetTime() * i * 66f % screenWidth),
                (float)(Raylib.GetTime() * i * 46f % screenHeight));
            Raylib.DrawCircleV(emissivePos, 100f, Color.Orange);
        }

        Raylib.EndTextureMode();
    }

    // ---------- RC2DGI pipeline ----------
    static void DoRC2DGI()
    {
        int maxIterations = 20;
        bool jumpFlood1IsFinal = false;
        bool gi1IsFinal = false;
        int cascadeRes = 128;
        Vector2 aspect = new Vector2(screenWidth, screenHeight) / Math.Max(screenWidth, screenHeight);
        Vector2 screen = new Vector2(screenWidth, screenHeight);

        // ---- 1. ScreenUV ----
        //Copying the color texture to another one using Screen UV shader to start the JumpFlood Algorithm
        Raylib.BeginTextureMode(jumpRT1);
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginShaderMode(screenUV_shader);
        Raylib.DrawTextureRec(colorRT.Texture,
            new Rectangle(0, 0, colorRT.Texture.Width, -colorRT.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        //Start JumpFlood Algorithm
        jumpFlood1IsFinal = true;
        int max = (int)Math.Max(screen.X, screen.Y);
        //int steps = Mathf.CeilToInt(Mathf.Log(max)); -> Unity original
        int steps = (int)Math.Ceiling(Math.Log(max, 2.0));
        if (steps < 1) steps = 1;

        float stepSize = 1.0f;

        for (int i = 0; i < steps; ++i)
        {
            stepSize *= 0.5f;
            Raylib.SetShaderValue(jumpFlood_shader, Raylib.GetShaderLocation(jumpFlood_shader, "_StepSize"), stepSize, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(jumpFlood_shader, Raylib.GetShaderLocation(jumpFlood_shader, "_Aspect"), aspect, ShaderUniformDataType.Vec2);

            if (jumpFlood1IsFinal)
            {
                // src jumpRT1 → dst jumpRT2
                Raylib.BeginTextureMode(jumpRT2);
                Raylib.BeginShaderMode(jumpFlood_shader);
                Raylib.DrawTextureRec(jumpRT1.Texture,
                    new Rectangle(0, 0, jumpRT1.Texture.Width, -jumpRT1.Texture.Height),
                    Vector2.Zero, Color.White);
                Raylib.EndShaderMode();
                Raylib.EndTextureMode();
            }
            else
            {
                // src jumpRT2 → dst jumpRT1
                Raylib.BeginTextureMode(jumpRT1);
                Raylib.BeginShaderMode(jumpFlood_shader);
                Raylib.DrawTextureRec(jumpRT2.Texture,
                    new Rectangle(0, 0, jumpRT2.Texture.Width, -jumpRT2.Texture.Height),
                    Vector2.Zero, Color.White);
                Raylib.EndShaderMode();
                Raylib.EndTextureMode();
            }

            jumpFlood1IsFinal = !jumpFlood1IsFinal;
        }

        // ---- 3. Distance field ----
        RenderTexture2D finalJumpFloodRT = jumpFlood1IsFinal ? jumpRT1 : jumpRT2;
        Raylib.BeginTextureMode(distRT);
        Raylib.BeginShaderMode(distanceField_shader);

        //aspect ratio unnecessary because both textures are same size
        //Raylib.SetShaderValue(distanceField_shader, Raylib.GetShaderLocation(distanceField_shader, "_Aspect"), aspect, ShaderUniformDataType.Vec2);

        Raylib.DrawTextureRec(finalJumpFloodRT.Texture,
            new Rectangle(0, 0, finalJumpFloodRT.Texture.Width, -finalJumpFloodRT.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        // ---- 4. Radiance cascades ----
        gi1IsFinal = false;

        for (int i = cascadeCount - 1; i >= 0; i--)
        {
            RenderTexture2D srcGI = gi1IsFinal ? giRT1 : giRT2;
            RenderTexture2D dstGI = gi1IsFinal ? giRT2 : giRT1;

            Raylib.BeginTextureMode(dstGI);
            Raylib.ClearBackground(Color.Black);
            Raylib.BeginShaderMode(GI_shader);

            SetGIShaderValues(aspect, i);

            Raylib.DrawTextureRec(srcGI.Texture,
                new Rectangle(0, 0, srcGI.Texture.Width, -srcGI.Texture.Height),
                Vector2.Zero, Color.White);
            Raylib.EndShaderMode();
            Raylib.EndTextureMode();

            gi1IsFinal = !gi1IsFinal;
        }

        //TODO -> Bilinear filtering in shader!

        RenderTexture2D finalGI = gi1IsFinal ? giRT1 : giRT2;

        Raylib.BeginTextureMode(tempRT);
        Raylib.BeginShaderMode(GIBlitter_shader);
        Raylib.SetShaderValueTexture(GIBlitter_shader, Raylib.GetShaderLocation(GIBlitter_shader, "_GITex"), finalGI.Texture);
        Raylib.DrawTextureRec(colorRT.Texture,
            new Rectangle(0, 0, colorRT.Texture.Width, -colorRT.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        // Copy back to colorRT
        Raylib.BeginTextureMode(colorRT);
        Raylib.DrawTextureRec(tempRT.Texture,
            new Rectangle(0, 0, tempRT.Texture.Width, -tempRT.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndTextureMode();
    }

    private static void SetGIShaderValues(Vector2 aspect, int cascadeLevel)
    {
        // bind textures
        Raylib.SetShaderValueTexture(GI_shader, Raylib.GetShaderLocation(GI_shader, "_ColorTex"), colorRT.Texture);
        Raylib.SetShaderValueTexture(GI_shader, Raylib.GetShaderLocation(GI_shader, "_EmissiveTex"), emissiveRT.Texture);
        Raylib.SetShaderValueTexture(GI_shader, Raylib.GetShaderLocation(GI_shader, "_DistanceTex"), distRT.Texture);

        // uniform params
        Vector2 cascadeResVec = new Vector2(cascadeResolution.x, cascadeResolution.y);
        int locCascadeRes = Raylib.GetShaderLocation(GI_shader, "_CascadeResolution");
        Raylib.SetShaderValue(GI_shader, locCascadeRes, cascadeResVec, ShaderUniformDataType.Vec2);

        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_CascadeLevel"), cascadeLevel, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_CascadeCount"), cascadeCount, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_Aspect"), aspect, ShaderUniformDataType.Vec2);

        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_RayRange"), rayRange, ShaderUniformDataType.Float);

        // sky params
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_SkyRadiance"), 1.0f, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_SkyColor"), new Vector3(0.5f, 0.6f, 0.8f), ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_SunColor"), new Vector3(1.0f, 0.9f, 0.6f), ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_SunAngle"), 0.3f, ShaderUniformDataType.Float);
    }

    static void DrawDebugTexture(RenderTexture2D texture, Vector2 position, int size, string label)
    {
        // Draw the texture
        Raylib.DrawTexturePro(texture.Texture,
            new Rectangle(0, 0, texture.Texture.Width, -texture.Texture.Height),
            new Rectangle(position.X, position.Y, size, size),
            Vector2.Zero, 0f, Color.White);

        // Draw a border
        Raylib.DrawRectangleLines((int)position.X, (int)position.Y, size, size, Color.Red);

        // Draw label
        Raylib.DrawText(label, (int)position.X, (int)position.Y - 20, 20, Color.White);
    }

    static void ClearAllRTs()
    {
        Raylib.BeginTextureMode(colorRT);
        Raylib.ClearBackground(Color.Black);
        Raylib.EndTextureMode();

        Raylib.BeginTextureMode(emissiveRT);
        Raylib.ClearBackground(Color.Black);
        Raylib.EndTextureMode();

        Raylib.BeginTextureMode(jumpRT1);
        Raylib.ClearBackground(Color.Black);
        Raylib.EndTextureMode();

        Raylib.BeginTextureMode(jumpRT2);
        Raylib.ClearBackground(Color.Black);
        Raylib.EndTextureMode();

        Raylib.BeginTextureMode(distRT);
        Raylib.ClearBackground(Color.Black);
        Raylib.EndTextureMode();

        Raylib.BeginTextureMode(giRT1);
        Raylib.ClearBackground(Color.Black);
        Raylib.EndTextureMode();

        Raylib.BeginTextureMode(giRT2);
        Raylib.ClearBackground(Color.Black);
        Raylib.EndTextureMode();

        Raylib.BeginTextureMode(tempRT);
        Raylib.ClearBackground(Color.Black);
        Raylib.EndTextureMode();
    }
}
