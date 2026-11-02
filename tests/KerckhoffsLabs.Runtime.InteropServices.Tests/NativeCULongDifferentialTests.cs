// Licensed under the MIT License
// Differential property tests: NativeCULong must behave exactly like the BCL unsigned integer of its
// storage width (uint where the storage is 32-bit, ulong where it is 64-bit), on every input. That
// gives each member an oracle, so CsCheck can generate inputs instead of the tests listing them: an
// operation is applied to the same value held both ways, and the outcomes, the result as invariant
// text or the type of the exception thrown, must be equal. On a failure CsCheck shrinks the input
// to a minimal case and reports the seed that reproduces it.
//
// The oracle is uint/ulong rather than the storage type itself (uint or nuint) because much of
// NativeCULong delegates to its storage type: comparing against that would only re-run the same code.
//
// The suite runs once per storage width (see the NativeCULongNarrow switch in the .csproj), and
// Has32BitStorage picks the oracle to match.

using System.Globalization;
using System.Numerics;
using System.Text;
using CsCheck;
using static KerckhoffsLabs.Runtime.InteropServices.Tests.PlatformLayout;

namespace KerckhoffsLabs.Runtime.InteropServices.Tests;

public sealed class NativeCULongDifferentialTests
{
    private const long Iterations = 2_000;

    // Bit patterns, weighted toward the edges of either storage width, where off-by-one and
    // truncation bugs live. Read through Oracle and Subject, each keeps the low bits that fit.
    private static readonly Gen<ulong> GenBits = Gen.Frequency(
        (4, Gen.ULong),
        (2, Gen.ULong[0, 300]),
        (3, Gen.OneOfConst(
            0UL, 1UL, 2UL, 0x7FUL, 0x80UL, 0xFFUL, 0x7FFF_FFFFUL, 0x8000_0000UL, 0xFFFF_FFFEUL, 0xFFFF_FFFFUL,
            0x1_0000_0000UL, 0x7FFF_FFFF_FFFF_FFFFUL, 0x8000_0000_0000_0000UL, ulong.MaxValue - 1, ulong.MaxValue)));

    private static readonly Gen<int> GenShift = Gen.Frequency((3, Gen.Int[-1, 65]), (1, Gen.Int));

    private static readonly Gen<double> GenDouble = Gen.Frequency(
        (3, Gen.Double),
        (2, GenBits.Select(b => (double)b)),
        (2, Gen.Double[-1e20, 1e20]),
        (2, Gen.OneOfConst(
            double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.Epsilon, -0.0, 0.5, -0.5, -1.0,
            4294967295.5, 4294967296.0, 18446744073709551615.0, 1.8446744073709552E19, double.MaxValue, double.MinValue)));

    private static readonly Gen<decimal> GenDecimal = Gen.Frequency(
        (3, Gen.Decimal),
        (2, GenBits.Select(b => (decimal)b)),
        (1, Gen.OneOfConst(decimal.MinValue, decimal.MaxValue, -1m, -0.5m, 0.5m, 4294967295.5m, 18446744073709551615.5m)));

    private static readonly CultureInfo[] Cultures =
    [
        CultureInfo.InvariantCulture,
        CultureInfo.GetCultureInfo("en-US"),
        CultureInfo.GetCultureInfo("fr-FR"),
        CultureInfo.GetCultureInfo("de-CH"),
        CultureInfo.GetCultureInfo("ar-EG"),
    ];

    private static readonly Gen<CultureInfo> GenCulture = Gen.OneOfConst(Cultures);

    // Valid and invalid styles alike: an invalid combination must throw ArgumentException both ways.
    private static readonly Gen<NumberStyles> GenStyle = Gen.OneOfConst(
        NumberStyles.None, NumberStyles.Integer, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
        NumberStyles.AllowLeadingSign, NumberStyles.AllowTrailingSign, NumberStyles.AllowParentheses,
        NumberStyles.AllowThousands, NumberStyles.AllowDecimalPoint, NumberStyles.AllowExponent,
        NumberStyles.AllowCurrencySymbol, NumberStyles.Number, NumberStyles.Float, NumberStyles.Currency,
        NumberStyles.Any, NumberStyles.HexNumber, NumberStyles.AllowHexSpecifier, NumberStyles.BinaryNumber,
        NumberStyles.AllowBinarySpecifier, NumberStyles.HexNumber | NumberStyles.AllowLeadingSign,
        (NumberStyles)0x10000);

    // Text a parser has to think about: digits (ASCII and Arabic-Indic), hex and binary digits, signs,
    // separators of the cultures above, currency symbols, parentheses, exponents and white space.
    private static readonly Gen<string> GenNumberText = Gen.Frequency(
        (4, Gen.Char["0123456789abcdefABCDEF+-.,' _()$€eE\t  −٠١٩"].Array[0, 24].Select(chars => new string(chars))),
        (3, GenBits.Select(b => b.ToString(CultureInfo.InvariantCulture))),
        (1, GenBits.Select(b => b.ToString("X", CultureInfo.InvariantCulture))),
        (1, GenBits.Select(b => b.ToString("N0", CultureInfo.GetCultureInfo("fr-FR")))),
        (2, Gen.OneOfConst(
            "", " ", "-0", "+0", "-1", "0x10", "1_000", "4294967295", "4294967296", "18446744073709551615",
            "18446744073709551616", "1,000", "1 000", "1e3", "1E+3", "1.0", "1.5", "(1)", "$1", "1-", " 42 ", "١٢")));

    private static readonly Gen<string?> GenFormat = Gen.OneOfConst<string?>(
        null, "", "D", "d", "D0", "D3", "D25", "X", "x", "X0", "X17", "N", "N0", "N3", "n", "C", "C0", "E", "e2",
        "G", "G0", "G3", "G25", "F", "F0", "F3", "P", "P0", "B", "b", "B40", "R", "0", "0000", "#,##0.00", "#",
        "%", "0;(0);zero", "Z", "D1000000000");

    //
    // Operators and the IBinaryInteger statics.
    //

    [Fact]
    public void BinaryOperations_MatchOracle() => ForStorage(new BinaryOperations());

    [Fact]
    public void ShiftsAndRotations_MatchOracle() => ForStorage(new ShiftsAndRotations());

    [Fact]
    public void UnaryOperations_MatchOracle() => ForStorage(new UnaryOperations());

    //
    // Bytes.
    //

    [Fact]
    public void TryWriteEndian_MatchesOracle() => ForStorage(new WriteEndian());

    [Fact]
    public void TryReadEndian_MatchesOracle() => ForStorage(new ReadEndian());

    //
    // Conversions: CreateChecked/Saturating/Truncating both ways, and the explicit casts.
    //

    [Fact]
    public void CreateFromPrimitive_MatchesOracle() => ForStorage(new CreateFrom());

    [Fact]
    public void CreateToNumber_MatchesOracle() => ForStorage(new CreateTo());

    [Fact]
    public void ExplicitCasts_MatchOracle() => ForStorage(new Casts());

    //
    // Text.
    //

    [Fact]
    public void Parse_MatchesOracle() => ForStorage(new Parsing());

    [Fact]
    public void Format_MatchesOracle() => ForStorage(new Formatting());

    //
    // Harness.
    //

    private interface IProperty
    {
        void Check<TOracle>()
            where TOracle : struct, IBinaryInteger<TOracle>, IUnsignedNumber<TOracle>, IMinMaxValue<TOracle>;
    }

    private static void ForStorage(IProperty property)
    {
        if (Has32BitStorage)
        {
            property.Check<uint>();
        }
        else
        {
            property.Check<ulong>();
        }
    }

    private static TOracle Oracle<TOracle>(ulong bits)
        where TOracle : IBinaryInteger<TOracle>
        => TOracle.CreateTruncating(bits);

    // Built through the constructor, the most direct way to set the storage, so that no conversion
    // under test is involved.
    private static NativeCULong Subject(ulong bits) =>
#if WINDOWS
        new(unchecked((uint)bits));
#else
        new(unchecked((nuint)bits));
#endif

    // Only the exceptions an integer operation is documented to throw are outcomes to compare: overflow
    // and division by zero (ArithmeticException), unparsable text (FormatException), an invalid style,
    // format or argument (ArgumentException) and an unsupported conversion (NotSupportedException). Any
    // other exception is a bug in itself and fails the test as it is.
    private static string Outcome(Func<object?> operation)
    {
        try
        {
            return Show(operation());
        }
        catch (Exception e) when (e is ArithmeticException or FormatException or ArgumentException or NotSupportedException)
        {
            return e.GetType().Name;
        }
    }

    private static string Show(object? value) => value switch
    {
        null => "null",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "null",
    };

    private static void Same(string what, Func<object?> oracle, Func<object?> subject)
        => Assert.Equal($"{what}: {Outcome(oracle)}", $"{what}: {Outcome(subject)}");

    // One table of operations, instantiated for the oracle and for NativeCULong in the same order.
    private static class Ops<T>
        where T : IBinaryInteger<T>, IUnsignedNumber<T>, IMinMaxValue<T>
    {
        public static readonly (string Name, Func<T, T, object?> Apply)[] Binary =
        [
            ("x + y", (x, y) => unchecked(x + y)),
            ("checked(x + y)", (x, y) => checked(x + y)),
            ("x - y", (x, y) => unchecked(x - y)),
            ("checked(x - y)", (x, y) => checked(x - y)),
            ("x * y", (x, y) => unchecked(x * y)),
            ("checked(x * y)", (x, y) => checked(x * y)),
            ("x / y", (x, y) => unchecked(x / y)),
            ("checked(x / y)", (x, y) => checked(x / y)),
            ("x % y", (x, y) => x % y),
            ("x & y", (x, y) => x & y),
            ("x | y", (x, y) => x | y),
            ("x ^ y", (x, y) => x ^ y),
            ("x == y", (x, y) => x == y),
            ("x != y", (x, y) => x != y),
            ("x < y", (x, y) => x < y),
            ("x <= y", (x, y) => x <= y),
            ("x > y", (x, y) => x > y),
            ("x >= y", (x, y) => x >= y),
            ("x.CompareTo(y)", (x, y) => Math.Sign(x.CompareTo(y))),
            ("x.CompareTo((object)y)", (x, y) => Math.Sign(x.CompareTo((object)y))),
            ("x.Equals(y)", (x, y) => x.Equals(y)),
            ("DivRem(x, y)", (x, y) => T.DivRem(x, y)),
            ("Min(x, y)", (x, y) => T.Min(x, y)),
            ("Max(x, y)", (x, y) => T.Max(x, y)),
            ("MinMagnitude(x, y)", (x, y) => T.MinMagnitude(x, y)),
            ("MaxMagnitude(x, y)", (x, y) => T.MaxMagnitude(x, y)),
            ("MinMagnitudeNumber(x, y)", (x, y) => T.MinMagnitudeNumber(x, y)),
            ("MaxMagnitudeNumber(x, y)", (x, y) => T.MaxMagnitudeNumber(x, y)),
            ("MinNumber(x, y)", (x, y) => T.MinNumber(x, y)),
            ("MaxNumber(x, y)", (x, y) => T.MaxNumber(x, y)),
            ("CopySign(x, y)", (x, y) => T.CopySign(x, y)),
            ("Clamp(2, x, y)", (x, y) => T.Clamp(T.One + T.One, x, y)),
        ];

        public static readonly (string Name, Func<T, int, object?> Apply)[] Shift =
        [
            ("x << s", (x, s) => x << s),
            ("x >> s", (x, s) => x >> s),
            ("x >>> s", (x, s) => x >>> s),
            ("RotateLeft(x, s)", (x, s) => T.RotateLeft(x, s)),
            ("RotateRight(x, s)", (x, s) => T.RotateRight(x, s)),
        ];

        public static readonly (string Name, Func<T, object?> Apply)[] Unary =
        [
            ("~x", x => ~x),
            ("-x", x => unchecked(-x)),
            ("checked(-x)", x => checked(-x)),
            ("+x", x => +x),
            ("x++", x => { T t = x; unchecked { t++; } return t; }),
            ("checked(x++)", x => { T t = x; checked { t++; } return t; }),
            ("x--", x => { T t = x; unchecked { t--; } return t; }),
            ("checked(x--)", x => { T t = x; checked { t--; } return t; }),
            ("LeadingZeroCount(x)", x => T.LeadingZeroCount(x)),
            ("TrailingZeroCount(x)", x => T.TrailingZeroCount(x)),
            ("PopCount(x)", x => T.PopCount(x)),
            ("Log2(x)", x => T.Log2(x)),
            ("Abs(x)", x => T.Abs(x)),
            ("Sign(x)", x => T.Sign(x)),
            ("IsPow2(x)", x => T.IsPow2(x)),
            ("IsZero(x)", x => T.IsZero(x)),
            ("IsEvenInteger(x)", x => T.IsEvenInteger(x)),
            ("IsOddInteger(x)", x => T.IsOddInteger(x)),
            ("IsNegative(x)", x => T.IsNegative(x)),
            ("IsPositive(x)", x => T.IsPositive(x)),
            ("IsCanonical(x)", x => T.IsCanonical(x)),
            ("IsInteger(x)", x => T.IsInteger(x)),
            ("IsFinite(x)", x => T.IsFinite(x)),
            ("IsNaN(x)", x => T.IsNaN(x)),
            ("IsNormal(x)", x => T.IsNormal(x)),
            ("IsSubnormal(x)", x => T.IsSubnormal(x)),
            ("x.GetShortestBitLength()", x => x.GetShortestBitLength()),
            ("x.GetByteCount()", x => x.GetByteCount()),
            ("x.Equals((object)x)", x => x.Equals((object)x)),
            ("x.Equals((object)0)", x => x.Equals((object)0)),
            ("x == MaxValue", x => x == T.MaxValue),
            ("x == MinValue", x => x == T.MinValue),
            ("x.ToString()", x => x.ToString()),
        ];
    }

    private sealed class BinaryOperations : IProperty
    {
        public void Check<TOracle>()
            where TOracle : struct, IBinaryInteger<TOracle>, IUnsignedNumber<TOracle>, IMinMaxValue<TOracle>
        {
            var oracle = Ops<TOracle>.Binary;
            var subject = Ops<NativeCULong>.Binary;
            Gen.Select(GenBits, GenBits).Sample((a, b) =>
            {
                for (int i = 0; i < oracle.Length; i++)
                {
                    Same(oracle[i].Name, () => oracle[i].Apply(Oracle<TOracle>(a), Oracle<TOracle>(b)), () => subject[i].Apply(Subject(a), Subject(b)));
                }
            }, iter: Iterations);
        }
    }

    private sealed class ShiftsAndRotations : IProperty
    {
        public void Check<TOracle>()
            where TOracle : struct, IBinaryInteger<TOracle>, IUnsignedNumber<TOracle>, IMinMaxValue<TOracle>
        {
            var oracle = Ops<TOracle>.Shift;
            var subject = Ops<NativeCULong>.Shift;
            Gen.Select(GenBits, GenShift).Sample((a, s) =>
            {
                for (int i = 0; i < oracle.Length; i++)
                {
                    Same(oracle[i].Name, () => oracle[i].Apply(Oracle<TOracle>(a), s), () => subject[i].Apply(Subject(a), s));
                }
            }, iter: Iterations);
        }
    }

    private sealed class UnaryOperations : IProperty
    {
        public void Check<TOracle>()
            where TOracle : struct, IBinaryInteger<TOracle>, IUnsignedNumber<TOracle>, IMinMaxValue<TOracle>
        {
            var oracle = Ops<TOracle>.Unary;
            var subject = Ops<NativeCULong>.Unary;
            GenBits.Sample(a =>
            {
                for (int i = 0; i < oracle.Length; i++)
                {
                    Same(oracle[i].Name, () => oracle[i].Apply(Oracle<TOracle>(a)), () => subject[i].Apply(Subject(a)));
                }
            }, iter: Iterations);
        }
    }

    private sealed class WriteEndian : IProperty
    {
        public void Check<TOracle>()
            where TOracle : struct, IBinaryInteger<TOracle>, IUnsignedNumber<TOracle>, IMinMaxValue<TOracle>
        {
            Gen.Select(GenBits, Gen.Int[0, 12]).Sample((a, length) =>
            {
                Same("TryWriteBigEndian", () => Write(Oracle<TOracle>(a), length, bigEndian: true), () => Write(Subject(a), length, bigEndian: true));
                Same("TryWriteLittleEndian", () => Write(Oracle<TOracle>(a), length, bigEndian: false), () => Write(Subject(a), length, bigEndian: false));
            }, iter: Iterations);
        }

        // The destination is pre-filled, so a write past bytesWritten shows in the result too.
        private static string Write<T>(T value, int length, bool bigEndian)
            where T : IBinaryInteger<T>
        {
            byte[] destination = new byte[length];
            Array.Fill(destination, (byte)0xA5);
            bool written = bigEndian
                ? value.TryWriteBigEndian(destination, out int bytesWritten)
                : value.TryWriteLittleEndian(destination, out bytesWritten);
            return $"{written} {bytesWritten} {Convert.ToHexString(destination)}";
        }
    }

    private sealed class ReadEndian : IProperty
    {
        public void Check<TOracle>()
            where TOracle : struct, IBinaryInteger<TOracle>, IUnsignedNumber<TOracle>, IMinMaxValue<TOracle>
        {
            Gen.Select(Gen.Byte.Array[0, 12], Gen.Bool).Sample((source, isUnsigned) =>
            {
                Same("TryReadBigEndian", () => Read<TOracle>(source, isUnsigned, bigEndian: true), () => Read<NativeCULong>(source, isUnsigned, bigEndian: true));
                Same("TryReadLittleEndian", () => Read<TOracle>(source, isUnsigned, bigEndian: false), () => Read<NativeCULong>(source, isUnsigned, bigEndian: false));
            }, iter: Iterations);
        }

        private static string Read<T>(byte[] source, bool isUnsigned, bool bigEndian)
            where T : IBinaryInteger<T>
        {
            bool read = bigEndian
                ? T.TryReadBigEndian(source, isUnsigned, out T value)
                : T.TryReadLittleEndian(source, isUnsigned, out value);
            return read ? $"true {Show(value)}" : "false";
        }
    }

    private sealed class CreateFrom : IProperty
    {
        // BigInteger and Complex are left out on purpose: NativeCULong.CreateXxx doesn't accept them
        // (NativeCULongTests.ConversionsFrom pins that), whereas uint and ulong do.
        public void Check<TOracle>()
            where TOracle : struct, IBinaryInteger<TOracle>, IUnsignedNumber<TOracle>, IMinMaxValue<TOracle>
        {
            From<TOracle, byte>(Gen.Byte);
            From<TOracle, sbyte>(Gen.SByte);
            From<TOracle, short>(Gen.Short);
            From<TOracle, ushort>(Gen.UShort);
            From<TOracle, char>(Gen.Char);
            From<TOracle, int>(Gen.Frequency((3, Gen.Int), (1, GenBits.Select(b => unchecked((int)b)))));
            From<TOracle, uint>(GenBits.Select(b => unchecked((uint)b)));
            From<TOracle, long>(GenBits.Select(b => unchecked((long)b)));
            From<TOracle, ulong>(GenBits);
            From<TOracle, nint>(GenBits.Select(b => unchecked((nint)(long)b)));
            From<TOracle, nuint>(GenBits.Select(b => unchecked((nuint)b)));
            From<TOracle, Int128>(Gen.Select(GenBits, GenBits).Select((upper, lower) => new Int128(upper, lower)));
            From<TOracle, UInt128>(Gen.Select(GenBits, GenBits).Select((upper, lower) => new UInt128(upper, lower)));
            From<TOracle, decimal>(GenDecimal);
            From<TOracle, double>(GenDouble);
            From<TOracle, float>(GenDouble.Select(d => (float)d));
            From<TOracle, Half>(GenDouble.Select(d => (Half)d));
        }

        private static void From<TOracle, TSource>(Gen<TSource> source)
            where TOracle : struct, IBinaryInteger<TOracle>
            where TSource : INumberBase<TSource>
            => source.Sample(value =>
            {
                string what = $"from {typeof(TSource).Name} {Show(value)}";
                Same($"CreateChecked {what}", () => TOracle.CreateChecked(value), () => NativeCULong.CreateChecked(value));
                Same($"CreateSaturating {what}", () => TOracle.CreateSaturating(value), () => NativeCULong.CreateSaturating(value));
                Same($"CreateTruncating {what}", () => TOracle.CreateTruncating(value), () => NativeCULong.CreateTruncating(value));
            }, iter: Iterations);
    }

    private sealed class CreateTo : IProperty
    {
        public void Check<TOracle>()
            where TOracle : struct, IBinaryInteger<TOracle>, IUnsignedNumber<TOracle>, IMinMaxValue<TOracle>
        {
            To<TOracle, byte>();
            To<TOracle, sbyte>();
            To<TOracle, short>();
            To<TOracle, ushort>();
            To<TOracle, char>();
            To<TOracle, int>();
            To<TOracle, uint>();
            To<TOracle, long>();
            To<TOracle, ulong>();
            To<TOracle, nint>();
            To<TOracle, nuint>();
            To<TOracle, Int128>();
            To<TOracle, UInt128>();
            To<TOracle, decimal>();
            To<TOracle, double>();
            To<TOracle, float>();
            To<TOracle, Half>();
            To<TOracle, BigInteger>();
            To<TOracle, Complex>();
        }

        private static void To<TOracle, TTarget>()
            where TOracle : struct, IBinaryInteger<TOracle>
            where TTarget : INumberBase<TTarget>
            => GenBits.Sample(a =>
            {
                string what = $"to {typeof(TTarget).Name}";
                Same($"CreateChecked {what}", () => TTarget.CreateChecked(Oracle<TOracle>(a)), () => TTarget.CreateChecked(Subject(a)));
                Same($"CreateSaturating {what}", () => TTarget.CreateSaturating(Oracle<TOracle>(a)), () => TTarget.CreateSaturating(Subject(a)));
                Same($"CreateTruncating {what}", () => TTarget.CreateTruncating(Oracle<TOracle>(a)), () => TTarget.CreateTruncating(Subject(a)));
            }, iter: Iterations);
    }

    // An unchecked cast truncates and a checked one throws OverflowException, as CreateTruncating and
    // CreateChecked do on the oracle; the casts are separate code from CreateXxx, so they are checked here.
    private sealed class Casts : IProperty
    {
        public void Check<TOracle>()
            where TOracle : struct, IBinaryInteger<TOracle>, IUnsignedNumber<TOracle>, IMinMaxValue<TOracle>
        {
            Gen.Select(GenBits, Gen.Int).Sample((a, i) =>
            {
                long l = unchecked((long)a);
                uint u = unchecked((uint)a);
                nuint n = unchecked((nuint)a);
                NativeCULong subject = Subject(a);
                TOracle oracle = Oracle<TOracle>(a);

                Same("(NativeCULong)int", () => TOracle.CreateTruncating(i), () => unchecked((NativeCULong)i));
                Same("checked((NativeCULong)int)", () => TOracle.CreateChecked(i), () => checked((NativeCULong)i));
                Same("(NativeCULong)uint", () => TOracle.CreateTruncating(u), () => unchecked((NativeCULong)u));
                Same("(NativeCULong)long", () => TOracle.CreateTruncating(l), () => unchecked((NativeCULong)l));
                Same("checked((NativeCULong)long)", () => TOracle.CreateChecked(l), () => checked((NativeCULong)l));
                Same("(NativeCULong)ulong", () => TOracle.CreateTruncating(a), () => unchecked((NativeCULong)a));
                Same("checked((NativeCULong)ulong)", () => TOracle.CreateChecked(a), () => checked((NativeCULong)a));
                Same("(NativeCULong)nuint", () => TOracle.CreateTruncating(n), () => unchecked((NativeCULong)n));

                Same("(int)x", () => int.CreateTruncating(oracle), () => unchecked((int)subject));
                Same("checked((int)x)", () => int.CreateChecked(oracle), () => checked((int)subject));
                Same("(uint)x", () => uint.CreateTruncating(oracle), () => unchecked((uint)subject));
                Same("checked((uint)x)", () => uint.CreateChecked(oracle), () => checked((uint)subject));
                Same("(long)x", () => long.CreateTruncating(oracle), () => unchecked((long)subject));
                Same("checked((long)x)", () => long.CreateChecked(oracle), () => checked((long)subject));
                Same("(ulong)x", () => ulong.CreateTruncating(oracle), () => unchecked((ulong)subject));
                Same("checked((ulong)x)", () => ulong.CreateChecked(oracle), () => checked((ulong)subject));
                Same("(nuint)x", () => nuint.CreateTruncating(oracle), () => unchecked((nuint)subject));
            }, iter: Iterations);
        }
    }

    private sealed class Parsing : IProperty
    {
        public void Check<TOracle>()
            where TOracle : struct, IBinaryInteger<TOracle>, IUnsignedNumber<TOracle>, IMinMaxValue<TOracle>
            => Gen.Select(GenNumberText, GenStyle, GenCulture).Sample((text, style, culture) =>
            {
                string what = $"\"{text}\" {style} {culture.Name}";
                Same($"Parse(string) {what}", () => TOracle.Parse(text, style, culture), () => NativeCULong.Parse(text, style, culture));
                Same($"Parse(span) {what}", () => TOracle.Parse(text.AsSpan(), style, culture), () => NativeCULong.Parse(text.AsSpan(), style, culture));
                Same($"TryParse(string) {what}", () => TryParse<TOracle>(text, style, culture), () => TryParse<NativeCULong>(text, style, culture));
                Same($"TryParse(span) {what}", () => TryParseSpan<TOracle>(text, style, culture), () => TryParseSpan<NativeCULong>(text, style, culture));
                Same($"TryParse(UTF-8) {what}", () => TryParseUtf8<TOracle>(text, style, culture), () => TryParseUtf8<NativeCULong>(text, style, culture));
                Same($"Parse(string, provider) {what}", () => TOracle.Parse(text, culture), () => NativeCULong.Parse(text, culture));
                Same($"TryParse(span, provider) {what}", () => TryParseDefault<TOracle>(text, culture), () => TryParseDefault<NativeCULong>(text, culture));
            }, iter: Iterations);

        private static string TryParse<T>(string text, NumberStyles style, IFormatProvider provider)
            where T : INumberBase<T>
            => T.TryParse(text, style, provider, out T? value) ? $"true {Show(value)}" : $"false {Show(value)}";

        private static string TryParseSpan<T>(string text, NumberStyles style, IFormatProvider provider)
            where T : INumberBase<T>
            => T.TryParse(text.AsSpan(), style, provider, out T? value) ? $"true {Show(value)}" : $"false {Show(value)}";

        private static string TryParseUtf8<T>(string text, NumberStyles style, IFormatProvider provider)
            where T : INumberBase<T>
            => T.TryParse(Encoding.UTF8.GetBytes(text), style, provider, out T? value) ? $"true {Show(value)}" : $"false {Show(value)}";

        private static string TryParseDefault<T>(string text, IFormatProvider provider)
            where T : ISpanParsable<T>
            => T.TryParse(text.AsSpan(), provider, out T? value) ? $"true {Show(value)}" : $"false {Show(value)}";
    }

    private sealed class Formatting : IProperty
    {
        public void Check<TOracle>()
            where TOracle : struct, IBinaryInteger<TOracle>, IUnsignedNumber<TOracle>, IMinMaxValue<TOracle>
            => Gen.Select(GenBits, GenFormat, GenCulture, Gen.Int[0, 48]).Sample((a, format, culture, length) =>
            {
                string what = $"\"{format}\" {culture.Name} [{length}]";
                Same($"ToString {what}", () => Oracle<TOracle>(a).ToString(format, culture), () => Subject(a).ToString(format, culture));
                Same($"TryFormat(char) {what}", () => FormatChars(Oracle<TOracle>(a), format, culture, length), () => FormatChars(Subject(a), format, culture, length));
                Same($"TryFormat(UTF-8) {what}", () => FormatUtf8(Oracle<TOracle>(a), format, culture, length), () => FormatUtf8(Subject(a), format, culture, length));
            }, iter: Iterations);

        private static string FormatChars<T>(T value, string? format, IFormatProvider provider, int length)
            where T : ISpanFormattable
        {
            char[] destination = new char[length];
            bool formatted = value.TryFormat(destination, out int charsWritten, format, provider);
            return $"{formatted} {charsWritten} {(formatted ? new string(destination, 0, charsWritten) : "")}";
        }

        private static string FormatUtf8<T>(T value, string? format, IFormatProvider provider, int length)
            where T : IUtf8SpanFormattable
        {
            byte[] destination = new byte[length];
            bool formatted = value.TryFormat(destination, out int bytesWritten, format, provider);
            return $"{formatted} {bytesWritten} {(formatted ? Encoding.UTF8.GetString(destination, 0, bytesWritten) : "")}";
        }
    }
}
