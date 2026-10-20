// Licensed under the MIT License

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

namespace KerckhoffsLabs.Runtime.InteropServices.Tests;

public partial class NativeCULongTests
{
    //
    // ToString(format) / ToString(provider)
    //

    [Theory]
    [InlineData("D", "42")]
    [InlineData("X", "2A")]
    [InlineData("X4", "002A")]
    [InlineData("D5", "00042")]
    public void ToString_Format_FormatsValue(string format, string expected)
    {
        NativeCULong v = new(42u);
        Assert.Equal(expected, v.ToString(format));
    }

    [Fact]
    public void ToString_Provider_UsesProvider()
    {
        // Default integer ToString does not interpolate culture-specific separators,
        // but the (IFormatProvider) overload still dispatches through the BCL.
        NativeCULong v = new(1234567u);
        Assert.Equal("1234567", v.ToString(CultureInfo.InvariantCulture));
    }

    //
    // TryFormat (ISpanFormattable)
    //

    [Fact]
    public void TryFormat_FitsDestination_WritesAndReturnsTrue()
    {
        Span<char> destination = stackalloc char[16];
        NativeCULong v = new(42u);
        Assert.True(v.TryFormat(destination, out int written, default, CultureInfo.InvariantCulture));
        Assert.Equal(2, written);
        Assert.Equal("42", destination[..written].ToString());
    }

    [Fact]
    public void TryFormat_HexFormat_WritesHex()
    {
        Span<char> destination = stackalloc char[16];
        NativeCULong v = new(0xCAFEu);
        Assert.True(v.TryFormat(destination, out int written, "X", CultureInfo.InvariantCulture));
        Assert.Equal(4, written);
        Assert.Equal("CAFE", destination[..written].ToString());
    }

    [Fact]
    public void TryFormat_DestinationTooSmall_ReturnsFalse()
    {
        Span<char> destination = stackalloc char[1]; // 1 char is too small for "42"
        NativeCULong v = new(42u);
        Assert.False(v.TryFormat(destination, out int written, default, CultureInfo.InvariantCulture));
        Assert.Equal(0, written);
    }

    //
    // Parse / TryParse — IParsable<TSelf>
    //

    [Theory]
    [InlineData("0", 0u)]
    [InlineData("42", 42u)]
    [InlineData("4294967295", uint.MaxValue)]
    public void Parse_String_DefaultStyle_RoundTrips(string input, uint expected)
    {
        NativeCULong v = NativeCULong.Parse(input, CultureInfo.InvariantCulture);
        Assert.Equal(expected, (uint)v.Value);
    }

    [Theory]
    [InlineData("FF", 0xFFu)]
    [InlineData("CAFE", 0xCAFEu)]
    [InlineData("ffffffff", uint.MaxValue)]
    public void Parse_String_HexStyle_RoundTrips(string input, uint expected)
    {
        NativeCULong v = NativeCULong.Parse(input, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        Assert.Equal(expected, (uint)v.Value);
    }

    [Fact]
    public void Parse_InvalidInput_ThrowsFormatException() => Assert.Throws<FormatException>(() => NativeCULong.Parse("not-a-number", CultureInfo.InvariantCulture));

    [Theory]
    [InlineData("0", 0u)]
    [InlineData("42", 42u)]
    public void TryParse_String_DefaultStyle_Success(string input, uint expected)
    {
        Assert.True(NativeCULong.TryParse(input, CultureInfo.InvariantCulture, out NativeCULong v));
        Assert.Equal(expected, (uint)v.Value);
    }

    [Theory]
    [InlineData("FF", 0xFFu)]
    [InlineData("CAFE", 0xCAFEu)]
    public void TryParse_String_HexStyle_Success(string input, uint expected)
    {
        Assert.True(NativeCULong.TryParse(input, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out NativeCULong v));
        Assert.Equal(expected, (uint)v.Value);
    }

    [Fact]
    public void TryParse_InvalidInput_ReturnsFalseAndDefault()
    {
        Assert.False(NativeCULong.TryParse("invalid", CultureInfo.InvariantCulture, out NativeCULong v));
        Assert.Equal(default, v);
    }

    [Fact]
    public void TryParse_NullString_ReturnsFalse()
    {
        Assert.False(NativeCULong.TryParse((string?)null, CultureInfo.InvariantCulture, out NativeCULong v));
        Assert.Equal(default, v);
    }

    //
    // Parse / TryParse — ISpanParsable<TSelf>
    //

    [Theory]
    [InlineData("0", 0u)]
    [InlineData("42", 42u)]
    [InlineData("4294967295", uint.MaxValue)]
    public void Parse_Span_DefaultStyle_RoundTrips(string input, uint expected)
    {
        NativeCULong v = NativeCULong.Parse(input.AsSpan(), CultureInfo.InvariantCulture);
        Assert.Equal(expected, (uint)v.Value);
    }

    [Fact]
    public void Parse_Span_HexStyle_RoundTrips()
    {
        NativeCULong v = NativeCULong.Parse("CAFE".AsSpan(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        Assert.Equal(0xCAFEu, (uint)v.Value);
    }

    [Fact]
    public void TryParse_Span_DefaultStyle_Success()
    {
        Assert.True(NativeCULong.TryParse("123".AsSpan(), CultureInfo.InvariantCulture, out NativeCULong v));
        Assert.Equal(123u, (uint)v.Value);
    }

    [Fact]
    public void TryParse_Span_HexStyle_Success()
    {
        Assert.True(NativeCULong.TryParse("DEAD".AsSpan(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out NativeCULong v));
        Assert.Equal(0xDEADu, (uint)v.Value);
    }

    [Fact]
    public void TryParse_Span_InvalidInput_ReturnsFalseAndDefault()
    {
        Assert.False(NativeCULong.TryParse("invalid".AsSpan(), CultureInfo.InvariantCulture, out NativeCULong v));
        Assert.Equal(default, v);
    }

    [Fact]
    public void Parse_Span_InvalidInput_ThrowsFormatException() => Assert.Throws<FormatException>(() => NativeCULong.Parse("not-a-number".AsSpan(), CultureInfo.InvariantCulture));

    //
    // TryParse — overflow paths. Pinning behavior so future refactors can't accidentally
    // change "fail-on-overflow" into wrap or saturate.
    //

    [Fact]
    public void TryParse_String_Overflow_ReturnsFalseAndDefault()
    {
        // 99999999999999999999 is wider than ulong → wider than any NativeCULong storage.
        Assert.False(NativeCULong.TryParse("99999999999999999999", CultureInfo.InvariantCulture, out NativeCULong v));
        Assert.Equal(default, v);
    }

    [Fact]
    public void TryParse_Span_Overflow_ReturnsFalseAndDefault()
    {
        Assert.False(NativeCULong.TryParse("99999999999999999999".AsSpan(), CultureInfo.InvariantCulture, out NativeCULong v));
        Assert.Equal(default, v);
    }

    //
    // IUtf8SpanFormattable. NativeCULong declares the interface but provides no
    // explicit TryFormat(Span&lt;byte&gt;, ...) — the BCL supplies the default interface
    // method that transcodes the ISpanFormattable (char) output. These tests pin
    // that the contract is actually wired up and produces the right bytes.
    //

    [Fact]
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance",
        Justification = "v is intentionally typed as the interface so the test exercises the BCL default interface method that transcodes ISpanFormattable to UTF-8; using the concrete type would bind a different code path and defeat the test.")]
    public void Utf8TryFormat_FitsDestination_WritesBytesAndReturnsTrue()
    {
        IUtf8SpanFormattable v = new NativeCULong(12345u);
        Span<byte> destination = stackalloc byte[16];
        Assert.True(v.TryFormat(destination, out int written, default, CultureInfo.InvariantCulture));
        Assert.Equal(5, written);
        Assert.Equal(new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35 }, destination[..written].ToArray());
    }

    [Fact]
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance",
        Justification = "v is intentionally typed as the interface so the test exercises the BCL default interface method that transcodes ISpanFormattable to UTF-8; using the concrete type would bind a different code path and defeat the test.")]
    public void Utf8TryFormat_HexFormat_WritesHexBytes()
    {
        IUtf8SpanFormattable v = new NativeCULong(0xCAFEu);
        Span<byte> destination = stackalloc byte[16];
        Assert.True(v.TryFormat(destination, out int written, "X", CultureInfo.InvariantCulture));
        Assert.Equal(4, written);
        Assert.Equal([(byte)'C', (byte)'A', (byte)'F', (byte)'E'], destination[..written].ToArray());
    }

    [Fact]
    [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance",
        Justification = "v is intentionally typed as the interface so the test exercises the BCL default interface method that transcodes ISpanFormattable to UTF-8; using the concrete type would bind a different code path and defeat the test.")]
    public void Utf8TryFormat_DestinationTooSmall_ReturnsFalse()
    {
        IUtf8SpanFormattable v = new NativeCULong(42u);
        Span<byte> destination = stackalloc byte[1]; // 1 byte is too small for "42"
        Assert.False(v.TryFormat(destination, out int written, default, CultureInfo.InvariantCulture));
        Assert.Equal(0, written);
    }

    //
    // Parse / TryParse — IUtf8SpanParsable<TSelf>
    //

    [Fact]
    public void Parse_Utf8_DefaultStyle_RoundTrips() => Assert.Equal(new NativeCULong(42u), NativeCULong.Parse("42"u8, CultureInfo.InvariantCulture));

    [Fact]
    public void Parse_Utf8_HexStyle_RoundTrips() => Assert.Equal(new NativeCULong(0xFFu), NativeCULong.Parse("FF"u8, NumberStyles.HexNumber, CultureInfo.InvariantCulture));

    [Fact]
    public void Parse_Utf8_InvalidInput_ThrowsFormatException() => Assert.Throws<FormatException>(() => NativeCULong.Parse("not-a-number"u8, CultureInfo.InvariantCulture));

    [Fact]
    public void TryParse_Utf8_DefaultStyle_Success()
    {
        Assert.True(NativeCULong.TryParse("4294967295"u8, CultureInfo.InvariantCulture, out NativeCULong v));
        Assert.Equal(new NativeCULong(0xFFFFFFFFu), v);
    }

    [Fact]
    public void TryParse_Utf8_InvalidInput_ReturnsFalseAndDefault()
    {
        Assert.False(NativeCULong.TryParse("invalid"u8, NumberStyles.Integer, CultureInfo.InvariantCulture, out NativeCULong v));
        Assert.Equal(default, v);
    }

    // fr-FR's group separator is a narrow no-break space (U+202F). The BCL's UTF-16 parser also accepts
    // an ordinary space in its place, and its UTF-8 parser doesn't; NativeCULong's UTF-8 parse must give
    // the storage type's UTF-8 answer, not the UTF-16 one that INumberBase's default implementation,
    // which transcodes, would give.
    [Fact]
    public void TryParse_Utf8_FrenchOrdinarySpace_MatchesStorageTypeUtf8()
    {
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");
        bool expected = uint.TryParse("1 000"u8, NumberStyles.AllowThousands, french, out _);
        Assert.Equal(expected, NativeCULong.TryParse("1 000"u8, NumberStyles.AllowThousands, french, out _));
        Assert.Equal(expected, GenericTryParse<NativeCULong>("1 000"u8, NumberStyles.AllowThousands, french));
    }

    // Through INumberBase<T>, as generic code reaches it.
    private static bool GenericTryParse<T>(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider provider)
        where T : struct, INumberBase<T>
        => T.TryParse(utf8Text, style, provider, out _);
}
