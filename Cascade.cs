using Raylib_cs;
using System;

public sealed class Cascade : IDisposable
{
    public readonly RenderTexture2D Texture;   // Holds the radiance map for this cascade
    public readonly int Width, Height;        // Grid size (e.g., 128x128)
    public readonly int RaysPerProbe;         // Fixed cost per cascade

    public Cascade(int width, int height, int raysPerProbe)
    {
        Width = width;
        Height = height;
        RaysPerProbe = raysPerProbe;
        Texture = Raylib.LoadRenderTexture(width, height);
        // Clear the texture initially
        Raylib.BeginTextureMode(Texture);
        Raylib.ClearBackground(Color.Black);
        Raylib.EndTextureMode();
    }

    public void Dispose() => Raylib.UnloadRenderTexture(Texture);
}
