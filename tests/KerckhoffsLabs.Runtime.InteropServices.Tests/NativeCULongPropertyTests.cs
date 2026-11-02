// Licensed under the MIT License
// FsCheck properties: laws NativeCULong must keep on every input, where NativeCULongDifferentialTests
// compares each member with an oracle. Here the members are checked against each other and against the
// type's contract: it is laid out and marshalled as the BCL's CULong, formatting and parsing are inverse,
// writing and reading its bytes are inverse, the arithmetic is arithmetic modulo 2^n, and equality,
// hashing, ordering and the default comparers agree. The generic-math members are reached through type
// parameters, so the interfaces' default implementations are covered too. On a failure FsCheck shrinks
// the input and reports the seed that replays it ([Property(Replay = "...")]).
//
// The suite runs once per storage width (see the NativeCULongNarrow switch in the .csproj).

using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using static KerckhoffsLabs.Runtime.InteropServices.Tests.PlatformLayout;

namespace KerckhoffsLabs.Runtime.InteropServices.Tests;

/// <summary>Generators for <see cref="NativeCULongPropertyTests"/>.</summary>
public static class NativeCULongArbitraries
{
    // Uniform 64-bit patterns, small values, and the edges of either storage width, where off-by-one
    // and truncation bugs live. The storage keeps the low bits that fit.
    private static readonly Gen<ulong> Bits = Gen.Frequency(
        (4, from high in Gen.Choose(int.MinValue, int.MaxValue)
            from low in Gen.Choose(int.MinValue, int.MaxValue)
            select ((ulong)unchecked((uint)high) << 32) | unchecked((uint)low)),
        (2, Gen.Choose(0, 300).Select(i => (ulong)i)),
        (3, Gen.Elements(
            0UL, 1UL, 2UL, 0x7FUL, 0x80UL, 0xFFUL, 0x7FFF_FFFFUL, 0x8000_0000UL, 0xFFFF_FFFEUL, 0xFFFF_FFFFUL,
            0x1_0000_0000UL, 0x7FFF_FFFF_FFFF_FFFFUL, 0x8000_0000_0000_0000UL, ulong.MaxValue - 1, ulong.MaxValue)));

    private static readonly Arbitrary<ulong> UInt64 = ArbMap.Default.ArbFor<ulong>();

    /// <summary>Any value of the storage width, shrinking toward zero.</summary>
    public static Arbitrary<NativeCULong> NativeCULong() =>
        Arb.From(Bits.Select(Subject), x => UInt64.Shrinker((ulong)x).Select(Subject));

    /// <summary>Any value at all, as the text and endianness properties take it.</summary>
    public static Arbitrary<ulong> UInt64Bits() => Arb.From(Bits, UInt64.Shrinker);

    // FsCheck 3.6's own Int128, UInt128 and Half arbitraries. Their generators are sized (small values
    // only, within -size..size), so the values below add the whole range and the edges; their shrinkers
    // are kept as they are.
    private static readonly Arbitrary<Int128> Int128Default = ArbMap.Default.ArbFor<Int128>();
    private static readonly Arbitrary<UInt128> UInt128Default = ArbMap.Default.ArbFor<UInt128>();
    private static readonly Arbitrary<Half> HalfDefault = ArbMap.Default.ArbFor<Half>();

    // 0, 1, 2, and 2^n - 1, 2^n, 2^n + 1 around the top bit and the width of either storage width and of
    // the 128-bit types, then the top of UInt128.
    private static readonly UInt128[] UInt128Edges =
    [
        0, 1, 2,
        .. new[] { 31, 32, 63, 64, 127 }.SelectMany(n => new[] { (UInt128.One << n) - 1, UInt128.One << n, (UInt128.One << n) + 1 }),
        UInt128.MaxValue - 1, UInt128.MaxValue,
    ];

    // The same magnitudes with either sign, where they fit, then Int128.MinValue (2^127 - 1 is
    // Int128.MaxValue, and its negation Int128.MinValue + 1).
    private static readonly Int128[] Int128Edges =
    [
        .. UInt128Edges.Where(u => u <= (UInt128)Int128.MaxValue).SelectMany(u => new[] { (Int128)u, -(Int128)u }),
        Int128.MinValue,
    ];

    // Signed zeros, the subnormals' ends, the largest values below 1 in magnitude (which truncate to zero),
    // ±1 and ±1.5, the last integers Half holds exactly, MaxValue and MinValue, NaN and the infinities.
    private static readonly Half[] HalfEdges =
    [
        Half.Zero, Half.NegativeZero, Half.Epsilon, BitConverter.UInt16BitsToHalf(0x03FF), BitConverter.UInt16BitsToHalf(0x0400),
        BitConverter.UInt16BitsToHalf(0x3BFF), BitConverter.UInt16BitsToHalf(0xBBFF), (Half)0.5, (Half)(-0.5),
        Half.One, Half.NegativeOne, (Half)1.5, (Half)(-1.5), (Half)2048, (Half)2050, Half.MaxValue, Half.MinValue,
        Half.NaN, Half.PositiveInfinity, Half.NegativeInfinity,
    ];

    /// <summary>Any UInt128: uniform over the whole range, the 64-bit patterns above, small values, and the
    /// edges; shrinking with FsCheck's own UInt128 shrinker.</summary>
    public static Arbitrary<UInt128> AnyUInt128() => Arb.From(
        Gen.Frequency(
            (3, ArbMap.Default.ArbFor<DoNotSize<UInt128>>().Generator.Select(v => v.Item)),
            (3, Bits.Select(b => (UInt128)b)),
            (1, UInt128Default.Generator),
            (3, Gen.Elements(UInt128Edges))),
        UInt128Default.Shrinker);

    /// <summary>Any Int128: uniform over the whole range, the 64-bit patterns above zero- and sign-extended,
    /// small values, and the edges; shrinking with FsCheck's own Int128 shrinker.</summary>
    public static Arbitrary<Int128> AnyInt128() => Arb.From(
        Gen.Frequency(
            (3, ArbMap.Default.ArbFor<DoNotSize<Int128>>().Generator.Select(v => v.Item)),
            (2, Bits.Select(b => (Int128)b)),
            (1, Bits.Select(b => (Int128)unchecked((long)b))),
            (1, Int128Default.Generator),
            (3, Gen.Elements(Int128Edges))),
        Int128Default.Shrinker);

    /// <summary>Any Half: FsCheck's own (small values with fractions, now and then NaN, an infinity or a
    /// subnormal), uniform over every bit pattern, and the edges; shrinking with FsCheck's own Half shrinker.</summary>
    public static Arbitrary<Half> AnyHalf() => Arb.From(
        Gen.Frequency(
            (3, HalfDefault.Generator),
            (3, Gen.Choose(0, ushort.MaxValue).Select(bits => BitConverter.UInt16BitsToHalf((ushort)bits))),
            (2, Gen.Elements(HalfEdges))),
        HalfDefault.Shrinker);

    /// <summary>Built through the constructor, the most direct way to set the storage, so that no
    /// conversion under test is involved.</summary>
    internal static NativeCULong Subject(ulong bits) =>
#if WINDOWS
        new(unchecked((uint)bits));
#else
        new(unchecked((nuint)bits));
#endif
}

[Properties(Arbitrary = new[] { typeof(NativeCULongArbitraries) }, MaxTest = 1_000)]
public sealed class NativeCULongPropertyTests
{
    private static readonly int Width = Has32BitStorage ? 32 : 64;

    private static readonly ulong Max = (ulong)NativeCULong.MaxValue;

    //
    // Interop: the type's purpose is to stand for C's unsigned long, as CULong does.
    //

    // The narrow build's storage is 32-bit on a 64-bit Unix process too, where CULong is 64-bit, so the
    // two are compared only where their widths are meant to agree.
    private static bool CULongHasStorageWidth => Marshal.SizeOf<CULong>() * 8 == Width;

    [Property]
    public void Layout_IsCULongs(NativeCULong x)
    {
        Assert.Equal(Width / 8, Unsafe.SizeOf<NativeCULong>());
        Assert.Equal(Width / 8, Marshal.SizeOf<NativeCULong>());
        if (!CULongHasStorageWidth)
            return;

        Assert.Equal(x.Value, Unsafe.As<NativeCULong, CULong>(ref x).Value);
        CULong c = new(x.Value);
        Assert.Equal(x, Unsafe.As<CULong, NativeCULong>(ref c));
    }

    // Marshalled into and out of native memory, alone and as a struct field, the value and the layout
    // are CULong's.
    [Property]
    public void Marshal_RoundTripsAsCULong(NativeCULong x, byte tag)
    {
        var pair = new WithTag { Tag = tag, Value = x };
        IntPtr native = Marshal.AllocHGlobal(Marshal.SizeOf<WithTag>());
        try
        {
            Marshal.StructureToPtr(pair, native, fDeleteOld: false);
            WithTag back = Marshal.PtrToStructure<WithTag>(native);
            Assert.Equal(tag, back.Tag);
            Assert.Equal(x, back.Value);

            int offset = (int)Marshal.OffsetOf<WithTag>(nameof(WithTag.Value));
            Assert.Equal(x.Value, Width == 32
                ? (nuint)unchecked((uint)Marshal.ReadInt32(native, offset))
                : unchecked((nuint)Marshal.ReadInt64(native, offset)));
            if (CULongHasStorageWidth)
            {
                Assert.Equal(Marshal.SizeOf<WithTagCULong>(), Marshal.SizeOf<WithTag>());
                Assert.Equal(Marshal.OffsetOf<WithTagCULong>(nameof(WithTagCULong.Value)), Marshal.OffsetOf<WithTag>(nameof(WithTag.Value)));
            }
        }
        finally
        {
            Marshal.FreeHGlobal(native);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WithTag
    {
        public byte Tag;
        public NativeCULong Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WithTagCULong
    {
        public byte Tag;
        public CULong Value;
    }

    //
    // Text: formatting and parsing are inverse.
    //

    private static readonly (string Format, NumberStyles Style)[] RoundTripFormats =
    [
        ("D", NumberStyles.Integer), ("G", NumberStyles.Integer), ("D25", NumberStyles.Integer),
        ("X", NumberStyles.HexNumber), ("x16", NumberStyles.HexNumber), ("B", NumberStyles.BinaryNumber),
        ("N0", NumberStyles.Number), ("R", NumberStyles.Integer),
    ];

    private static readonly CultureInfo[] Cultures =
    [
        CultureInfo.InvariantCulture,
        CultureInfo.GetCultureInfo("en-US"),
        CultureInfo.GetCultureInfo("fr-FR"),
        CultureInfo.GetCultureInfo("de-CH"),
        CultureInfo.GetCultureInfo("ar-EG"),
    ];

    [Property]
    public void Parse_OfToString_IsTheValue(NativeCULong x, PositiveInt formatIndex, PositiveInt cultureIndex)
    {
        (string format, NumberStyles style) = RoundTripFormats[formatIndex.Get % RoundTripFormats.Length];
        CultureInfo culture = Cultures[cultureIndex.Get % Cultures.Length];
        string text = x.ToString(format, culture);

        Assert.Equal(x, NativeCULong.Parse(text, style, culture));
        Assert.Equal(x, NativeCULong.Parse(text.AsSpan(), style, culture));
        Assert.Equal(x, NativeCULong.Parse(Encoding.UTF8.GetBytes(text), style, culture));
        Assert.True(NativeCULong.TryParse(text, style, culture, out NativeCULong parsed));
        Assert.Equal(x, parsed);
        Assert.Equal(x, ParseVia<NativeCULong>(text, culture, style));
    }

    // TryFormat writes exactly what ToString returns when it fits, and nothing when it doesn't, as the
    // UTF-8 overload (the interface's default implementation) does in bytes.
    [Property]
    public void TryFormat_FitsOrWritesNothing(NativeCULong x, PositiveInt formatIndex, PositiveInt cultureIndex, NonNegativeInt shortBy)
    {
        string format = RoundTripFormats[formatIndex.Get % RoundTripFormats.Length].Format;
        CultureInfo culture = Cultures[cultureIndex.Get % Cultures.Length];
        string expected = x.ToString(format, culture);
        byte[] expectedUtf8 = Encoding.UTF8.GetBytes(expected);

        char[] chars = new char[expected.Length];
        Assert.True(x.TryFormat(chars, out int charsWritten, format, culture));
        Assert.Equal(expected, new string(chars, 0, charsWritten));

        byte[] bytes = new byte[expectedUtf8.Length];
        Assert.True(FormatUtf8Via(x, bytes, out int bytesWritten, format, culture));
        Assert.Equal(expectedUtf8, bytes[..bytesWritten]);

        int shortChars = Math.Max(0, expected.Length - 1 - (shortBy.Get % (expected.Length + 1)));
        Assert.False(x.TryFormat(new char[shortChars], out charsWritten, format, culture));
        Assert.Equal(0, charsWritten);
        int shortBytes = Math.Max(0, expectedUtf8.Length - 1 - (shortBy.Get % (expectedUtf8.Length + 1)));
        Assert.False(FormatUtf8Via(x, new byte[shortBytes], out bytesWritten, format, culture));
        Assert.Equal(0, bytesWritten);
    }

    //
    // Bytes: writing and reading are inverse, in either byte order.
    //

    [Property]
    public void ReadEndian_OfWriteEndian_IsTheValue(NativeCULong x, bool isUnsigned, byte padding)
    {
        int count = GetByteCountVia(x);
        Assert.Equal(Width / 8, count);

        byte[] big = new byte[count];
        byte[] little = new byte[count];
        Assert.Equal(count, WriteBigEndianVia(x, big));
        Assert.Equal(count, WriteLittleEndianVia(x, little));
        Assert.Equal(big.Reverse(), little);
        Assert.Equal(x.Value, Width == 32 ? BinaryPrimitives.ReadUInt32BigEndian(big) : (nuint)BinaryPrimitives.ReadUInt64BigEndian(big));

        // A set top bit read as signed is a negative value, which an unsigned type can't hold.
        bool fits = isUnsigned || (big[0] & 0x80) == 0;
        Assert.Equal(fits, TryReadBigEndianVia(big, isUnsigned, out NativeCULong fromBig));
        Assert.Equal(fits, TryReadLittleEndianVia(little, isUnsigned, out NativeCULong fromLittle));
        if (fits)
        {
            Assert.Equal(x, fromBig);
            Assert.Equal(x, fromLittle);
            Assert.Equal(x, ReadBigEndianVia<NativeCULong>(big, isUnsigned));
            Assert.Equal(x, ReadLittleEndianVia<NativeCULong>(little, isUnsigned));
        }
        else
        {
            Assert.Throws<OverflowException>(() => ReadBigEndianVia<NativeCULong>(big, isUnsigned));
        }

        // Extra high-order zero bytes don't change the value; any other extra byte doesn't fit.
        byte[] widerBig = [padding, .. big];
        byte[] widerLittle = [.. little, padding];
        Assert.Equal(padding == 0, TryReadBigEndianVia(widerBig, isUnsigned: true, out NativeCULong fromWiderBig));
        Assert.Equal(padding == 0, TryReadLittleEndianVia(widerLittle, isUnsigned: true, out NativeCULong fromWiderLittle));
        if (padding == 0)
        {
            Assert.Equal(x, fromWiderBig);
            Assert.Equal(x, fromWiderLittle);
        }
    }

    [Property]
    public void WriteEndian_ToAShortDestination_WritesNothing(NativeCULong x, NonNegativeInt length)
    {
        byte[] destination = new byte[length.Get % (Width / 8)];
        Assert.False(TryWriteBigEndianVia(x, destination, out int written));
        Assert.Equal(0, written);
        Assert.False(TryWriteLittleEndianVia(x, destination, out written));
        Assert.Equal(0, written);
        Assert.Throws<ArgumentException>(() => WriteBigEndianVia(x, destination));
    }

    //
    // Equality, hashing and ordering agree with the value, and with each other.
    //

    [Property]
    public void Equality_Hashing_Ordering_AgreeWithTheValue(NativeCULong x, NativeCULong y)
    {
        int order = x.Value.CompareTo(y.Value);
        bool equal = order == 0;

        Assert.Equal(equal, x.Equals(y));
        Assert.Equal(equal, x.Equals((object)y));
        Assert.Equal(equal, x == y);
        Assert.Equal(!equal, x != y);
        Assert.Equal(equal, EqualityComparer<NativeCULong>.Default.Equals(x, y));
        if (equal)
        {
            Assert.Equal(x.GetHashCode(), y.GetHashCode());
            Assert.Equal(EqualityComparer<NativeCULong>.Default.GetHashCode(x), EqualityComparer<NativeCULong>.Default.GetHashCode(y));
        }

        Assert.Equal(Math.Sign(order), Math.Sign(x.CompareTo(y)));
        Assert.Equal(-Math.Sign(order), Math.Sign(y.CompareTo(x)));
        Assert.Equal(Math.Sign(order), Math.Sign(x.CompareTo((object)y)));
        Assert.Equal(Math.Sign(order), Math.Sign(Comparer<NativeCULong>.Default.Compare(x, y)));
        Assert.Equal(order < 0, x < y);
        Assert.Equal(order <= 0, x <= y);
        Assert.Equal(order > 0, x > y);
        Assert.Equal(order >= 0, x >= y);
        Assert.Equal(order <= 0 ? x : y, NativeCULong.Min(x, y));
        Assert.Equal(order >= 0 ? x : y, NativeCULong.Max(x, y));
    }

    // A boxed value of another type is never equal, and isn't comparable; null sorts first.
    [Property]
    public void Equality_WithAnotherType_IsFalse(NativeCULong x)
    {
        Assert.False(x.Equals((object)x.Value));
        Assert.False(x.Equals((object)(ulong)x));
        // Through a variable: Equals(object) must answer null with false, which is what this checks,
        // and a literal null would be flagged as a comparison that can never be true.
        object? none = null;
        Assert.False(x.Equals(none));
        Assert.Equal(1, x.CompareTo(none));
        Assert.Throws<ArgumentException>(() => x.CompareTo((object)(ulong)x));
    }

    // Sorting and hashing whole collections agree with doing it to the values.
    [Property]
    public void Sorting_And_Sets_AgreeWithTheValues(NativeCULong[] values)
    {
        NativeCULong[] sorted = [.. values];
        Array.Sort(sorted);
        Assert.Equal(values.Select(v => v.Value).Order(), sorted.Select(v => v.Value));

        Assert.Equal(values.Select(v => v.Value).Distinct().Count(), new HashSet<NativeCULong>(values).Count);
    }

    //
    // Arithmetic is modulo 2^Width, and checked arithmetic throws exactly when the true result doesn't fit.
    //

    [Property]
    public void Arithmetic_IsModulo2ToTheWidth(NativeCULong x, NativeCULong y)
    {
        UInt128 modulus = UInt128.One << Width;
        UInt128 a = (ulong)x, b = (ulong)y;

        Assert.Equal((ulong)((a + b) % modulus), (ulong)unchecked(x + y));
        Assert.Equal((ulong)((a + modulus - b) % modulus), (ulong)unchecked(x - y));
        Assert.Equal((ulong)(a * b % modulus), (ulong)unchecked(x * y));
        Assert.Equal((ulong)((modulus - a) % modulus), (ulong)NegateVia(x));
        Assert.Equal(x, unchecked(x + y - y));
        Assert.Equal(x, unchecked(x - y + y));
        Assert.Equal(x, NegateVia(NegateVia(x)));
        Assert.Equal(x, ~~x);
        Assert.Equal(NativeCULong.MinValue, x ^ x);
        Assert.Equal(x, x * MultiplicativeIdentityVia<NativeCULong>());
        Assert.Equal(x, x + AdditiveIdentityVia<NativeCULong>());
    }

    [Property]
    public void CheckedArithmetic_ThrowsExactlyWhenItDoesNotFit(NativeCULong x, NativeCULong y)
    {
        UInt128 a = (ulong)x, b = (ulong)y;

        // The project compiles checked, and the true result of a case that doesn't fit is never compared,
        // so it is narrowed unchecked.
        Fits(a + b <= Max, unchecked((ulong)(a + b)), () => checked(x + y));
        Fits(a >= b, unchecked((ulong)(a - b)), () => checked(x - y));
        Fits(a * b <= Max, unchecked((ulong)(a * b)), () => checked(x * y));
        Fits(a == 0, 0, () => CheckedNegateVia(x));
        Fits(a < Max, unchecked((ulong)(a + 1)), () => { NativeCULong t = x; checked { t++; } return t; });
        Fits(a > 0, unchecked((ulong)(a - 1)), () => { NativeCULong t = x; checked { t--; } return t; });

        static void Fits(bool fits, ulong expected, Func<NativeCULong> operation)
        {
            if (fits)
                Assert.Equal(expected, (ulong)operation());
            else
                Assert.Throws<OverflowException>(() => operation());
        }
    }

    [Property]
    public void Division_IsEuclidean(NativeCULong x, NativeCULong y)
    {
        if (y == NativeCULong.MinValue)
        {
            Assert.Throws<DivideByZeroException>(() => x / y);
            Assert.Throws<DivideByZeroException>(() => x % y);
            Assert.Throws<DivideByZeroException>(() => NativeCULong.DivRem(x, y));
            Assert.Throws<DivideByZeroException>(() => CheckedDivideVia(x, y));
            return;
        }

        (NativeCULong quotient, NativeCULong remainder) = NativeCULong.DivRem(x, y);
        Assert.Equal(quotient, x / y);
        Assert.Equal(quotient, CheckedDivideVia(x, y));
        Assert.Equal(remainder, x % y);
        Assert.True(remainder < y);
        Assert.Equal((UInt128)(ulong)x, ((UInt128)(ulong)quotient * (ulong)y) + (ulong)remainder);
    }

    //
    // Bits.
    //

    [Property]
    public void Shifts_And_Rotations_AreModuloTheWidth(NativeCULong x, int shift)
    {
        int s = shift & (Width - 1);
        UInt128 modulus = UInt128.One << Width;
        ulong a = (ulong)x;

        Assert.Equal((ulong)(((UInt128)a << s) % modulus), (ulong)(x << shift));
        Assert.Equal(a >> s, (ulong)(x >> shift));
        Assert.Equal(a >> s, (ulong)(x >>> shift));
        Assert.Equal(x, NativeCULong.RotateRight(NativeCULong.RotateLeft(x, shift), shift));
        Assert.Equal(NativeCULong.RotateLeft(x, shift), NativeCULong.RotateRight(x, Width - s));
        Assert.Equal(NativeCULong.PopCount(x), NativeCULong.PopCount(NativeCULong.RotateLeft(x, shift)));
    }

    [Property]
    public void BitCounts_AreConsistent(NativeCULong x)
    {
        int popCount = (int)(ulong)NativeCULong.PopCount(x);
        int leadingZeros = (int)(ulong)NativeCULong.LeadingZeroCount(x);
        int trailingZeros = (int)(ulong)NativeCULong.TrailingZeroCount(x);
        int shortest = GetShortestBitLengthVia(x);

        Assert.Equal(Width, popCount + (int)(ulong)NativeCULong.PopCount(~x));
        Assert.Equal(Width, leadingZeros + shortest);
        Assert.Equal(popCount == 1, NativeCULong.IsPow2(x));
        Assert.Equal(x == NativeCULong.MinValue ? Width : BitOperations.TrailingZeroCount((ulong)x), trailingZeros);
        Assert.Equal(x == NativeCULong.MinValue ? 0 : Width - 1 - leadingZeros, (int)(ulong)NativeCULong.Log2(x));
        Assert.Equal(((ulong)x & 1) == 0, IsEvenVia(x));
        Assert.Equal(((ulong)x & 1) != 0, IsOddVia(x));
    }

    //
    // Conversions agree with the true value: exact when it fits, else truncated modulo 2^Width,
    // saturated at the bounds, or refused when checked.
    //

    // Max and the modulus follow Width, so on 32-bit storage everything from 2^32 up is out of range, and
    // the generator puts values on both sides of 2^32 and of 2^64.
    [Property]
    public void Create_FromUInt128_AgreesWithTheTrueValue(UInt128 v)
    {
        UInt128 modulus = UInt128.One << Width;

        Assert.Equal((ulong)(v % modulus), (ulong)NativeCULong.CreateTruncating(v));
        Assert.Equal(v > Max ? Max : (ulong)v, (ulong)NativeCULong.CreateSaturating(v));
        if (v > Max)
        {
            Assert.Throws<OverflowException>(() => NativeCULong.CreateChecked(v));
        }
        else
        {
            Assert.Equal((ulong)v, (ulong)NativeCULong.CreateChecked(v));
            Assert.Equal(v, UInt128.CreateChecked(NativeCULong.CreateChecked(v)));
        }
    }

    [Property]
    public void Create_FromInt128_AgreesWithTheTrueValue(Int128 v)
    {
        UInt128 modulus = UInt128.One << Width;

        // 2^128 is a multiple of 2^Width, so the two's-complement bit pattern has the value's residue.
        Assert.Equal((ulong)(unchecked((UInt128)v) % modulus), (ulong)NativeCULong.CreateTruncating(v));
        Assert.Equal(v < 0 ? 0UL : v > Max ? Max : (ulong)v, (ulong)NativeCULong.CreateSaturating(v));
        if (v < 0 || v > Max)
        {
            Assert.Throws<OverflowException>(() => NativeCULong.CreateChecked(v));
        }
        else
        {
            Assert.Equal((ulong)v, (ulong)NativeCULong.CreateChecked(v));
            Assert.Equal(v, Int128.CreateChecked(NativeCULong.CreateChecked(v)));
        }
    }

    // A Half is truncated toward zero, then range-checked: -0.5 is 0, but -1.5 is out of range. Every finite
    // Half is below 2^16, so only the negatives, NaN and the infinities are out of range at either width,
    // and saturating sends NaN and the negatives to 0 and +∞ to Max. Truncating saturates too: since .NET 9
    // floating-point to integer conversions saturate on every platform, which is what uint and ulong do.
    [Property]
    public void Create_FromHalf_TruncatesTowardZero(Half h)
    {
        double d = (double)h; // exact
        double truncated = Math.Truncate(d);
        ulong saturated = double.IsNaN(d) || truncated <= 0 ? 0 : d > Max ? Max : (ulong)truncated;

        Assert.Equal(saturated, (ulong)NativeCULong.CreateSaturating(h));
        Assert.Equal(saturated, (ulong)NativeCULong.CreateTruncating(h));
        if (double.IsFinite(d) && truncated >= 0)
            Assert.Equal((ulong)truncated, (ulong)NativeCULong.CreateChecked(h));
        else
            Assert.Throws<OverflowException>(() => NativeCULong.CreateChecked(h));
    }

    // Int128 and UInt128 hold every value, so every conversion out and back is the identity.
    [Property]
    public void Create_RoundTripsThrough128BitTypes(NativeCULong x)
    {
        Assert.Equal(x, NativeCULong.CreateChecked(Int128.CreateChecked(x)));
        Assert.Equal(x, NativeCULong.CreateSaturating(Int128.CreateSaturating(x)));
        Assert.Equal(x, NativeCULong.CreateTruncating(Int128.CreateTruncating(x)));
        Assert.Equal(x, NativeCULong.CreateChecked(UInt128.CreateChecked(x)));
        Assert.Equal(x, NativeCULong.CreateSaturating(UInt128.CreateSaturating(x)));
        Assert.Equal(x, NativeCULong.CreateTruncating(UInt128.CreateTruncating(x)));
    }

    // A value Half holds exactly (an integer of at most 11 significant bits, up to Half.MaxValue = 65504)
    // comes back unchanged; a non-negative integral Half survives the trip the other way. x seldom lands on
    // such a value above 300, so the value h converts to is tried as well.
    [Property]
    public void Create_RoundTripsThroughHalf_WhenExact(NativeCULong x, Half h)
    {
        foreach (NativeCULong value in new[] { x, NativeCULong.CreateSaturating(h) }.Where(v => IsExactInHalf((ulong)v)))
        {
            Assert.Equal(value, NativeCULong.CreateChecked(Half.CreateChecked(value)));
            Assert.Equal(value, NativeCULong.CreateSaturating(Half.CreateSaturating(value)));
            Assert.Equal(value, NativeCULong.CreateTruncating(Half.CreateTruncating(value)));
        }

        if (Half.IsFinite(h) && Half.IsInteger(h) && h >= Half.Zero)
            Assert.Equal(h, Half.CreateChecked(NativeCULong.CreateChecked(h)));

        static bool IsExactInHalf(ulong a) =>
            a == 0 || (a <= 65504 && a >> BitOperations.TrailingZeroCount(a) < 2048);
    }

    // Every value survives a trip through a wider or exact type and back (Int128 and UInt128 above).
    [Property]
    public void Create_RoundTripsThroughExactTypes(NativeCULong x)
    {
        Assert.Equal(x, NativeCULong.CreateChecked(ulong.CreateChecked(x)));
        Assert.Equal(x, NativeCULong.CreateChecked(decimal.CreateChecked(x)));
        Assert.Equal(x, NativeCULong.CreateChecked(nuint.CreateChecked(x)));
        if ((ulong)x <= 1UL << 53)
            Assert.Equal(x, NativeCULong.CreateChecked(double.CreateChecked(x)));
    }

    [Property]
    public void Clamp_StaysWithinTheBounds(NativeCULong x, NativeCULong a, NativeCULong b)
    {
        if (a > b)
        {
            Assert.Throws<ArgumentException>(() => NativeCULong.Clamp(x, a, b));
            return;
        }

        NativeCULong clamped = NativeCULong.Clamp(x, a, b);
        Assert.InRange(clamped, a, b);
        Assert.Equal(x < a ? a : x > b ? b : x, clamped);
    }

    //
    // Generic-math members, reached through a type parameter as generic code reaches them. The explicit
    // interface implementations and the interfaces' default implementations are only reachable this way.
    //

    private static T ParseVia<T>(string text, IFormatProvider provider, NumberStyles style)
        where T : INumberBase<T>
        => T.Parse(text, style, provider);

    private static bool FormatUtf8Via<T>(T value, Span<byte> destination, out int bytesWritten, string format, IFormatProvider provider)
        where T : IUtf8SpanFormattable
        => value.TryFormat(destination, out bytesWritten, format, provider);

    private static int GetByteCountVia<T>(T value)
        where T : IBinaryInteger<T>
        => value.GetByteCount();

    private static int GetShortestBitLengthVia<T>(T value)
        where T : IBinaryInteger<T>
        => value.GetShortestBitLength();

    private static int WriteBigEndianVia<T>(T value, byte[] destination)
        where T : IBinaryInteger<T>
        => value.WriteBigEndian(destination);

    private static int WriteLittleEndianVia<T>(T value, byte[] destination)
        where T : IBinaryInteger<T>
        => value.WriteLittleEndian(destination);

    private static bool TryWriteBigEndianVia<T>(T value, Span<byte> destination, out int bytesWritten)
        where T : IBinaryInteger<T>
        => value.TryWriteBigEndian(destination, out bytesWritten);

    private static bool TryWriteLittleEndianVia<T>(T value, Span<byte> destination, out int bytesWritten)
        where T : IBinaryInteger<T>
        => value.TryWriteLittleEndian(destination, out bytesWritten);

    private static T ReadBigEndianVia<T>(byte[] source, bool isUnsigned)
        where T : IBinaryInteger<T>
        => T.ReadBigEndian(source, isUnsigned);

    private static T ReadLittleEndianVia<T>(byte[] source, bool isUnsigned)
        where T : IBinaryInteger<T>
        => T.ReadLittleEndian(source, isUnsigned);

    private static bool TryReadBigEndianVia<T>(ReadOnlySpan<byte> source, bool isUnsigned, out T value)
        where T : IBinaryInteger<T>
        => T.TryReadBigEndian(source, isUnsigned, out value);

    private static bool TryReadLittleEndianVia<T>(ReadOnlySpan<byte> source, bool isUnsigned, out T value)
        where T : IBinaryInteger<T>
        => T.TryReadLittleEndian(source, isUnsigned, out value);

    private static T NegateVia<T>(T value)
        where T : IUnaryNegationOperators<T, T>
        => unchecked(-value);

    private static T CheckedNegateVia<T>(T value)
        where T : IUnaryNegationOperators<T, T>
        => checked(-value);

    private static T CheckedDivideVia<T>(T left, T right)
        where T : IDivisionOperators<T, T, T>
        => checked(left / right);

    private static T MultiplicativeIdentityVia<T>()
        where T : IMultiplicativeIdentity<T, T>
        => T.MultiplicativeIdentity;

    private static T AdditiveIdentityVia<T>()
        where T : IAdditiveIdentity<T, T>
        => T.AdditiveIdentity;

    private static bool IsEvenVia<T>(T value)
        where T : INumberBase<T>
        => T.IsEvenInteger(value);

    private static bool IsOddVia<T>(T value)
        where T : INumberBase<T>
        => T.IsOddInteger(value);
}
