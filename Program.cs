using Raylib_cs;
using System;
using System.Numerics;

class Program
{
    // ---------- Config ----------
    const int screenWidth = 1280;
    const int screenHeight = 720;

    // ---------- Shaders ----------
    static Shader screenUV_shader;
    static Shader jumpFlood_shader;
    static Shader distanceField_shader;
    static Shader GI_shader;
    static Shader GIBlitter_shader;

    // ---------- Render textures ----------
    static RenderTexture2D colorRT;
    static RenderTexture2D jumpRT1, jumpRT2;
    static RenderTexture2D distRT;
    static RenderTexture2D giRT1, giRT2;

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
        GIBlitter_shader = Raylib.LoadShader(null,"shaders/blitter.glsl");

        // Setup GI config
        cascadeCount = 6;
        renderScale = 1.0f;
        rayRange = 1.0f;

        // Unity Original
        //int cascadeWidth = Mathf.CeilToInt((screenWidth * renderScale) / Math.Pow(2, cascadeCount)) * (int)Math.Pow(2, cascadeCount);
        //int cascadeHeight = Mathf.CeilToInt((screenHeight * renderScale) / Math.Pow(2, cascadeCount)) * (int)Math.Pow(2, cascadeCount);
        double powVal = Math.Pow(2, cascadeCount);
        int cascadeWidth = (int)Math.Ceiling((screenWidth * renderScale) / powVal) * (int)powVal;
        int cascadeHeight = (int)Math.Ceiling((screenHeight * renderScale) / powVal) * (int)powVal;

        cascadeResolution = new Vector2Int(cascadeWidth, cascadeHeight);

        // Create render textures
        /*
            // Unity Original
            colorRT, new RenderTextureDescriptor(textureSize.x, textureSize.y, RenderTextureFormat.ARGBFloat, 0), FilterMode.Point);
            distanceRT, new RenderTextureDescriptor(textureSize.x, textureSize.y, RenderTextureFormat.RHalf, 0), FilterMode.Point);

            jumpFloodRT1, new RenderTextureDescriptor(textureSize.x, textureSize.y, RenderTextureFormat.RGHalf, 0), FilterMode.Point);
            jumpFloodRT2, new RenderTextureDescriptor(textureSize.x, textureSize.y, RenderTextureFormat.RGHalf, 0), FilterMode.Point);

            giRT1, new RenderTextureDescriptor(cascadeResolution.x, cascadeResolution.y, RenderTextureFormat.ARGBHalf, 0), FilterMode.Bilinear);
            giRT2, new RenderTextureDescriptor(cascadeResolution.x, cascadeResolution.y, RenderTextureFormat.ARGBHalf, 0), FilterMode.Bilinear); 
         */

        colorRT = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        colorRT.Texture.Format = PixelFormat.UncompressedR32G32B32A32;

        distRT = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        distRT.Texture.Format = PixelFormat.UncompressedR16;

        jumpRT1 = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        jumpRT1.Texture.Format = PixelFormat.UncompressedR16G16B16;

        jumpRT2 = Raylib.LoadRenderTexture(screenWidth, screenHeight);
        jumpRT2.Texture.Format = PixelFormat.UncompressedR16G16B16;
                
        giRT1 = Raylib.LoadRenderTexture(cascadeResolution.x, cascadeResolution.y);
        giRT1.Texture.Format = PixelFormat.UncompressedR16G16B16A16;
        giRT2 = Raylib.LoadRenderTexture(cascadeResolution.x, cascadeResolution.y);
        giRT2.Texture.Format = PixelFormat.UncompressedR16G16B16A16;

        // Initialise all RTs to black
        ClearAllRTs();

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

            // Draw main scene
            Raylib.DrawTextureRec(colorRT.Texture,
                new Rectangle(0, 0, colorRT.Texture.Width, -colorRT.Texture.Height),
                Vector2.Zero, Color.White);

            // Draw debug textures
            int debugSize = 100;
            int padding = 10;
            int startX = screenWidth - debugSize - padding;

            DrawDebugTexture(colorRT, new Vector2(startX, padding), debugSize, "Scene");
            DrawDebugTexture(jumpRT1, new Vector2(startX, padding * 2 + debugSize), debugSize, "Jump1");
            DrawDebugTexture(jumpRT2, new Vector2(startX, padding * 3 + debugSize * 2), debugSize, "Jump2");
            DrawDebugTexture(distRT, new Vector2(startX, padding * 4 + debugSize * 3), debugSize, "Distance");
            DrawDebugTexture(giRT1, new Vector2(startX, padding * 5 + debugSize * 4), debugSize, "GI1");
            DrawDebugTexture(giRT2, new Vector2(startX, padding * 6 + debugSize * 5), debugSize, "GI2");

            Raylib.EndDrawing();
        }

        // cleanup
        Raylib.UnloadRenderTexture(colorRT);
        Raylib.UnloadRenderTexture(jumpRT1);
        Raylib.UnloadRenderTexture(jumpRT2);
        Raylib.UnloadRenderTexture(distRT);
        Raylib.UnloadRenderTexture(giRT1);
        Raylib.UnloadRenderTexture(giRT2);

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

        // ---- 1. ScreenUV ----
        Raylib.BeginTextureMode(jumpRT1);
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginShaderMode(screenUV_shader);
        Raylib.DrawTextureRec(colorRT.Texture,
            new Rectangle(0, 0, colorRT.Texture.Width, -colorRT.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        bool jumpIsFinal = true;

        // ---- 2. Jump Flood iterations ----
        for (int i = 0; i < maxIterations; ++i)
        {
            float step = 1f / (float)Math.Pow(2f, i + 1);
            Raylib.SetShaderValue(jumpFlood_shader, Raylib.GetShaderLocation(jumpFlood_shader, "_StepSize"), step, ShaderUniformDataType.Float);
            Raylib.SetShaderValue(jumpFlood_shader, Raylib.GetShaderLocation(jumpFlood_shader, "_Aspect"), aspect, ShaderUniformDataType.Vec2);

            if (jumpIsFinal)
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

            jumpIsFinal = !jumpIsFinal;
        }

        // ---- 3. Distance field ----
        RenderTexture2D src = jumpIsFinal ? jumpRT1 : jumpRT2;
        Raylib.BeginTextureMode(distRT);
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginShaderMode(distanceField_shader);

        Raylib.SetShaderValue(distanceField_shader, Raylib.GetShaderLocation(distanceField_shader, "_Aspect"), aspect, ShaderUniformDataType.Vec2);

        Raylib.DrawTextureRec(src.Texture,
            new Rectangle(0, 0, src.Texture.Width, -src.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        // ---- 4. Radiance cascades ----
        Raylib.BeginTextureMode(giRT1);
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginShaderMode(GI_shader);

        // bind textures
        Raylib.SetShaderValueTexture(GI_shader, Raylib.GetShaderLocation(GI_shader, "_ColorTex"), colorRT.Texture);
        Raylib.SetShaderValueTexture(GI_shader, Raylib.GetShaderLocation(GI_shader, "_DistanceTex"), distRT.Texture);

        // uniform params
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_CascadeResolutionX"), cascadeResolution.x, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_CascadeResolutionY"), cascadeResolution.y, ShaderUniformDataType.Float);

        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_CascadeLevel"), 0, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_CascadeCount"), cascadeCount, ShaderUniformDataType.Int);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_Aspect"), aspect, ShaderUniformDataType.Vec2);

        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_RayRange"), 5.0f, ShaderUniformDataType.Float);

        // sky params
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_SkyRadiance"), 1.0f, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_SkyColor"), new Vector3(0.5f, 0.6f, 0.8f), ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_SunColor"), new Vector3(1.0f, 0.9f, 0.6f), ShaderUniformDataType.Vec3);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_SunAngle"), 0.3f, ShaderUniformDataType.Float);

        Raylib.DrawTextureRec(distRT.Texture,
            new Rectangle(0, 0, distRT.Texture.Width, -distRT.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        //----5.Blit GI onto scene----
        //TODO -> Bilinear filtering in shader!
        Raylib.BeginShaderMode(GIBlitter_shader);
        Raylib.SetShaderValueTexture(GIBlitter_shader,
            Raylib.GetShaderLocation(GIBlitter_shader, "_GITex"), giRT1.Texture);
        Raylib.BeginTextureMode(colorRT);
        Raylib.DrawTextureRec(colorRT.Texture,
            new Rectangle(0, 0, colorRT.Texture.Width, -colorRT.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndTextureMode();
        Raylib.EndShaderMode();
    }

    //TODO implement shader variable descriptors to set shader variables on blit
    //public static void BlitColor(RenderTexture2D src, RenderTexture2D dst, Shader shader, bool clearDst = false)
    //{
    //    Raylib.BeginTextureMode(dst);
    //    if (clearDst) Raylib.ClearBackground(Color.Black);
    //    Raylib.BeginShaderMode(shader);

    //    Raylib.DrawTextureRec(src.Texture,
    //        new Rectangle(0, 0, src.Texture.Width, -src.Texture.Height),
    //        Vector2.Zero, Color.White);

    //    Raylib.EndShaderMode();
    //    Raylib.EndTextureMode();
    //}

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
