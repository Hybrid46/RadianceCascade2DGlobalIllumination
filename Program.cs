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
        GIBlitter_shader = Raylib.LoadShader(null, "shaders/blitter.glsl");

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
        Vector2 screen = new Vector2(screenWidth, screenHeight);

        // ---- 1. ScreenUV ----
        //Copying the color texture to another one using Screen UV shader to start the JumpFlood Algorithm
        //cmd.Blit(colorRT, jumpFloodRT1, screenUVMat); -> Unity original
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
        int steps = (int)Math.Ceiling(Math.Log(max));
        float stepSize = 1.0f;

        // ---- 2. Jump Flood iterations ----
        /*
            for (var n = 0; n < steps; n++)
            {
                stepSize *= 0.5f;
                cmd.SetGlobalFloat("_StepSize", stepSize);
                //you might find setting this value as global is unecessary but for some reason when using for loops you can't set the value directly to the material with Material.SetFloat
                //The value don't get passed correctly. Why, I have no idea

                if (jumpFlood1IsFinal)
                {
                    cmd.Blit(jumpFloodRT1, jumpFloodRT2, jumpFloodMat);
                }
                else
                {
                    cmd.Blit(jumpFloodRT2, jumpFloodRT1, jumpFloodMat);
                }

                jumpFlood1IsFinal = !jumpFlood1IsFinal;
            }
         */
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

        /*
            //We check which texture holds the final result and we apply the DistanceField shader to it
            if (jumpFlood1IsFinal)
            {
                cmd.Blit(jumpFloodRT1, distanceRT, distanceFieldMat);
            }
            else
            {
                cmd.Blit(jumpFloodRT2, distanceRT, distanceFieldMat);
            }
         */

        // ---- 3. Distance field ----
        RenderTexture2D src = jumpFlood1IsFinal ? jumpRT1 : jumpRT2;
        Raylib.BeginTextureMode(distRT);
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginShaderMode(distanceField_shader);

        Raylib.SetShaderValue(distanceField_shader, Raylib.GetShaderLocation(distanceField_shader, "_Aspect"), aspect, ShaderUniformDataType.Vec2);

        Raylib.DrawTextureRec(src.Texture,
            new Rectangle(0, 0, src.Texture.Width, -src.Texture.Height),
            Vector2.Zero, Color.White);
        Raylib.EndShaderMode();
        Raylib.EndTextureMode();

        /*
            gi1IsFinal = false;//Same as "jumpFlood1IsFinal"
            for (int i = cascadeCount - 1; i >= 0; i--)
            {
                cmd.SetGlobalInt("_CascadeLevel", i);//Again setting it as global cause I can't pass it directly to the material from a for loop
                                                        //the shader handles the computation of the cascades and the merging at the same time
                if (gi1IsFinal)
                {
                    cmd.Blit(giRT1, giRT2, giMat);
                }
                else
                {
                    cmd.Blit(giRT2, giRT1, giMat);
                }

                gi1IsFinal = !gi1IsFinal;
            }                
         */

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
        }

        //----5.Blit GI onto scene----
        /*
         cmd.Blit(renderingData.cameraData.renderer.cameraColorTargetHandle, colorRT);

                if (gi1IsFinal)
                {
                    blitterMat.SetTexture("_GITex", giRT1);
                }
                else
                {
                    blitterMat.SetTexture("_GITex", giRT2);
                }

                cmd.Blit(colorRT, renderingData.cameraData.renderer.cameraColorTargetHandle, blitterMat);//Finaly blending the final result to the camera texture
         */

        //TODO -> Bilinear filtering in shader!

        /*
         //Possible fix for self copy colorRT:

        RenderTexture2D tempRT = Raylib.LoadRenderTexture(screenWidth, screenHeight);

Raylib.BeginTextureMode(tempRT);
Raylib.BeginShaderMode(GIBlitter_shader);
Raylib.SetShaderValueTexture(GIBlitter_shader, Raylib.GetShaderLocation(GIBlitter_shader, "_GITex"), finalGI.Texture);
Raylib.DrawTextureRec(colorRT.Texture, 
    new Rectangle(0, 0, colorRT.Texture.Width, colorRT.Texture.Height),
    Vector2.Zero, Color.White);
Raylib.EndShaderMode();
Raylib.EndTextureMode();

// Copy back to colorRT
Raylib.BeginTextureMode(colorRT);
Raylib.DrawTextureRec(tempRT.Texture, 
    new Rectangle(0, 0, tempRT.Texture.Width, tempRT.Texture.Height),
    Vector2.Zero, Color.White);
Raylib.EndTextureMode();

Raylib.UnloadRenderTexture(tempRT);
         */

        RenderTexture2D finalGI = gi1IsFinal ? giRT1 : giRT2;

        Raylib.BeginTextureMode(colorRT);
        Raylib.BeginShaderMode(GIBlitter_shader);

        Raylib.SetShaderValueTexture(GIBlitter_shader, Raylib.GetShaderLocation(GIBlitter_shader, "_GITex"), finalGI.Texture);

        Raylib.DrawTextureRec(colorRT.Texture,
            new Rectangle(0, 0, colorRT.Texture.Width, -colorRT.Texture.Height),
            Vector2.Zero, Color.White);

        Raylib.EndShaderMode();
        Raylib.EndTextureMode();
    }

    private static void SetGIShaderValues(Vector2 aspect, int cascadeLevel)
    {
        /*
          //Passing values to the GI Shader
        giMat.SetTexture("_ColorTex", colorRT);
        giMat.SetTexture("_DistanceTex", distanceRT);
        giMat.SetFloat("_RayRange", (screen / Mathf.Min(screen.x, screen.y)).magnitude * rayRange);
        giMat.SetInt("_CascadeCount", cascadeCount);
        giMat.SetFloat("_SkyRadiance", volume.skyRadiance.value ? 1 : 0);
        giMat.SetColor("_SkyColor", volume.skyColor.value);
        giMat.SetColor("_SunColor", volume.sunColor.value);
        giMat.SetFloat("_SunAngle", volume.sunAngle.value);
        giMat.SetVector("_CascadeResolution", (Vector2)cascadeResolution);
 */

        // bind textures
        Raylib.SetShaderValueTexture(GI_shader, Raylib.GetShaderLocation(GI_shader, "_ColorTex"), colorRT.Texture);
        Raylib.SetShaderValueTexture(GI_shader, Raylib.GetShaderLocation(GI_shader, "_DistanceTex"), distRT.Texture);

        // uniform params
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_CascadeResolutionX"), cascadeResolution.x, ShaderUniformDataType.Float);
        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_CascadeResolutionY"), cascadeResolution.y, ShaderUniformDataType.Float);

        Raylib.SetShaderValue(GI_shader, Raylib.GetShaderLocation(GI_shader, "_CascadeLevel"), cascadeLevel, ShaderUniformDataType.Int); //TODO -> What do we use it for?
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
