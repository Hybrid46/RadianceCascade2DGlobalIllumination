using Raylib_cs;
using System;
using System.Numerics;
using System.Collections.Generic;

class Program
{
    // ---------- Config ----------
    const int screenWidth = 1280;
    const int screenHeight = 720;
    const int cascadeCount = 3;
    const int cascadeRes = 128;  // base resolution, will scale up

    // ---------- Shaders ----------
    static Shader screenUV;
    static Shader jumpFlood;
    static Shader distanceField;
    static Shader radiance;
    //static Shader blitter;   // add GI to scene

    // ---------- Render textures ----------
    static RenderTexture2D sceneRT;      // rendered scene
    static RenderTexture2D jumpRT1, jumpRT2;
    static RenderTexture2D distRT;
    static RenderTexture2D giRT1, giRT2; // ping‑pong

    // ---------- Helper ----------
    static int maxIterations;     // jump‑flood steps

    static void Main()
    {
        // 1. Load shaders
        screenUV = Raylib.LoadShader(null, "shaders/ScreenUV.fs");
        jumpFlood = Raylib.LoadShader(null, "shaders/JumpFlood.fs");
        distanceField = Raylib.LoadShader(null, "shaders/DistanceField.fs");
        radiance = Raylib.LoadShader(null, "shaders/RadianceCascades.fs");
        //blitter = Raylib.LoadShader(null,"shaders/blitter.glsl");

        // 2. Create render textures
        sceneRT = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        jumpRT1 = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        jumpRT2 = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        distRT = Raylib.LoadRenderTexture(screenWidth, screenHeight);

        int cascadeW = cascadeRes << (cascadeCount - 1); // 128,256,512
        int cascadeH = cascadeW;
        giRT1 = Raylib.LoadRenderTexture(cascadeW, cascadeH);
        giRT2 = Raylib.LoadRenderTexture(cascadeW, cascadeH);

        // 3. Jump‑flood steps = log2(max(screenWidth, screenHeight))
        maxIterations = (int)System.Math.Ceiling(Math.Log(Math.Max(screenWidth, screenHeight), 2));

        // 4. Initialise all RTs to black
        ClearAllRTs();

        Raylib.InitWindow(screenWidth, screenHeight, "RC2DGI in Raylib");
        Raylib.SetTargetFPS(60);

        // Demo geometry (simple walls + moving sprite)
        List<Rectangle> walls = new List<Rectangle>
        {
            new Rectangle(100, 100, 200, 20),
            new Rectangle(300, 300, 20, 200),
            new Rectangle(500, 100, 150, 150),
            new Rectangle(800, 400, 200, 20)
        };

        while (!Raylib.WindowShouldClose())
        {
            // 1. Scene render
            RenderScene(walls);

            // 2. RC2DGI pipeline
            DoRC2DGI();

            // 3. Display final
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            // a) show original scene
            Raylib.DrawTextureRec(sceneRT.Texture,
                new Rectangle(0, 0, sceneRT.Texture.Width, -sceneRT.Texture.Height),
                Vector2.Zero, Color.White);

            // b) add GI (blitted in pipeline, now on screen)
            Raylib.DrawTextureRec(sceneRT.Texture,
                new Rectangle(0, 0, sceneRT.Texture.Width, -sceneRT.Texture.Height),
                Vector2.Zero, Color.White);

            Raylib.EndDrawing();
        }

        // cleanup
        Raylib.UnloadRenderTexture(sceneRT);
        Raylib.UnloadRenderTexture(jumpRT1);
        Raylib.UnloadRenderTexture(jumpRT2);
        Raylib.UnloadRenderTexture(distRT);
        Raylib.UnloadRenderTexture(giRT1);
        Raylib.UnloadRenderTexture(giRT2);

        Raylib.UnloadShader(screenUV);
        Raylib.UnloadShader(jumpFlood);
        Raylib.UnloadShader(distanceField);
        Raylib.UnloadShader(radiance);
        //Raylib.UnloadShader(blitter);

        Raylib.CloseWindow();
    }

    // ---------- Render helpers ----------
    static void RenderScene(List<Rectangle> walls)
    {
        Raylib.BeginTextureMode(sceneRT);
        Raylib.ClearBackground(Color.Black);

        // draw walls (white)
        foreach (var r in walls)
            Raylib.DrawRectangleRec(r, Color.White);

        // moving sprite
        Vector2 pos = new Vector2(
            (float)(Raylib.GetTime() * 100f % screenWidth),
            (float)(Raylib.GetTime() * 75f % screenHeight));
        Raylib.DrawCircleV(pos, 20f, Color.Lime);

        Raylib.EndTextureMode();
    }

    // ---------- RC2DGI pipeline ----------
    static void DoRC2DGI()
    {
        // ---- 1. ScreenUV ----
        Raylib.BeginTextureMode(jumpRT1);
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginShaderMode(screenUV);
        Raylib.DrawTextureRec(sceneRT.Texture,
            new Rectangle(0, 0, sceneRT.Texture.Width, -sceneRT.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        bool jumpIsFinal = true; // ping‑pong flag

        // ---- 2. Jump Flood iterations ----
        for (int i = 0; i < maxIterations; ++i)
        {
            float step = 1f / (float)System.Math.Pow(2f, i + 1);
            Raylib.SetShaderValue(jumpFlood, Raylib.GetShaderLocation(jumpFlood, "_StepSize"), step, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(jumpFlood, Raylib.GetShaderLocation(jumpFlood, "_Aspect"), new Vector2(screenWidth, screenHeight) / System.Math.Max(screenWidth, screenHeight), ShaderUniformDataType.Vec2);

            if (jumpIsFinal)
            {
                // src jumpRT1 → dst jumpRT2
                Raylib.BeginTextureMode(jumpRT2);
                Raylib.BeginShaderMode(jumpFlood);
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
                Raylib.BeginShaderMode(jumpFlood);
                Raylib.DrawTextureRec(jumpRT2.Texture,
                    new Rectangle(0, 0, jumpRT2.Texture.Width, -jumpRT2.Texture.Height),
                    Vector2.Zero, Color.White);
                Raylib.EndShaderMode();
                Raylib.EndTextureMode();
            }

            jumpIsFinal = !jumpIsFinal;
        }

        // ---- 3. Distance field ----
        RenderTexture2D src = jumpIsFinal ? jumpRT1 : jumpRT2;
        Raylib.BeginTextureMode(distRT);
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginShaderMode(distanceField);
        Raylib.DrawTextureRec(src.Texture,
            new Rectangle(0, 0, src.Texture.Width, -src.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        // ---- 4. Radiance cascades ----
        // For demo we do only ONE cascade – the rest would be a loop over cascade levels
        int initialCascadeResolution = 512;
        int cascadeRes = initialCascadeResolution << (cascadeCount - 1);
        Raylib.BeginTextureMode(giRT1);
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginShaderMode(radiance);

        // bind textures
        Raylib.SetShaderValueTexture(radiance, Raylib.GetShaderLocation(radiance, "_ColorTex"), sceneRT.Texture);
        Raylib.SetShaderValueTexture(radiance, Raylib.GetShaderLocation(radiance, "_DistanceTex"), distRT.Texture);

        // uniform params
        Raylib.SetShaderValue(radiance, Raylib.GetShaderLocation(radiance, "_CascadeResolutionX"), cascadeRes, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(radiance, Raylib.GetShaderLocation(radiance, "_CascadeResolutionY"), cascadeRes, ShaderUniformDataType.Float);

        Raylib.SetShaderValue(radiance, Raylib.GetShaderLocation(radiance, "_CascadeLevel"), 0, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(radiance, Raylib.GetShaderLocation(radiance, "_CascadeCount"), cascadeCount, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(radiance, Raylib.GetShaderLocation(radiance, "_Aspect"), new Vector2(screenWidth, screenHeight) / System.Math.Min(screenWidth, screenHeight), ShaderUniformDataType.Vec2);

        Raylib.SetShaderValue(radiance, Raylib.GetShaderLocation(radiance, "_RayRange"), 5.0f, ShaderUniformDataType.Float);

        // sky params
        Raylib.SetShaderValue(radiance, Raylib.GetShaderLocation(radiance, "_SkyRadiance"), 1.0f, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(radiance, Raylib.GetShaderLocation(radiance, "_SkyColor"), new Vector3(0.5f, 0.6f, 0.8f), ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(radiance, Raylib.GetShaderLocation(radiance, "_SunColor"), new Vector3(1.0f, 0.9f, 0.6f), ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(radiance, Raylib.GetShaderLocation(radiance, "_SunAngle"), 0.3f, ShaderUniformDataType.Float);

        Raylib.DrawTextureRec(distRT.Texture,
            new Rectangle(0, 0, distRT.Texture.Width, -distRT.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        // ---- 5. Blit GI onto scene ----
        //Raylib.BeginShaderMode(blitter);
        //Raylib.SetShaderValueTexture(blitter,
        //    Raylib.GetShaderLocation(blitter, "_GITex"), giRT1.Texture);
        //Raylib.BeginTextureMode(sceneRT);
        //Raylib.DrawTextureRec(sceneRT.Texture,
        //    new Rectangle(0, 0, sceneRT.Texture.Width, -sceneRT.Texture.Height),
        //    Vector2.Zero, Color.White);
        //Raylib.EndTextureMode();
        //Raylib.EndShaderMode();
    }

    static void ClearAllRTs()
    {
        Raylib.BeginTextureMode(sceneRT);
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
    }
}
