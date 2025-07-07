public static class FixedPoint
{
    private const int fractionalBits = 12;
    private const int scale = 1 << fractionalBits;

    public static int FloatToF32(float n)
    {
        return (int)(n * scale);
    }
}
