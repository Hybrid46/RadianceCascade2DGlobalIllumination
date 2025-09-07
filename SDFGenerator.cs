using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;
using Color = Raylib_cs.Color;

public sealed class SDFGenerator
{
    // Simple bounding‑box based SDF – replace with your geometry
    public static RenderTexture2D BuildSDF(List<Rectangle> obstacles, int texSize)
    {
        Image img = Raylib.GenImageColor(texSize, texSize, Color.Black); // placeholder

        // Allocate a CPU array – we’ll fill with distances
        float[] sdf = new float[texSize * texSize];
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float minDist = float.MaxValue;
                foreach (var obs in obstacles)
                {
                    float d = DistanceToRect(pos, obs);
                    if (d < minDist) minDist = d;
                }
                sdf[y * texSize + x] = minDist;
            }
        }

        // Pack into 8‑bit grayscale image
        Color[] pixels = new Color[texSize * texSize];
        for (int i = 0; i < sdf.Length; i++)
        {
            byte v = (byte)Math.Clamp(sdf[i] * 4.0f, 0, 255);
            pixels[i] = new Color(v, v, v, (byte)255);
        }

        Texture2D tex = Raylib.LoadTextureFromImage(img);
        Raylib.UpdateTexture(tex, pixels);
        Raylib.UnloadImage(img);

        RenderTexture2D rt = Raylib.LoadRenderTexture(texSize, texSize);
        Raylib.BeginTextureMode(rt);
        Raylib.DrawTexture(tex, 0, 0, Color.White);
        Raylib.EndTextureMode();
        Raylib.UnloadTexture(tex);

        return rt;
    }

    // Distance from point to axis‑aligned rectangle
    private static float DistanceToRect(Vector2 p, Rectangle r)
    {
        float dx = Math.Max(r.X - p.X, Math.Max(0, p.X - (r.X + r.Width)));
        float dy = Math.Max(r.Y - p.Y, Math.Max(0, p.Y - (r.Y + r.Height)));
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
