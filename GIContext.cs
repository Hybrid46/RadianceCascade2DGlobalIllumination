using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;

public sealed class GIContext : IDisposable
{
    public readonly List<Cascade> Cascades;
    public readonly RenderTexture2D FinalGI;          // Composite of all cascades
    public readonly Shader RaymarchShader;
    public readonly Shader MergeShader;
    public readonly Shader FinalShader;               // Scene + GI composite

    private readonly RenderTexture2D SDFTex;          // SDF texture

    public GIContext(List<Rectangle> obstacles)
    {
        // Build the base SDF
        SDFTex = SDFGenerator.BuildSDF(obstacles, 512);

        // Setup cascades (example: 3 levels)
        Cascades = new List<Cascade>
        {
            new Cascade(128, 128, 8),   // near cascade
            new Cascade(256, 256, 16),  // mid
            new Cascade(512, 512, 32)   // far
        };

        FinalGI = Raylib.LoadRenderTexture(SDFTex.Texture.Width, SDFTex.Texture.Height);

        // Load shaders
        RaymarchShader = Raylib.LoadShader(null, $"shaders/raymarch.fs");
        MergeShader = Raylib.LoadShader(null, $"shaders/merge.fs");
        FinalShader = Raylib.LoadShader(null, $"shaders/final.fs");
    }

    // ---------------------------------------------------------
    // Per‑frame update
    // ---------------------------------------------------------
    public void Update()
    {
        // 1) Ray‑march each cascade
        foreach (Cascade cascade in Cascades)
        {
            RaymarchPass(cascade);
        }

        // 2) Merge all cascades into FinalGI
        MergePass();
    }

    public void DrawGI()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Raylib.BeginBlendMode(BlendMode.Additive);
        Raylib.DrawTextureRec(FinalGI.Texture, new Rectangle(0, 0, FinalGI.Texture.Width, -FinalGI.Texture.Height), Vector2.Zero, Color.White);
        Raylib.EndBlendMode();

        Raylib.EndDrawing();
    }

    public void DrawSDF()
    {
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Raylib.BeginBlendMode(BlendMode.Additive);
        Raylib.DrawTextureRec(SDFTex.Texture, new Rectangle(0, 0, SDFTex.Texture.Width, -SDFTex.Texture.Height), Vector2.Zero, Color.White);
        Raylib.EndBlendMode();

        Raylib.EndDrawing();
    }

    public void DrawRayMarch(int index)
    {
        foreach (Cascade cascade in Cascades)
        {
            RaymarchPass(cascade);
        }

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Raylib.BeginBlendMode(BlendMode.Additive);
        Raylib.DrawTextureRec(Cascades[index].Texture.Texture, new Rectangle(0, 0, Cascades[index].Texture.Texture.Width, -Cascades[index].Texture.Texture.Height), Vector2.Zero, Color.White);
        Raylib.EndBlendMode();

        Raylib.EndDrawing();
    }

    // ---------------------------------------------------------
    // GPU Passes
    // ---------------------------------------------------------
    private void RaymarchPass(Cascade cascade)
    {
        Raylib.BeginTextureMode(cascade.Texture);
        Raylib.ClearBackground(Color.Black);

        Raylib.BeginShaderMode(RaymarchShader);

        Raylib.SetShaderValueTexture(RaymarchShader,
            Raylib.GetShaderLocation(RaymarchShader, "u_sdf"),
            SDFTex.Texture);

        Vector2 res = new Vector2(cascade.Width, cascade.Height);
        Raylib.SetShaderValue(RaymarchShader,
            Raylib.GetShaderLocation(RaymarchShader, "u_resolution"),
            res,
            ShaderUniformDataType.Vec2);

        Raylib.SetShaderValue(RaymarchShader,
            Raylib.GetShaderLocation(RaymarchShader, "u_rayCount"),
            cascade.RaysPerProbe,
            ShaderUniformDataType.Int);

        // We draw a fullscreen quad that maps to the probe grid
        Raylib.DrawTextureRec(SDFTex.Texture, new Rectangle(0, 0, SDFTex.Texture.Width, -SDFTex.Texture.Height), Vector2.Zero, Color.White);

        Raylib.EndShaderMode();
        Raylib.EndTextureMode();
    }

    private void MergePass()
    {
        Raylib.BeginTextureMode(FinalGI);
        Raylib.ClearBackground(Color.Black);

        Raylib.BeginShaderMode(MergeShader);

        int count = Cascades.Count;
        int loc = Raylib.GetShaderLocation(MergeShader, "u_cascadeCount");
        Raylib.SetShaderValue(MergeShader, loc, count, ShaderUniformDataType.Int);

        // Bind all cascade textures as separate sampler2D uniforms
        for (int i = 0; i < Cascades.Count; i++)
        {
            string name = $"u_cascade{i}";
            int location = Raylib.GetShaderLocation(MergeShader, name);
            Raylib.SetShaderValueTexture(MergeShader, location, Cascades[i].Texture.Texture);
        }

        // Draw a full‑screen quad
        Raylib.DrawTextureRec(SDFTex.Texture, new Rectangle(0, 0, SDFTex.Texture.Width, -SDFTex.Texture.Height), Vector2.Zero, Color.White);

        Raylib.EndShaderMode();
        Raylib.EndTextureMode();
    }

    public void Dispose()
    {
        Raylib.UnloadTexture(SDFTex.Texture);
        Raylib.UnloadRenderTexture(SDFTex);
        Raylib.UnloadRenderTexture(FinalGI);
        Raylib.UnloadShader(RaymarchShader);
        Raylib.UnloadShader(MergeShader);
        Raylib.UnloadShader(FinalShader);
        foreach (var c in Cascades) c.Dispose();
    }
}
