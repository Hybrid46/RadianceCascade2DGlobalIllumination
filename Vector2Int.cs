using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

public struct Vector2Int : IEquatable<Vector2Int>, IFormattable
{
    private int m_X;
    private int m_Y;

    private static readonly Vector2Int s_Zero = new Vector2Int(0, 0);
    private static readonly Vector2Int s_One = new Vector2Int(1, 1);
    private static readonly Vector2Int s_Up = new Vector2Int(0, 1);
    private static readonly Vector2Int s_Down = new Vector2Int(0, -1);
    private static readonly Vector2Int s_Left = new Vector2Int(-1, 0);
    private static readonly Vector2Int s_Right = new Vector2Int(1, 0);

    public int x
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_X;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => m_X = value;
    }

    public int y
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => m_Y;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => m_Y = value;
    }

    public int this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => index switch
        {
            0 => x,
            1 => y,
            _ => throw new IndexOutOfRangeException($"Invalid Vector2Int index: {index}!")
        };
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            switch (index)
            {
                case 0: x = value; break;
                case 1: y = value; break;
                default: throw new IndexOutOfRangeException($"Invalid Vector2Int index: {index}!");
            }
        }
    }

    public float magnitude => (float)Math.Sqrt(sqrMagnitude);
    public int sqrMagnitude => x * x + y * y;

    public static Vector2Int zero => s_Zero;
    public static Vector2Int one => s_One;
    public static Vector2Int up => s_Up;
    public static Vector2Int down => s_Down;
    public static Vector2Int left => s_Left;
    public static Vector2Int right => s_Right;

    public Vector2Int(int x, int y)
    {
        m_X = x;
        m_Y = y;
    }

    public Vector2Int(Vector2 v)
    {
        m_X = (int)v.X;
        m_Y = (int)v.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(int x, int y)
    {
        m_X = x;
        m_Y = y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(Vector2Int a, Vector2Int b)
    {
        int dx = a.x - b.x;
        int dy = a.y - b.y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Min(Vector2Int lhs, Vector2Int rhs) =>
        new Vector2Int(Math.Min(lhs.x, rhs.x), Math.Min(lhs.y, rhs.y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Max(Vector2Int lhs, Vector2Int rhs) =>
        new Vector2Int(Math.Max(lhs.x, rhs.x), Math.Max(lhs.y, rhs.y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int Scale(Vector2Int a, Vector2Int b) =>
        new Vector2Int(a.x * b.x, a.y * b.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Scale(Vector2Int scale)
    {
        x *= scale.x;
        y *= scale.y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clamp(Vector2Int min, Vector2Int max)
    {
        x = Math.Max(min.x, Math.Min(max.x, x));
        y = Math.Max(min.y, Math.Min(max.y, y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int FloorToInt(Vector2 v) =>
        new Vector2Int((int)Math.Floor(v.X), (int)Math.Floor(v.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int CeilToInt(Vector2 v) =>
        new Vector2Int((int)Math.Ceiling(v.X), (int)Math.Ceiling(v.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int RoundToInt(Vector2 v) =>
        new Vector2Int((int)Math.Round(v.X), (int)Math.Round(v.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector2(Vector2Int v) =>
        new Vector2(v.x, v.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator -(Vector2Int v) => new Vector2Int(-v.x, -v.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator +(Vector2Int a, Vector2Int b) =>
        new Vector2Int(a.x + b.x, a.y + b.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator -(Vector2Int a, Vector2Int b) =>
        new Vector2Int(a.x - b.x, a.y - b.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator *(Vector2Int a, Vector2Int b) =>
        new Vector2Int(a.x * b.x, a.y * b.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator *(int a, Vector2Int b) =>
        new Vector2Int(a * b.x, a * b.y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator *(Vector2Int a, int b) =>
        new Vector2Int(a.x * b, a.y * b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2Int operator /(Vector2Int a, int b) =>
        new Vector2Int(a.x / b, a.y / b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector2Int lhs, Vector2Int rhs) =>
        lhs.x == rhs.x && lhs.y == rhs.y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector2Int lhs, Vector2Int rhs) =>
        !(lhs == rhs);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object other) =>
        other is Vector2Int v && Equals(v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Vector2Int other) =>
        x == other.x && y == other.y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() =>
        (x * 73856093) ^ (y * 83492791);

    public override string ToString() =>
        ToString(null, null);

    public string ToString(string format) =>
        ToString(format, null);

    public string ToString(string format, IFormatProvider formatProvider)
    {
        if (formatProvider == null)
            formatProvider = CultureInfo.InvariantCulture;

        return $"({x.ToString(format, formatProvider)}, {y.ToString(format, formatProvider)})";
    }
}