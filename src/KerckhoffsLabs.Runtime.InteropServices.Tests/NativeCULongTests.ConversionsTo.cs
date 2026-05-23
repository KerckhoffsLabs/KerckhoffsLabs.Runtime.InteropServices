// Licensed under the MIT License
// Coverage for INumberBase<T>.Create{Checked,Saturating,Truncating}<NativeCULong>(value)
// — converting OUT of NativeCULong into some other numeric type. The dispatch goes
// through BCL primitive T.CreateXxx → T.TryConvertFromChecked<NativeCULong> (false,
// NativeCULong isn't in T's from-table) → NativeCULong.TryConvertToXxx<T> (our explicit-
// interface impl). The To impls have their own typeof switch handling the signed-integer
// arms directly; other targets delegate to T.TryConvertFromXxx with a ulong source.
//
// From-direction tests (NativeCULong <- TOther) live in NativeCULongTests.ConversionsFrom.cs.

using KerckhoffsLabs.Runtime.InteropServices;

namespace KerckhoffsLabs.Runtime.InteropServices.Tests;

public partial class NativeCULongTests
{
    //
    // CreateChecked: throws on out-of-range.
    //

    [Fact]
    public void CreateChecked_ToByte_FitsRoundTrips()
    {
        Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateChecked<NativeCULong>(new NativeCULong(0xFF)));
    }

    [Fact]
    public void CreateChecked_ToByte_ExceedingThrows()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<byte>.CreateChecked<NativeCULong>(new NativeCULong(0x100)));
    }

    [Fact]
    public void CreateChecked_ToSbyte_FitsRoundTrips()
    {
        Assert.Equal((sbyte)0x7F, NumberBaseHelper<sbyte>.CreateChecked<NativeCULong>(new NativeCULong(0x7Fu)));
    }

    [Fact]
    public void CreateChecked_ToSbyte_ExceedingThrows()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<sbyte>.CreateChecked<NativeCULong>(new NativeCULong(0x80u)));
    }

    [Fact]
    public void CreateChecked_ToShort_FitsRoundTrips()
    {
        Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateChecked<NativeCULong>(new NativeCULong(0x7FFFu)));
    }

    [Fact]
    public void CreateChecked_ToShort_ExceedingThrows()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<short>.CreateChecked<NativeCULong>(new NativeCULong(0x8000u)));
    }

    [Fact]
    public void CreateChecked_ToInt_PositiveRoundTrips()
    {
        Assert.Equal(0x7FFFFFFF, NumberBaseHelper<int>.CreateChecked<NativeCULong>(new NativeCULong(0x7FFFFFFF)));
    }

    [Fact]
    public void CreateChecked_ToInt_LargeValueThrows()
    {
        // Value exceeding int.MaxValue must throw on every platform (uint can hold
        // 0x80000000 even on 32-bit storage).
        Assert.Throws<OverflowException>(() => NumberBaseHelper<int>.CreateChecked<NativeCULong>(new NativeCULong(0x80000000u)));
    }

    [Fact]
    public void CreateChecked_ToLong_FitsRoundTrips()
    {
        Assert.Equal(0x7FFFFFFFL, NumberBaseHelper<long>.CreateChecked<NativeCULong>(new NativeCULong(0x7FFFFFFFu)));
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.Has64BitStorage))]
    public void CreateChecked_ToLong_ExceedingThrowsOn64BitStorage()
    {
        // Only reachable on 64-bit storage — uint storage can never exceed long.MaxValue.
#pragma warning disable CS8778
        NativeCULong tooBig = new NativeCULong((nuint)0x8000_0000_0000_0000UL);
#pragma warning restore CS8778
        Assert.Throws<OverflowException>(() => NumberBaseHelper<long>.CreateChecked<NativeCULong>(tooBig));
    }

    [Fact]
    public void CreateChecked_ToNint_FitsRoundTrips()
    {
        Assert.Equal((nint)42, NumberBaseHelper<nint>.CreateChecked<NativeCULong>(new NativeCULong(42u)));
    }

    [Fact]
    public void CreateChecked_ToInt128_RoundTrips()
    {
        // Int128 strictly contains NativeCULong's range; always exact.
        Assert.Equal((Int128)0xFFFFFFFFu, NumberBaseHelper<Int128>.CreateChecked<NativeCULong>(new NativeCULong(0xFFFFFFFFu)));
    }

    //
    // CreateSaturating: clamps to TOther's bounds on out-of-range.
    //

    [Fact]
    public void CreateSaturating_ToByte_ExceedingSaturatesToFF()
    {
        Assert.Equal((byte)0xFF, NumberBaseHelper<byte>.CreateSaturating<NativeCULong>(new NativeCULong(0x100)));
    }

    [Fact]
    public void CreateSaturating_ToSbyte_FitsRoundTrips()
    {
        Assert.Equal((sbyte)0x7F, NumberBaseHelper<sbyte>.CreateSaturating<NativeCULong>(new NativeCULong(0x7Fu)));
    }

    [Fact]
    public void CreateSaturating_ToSbyte_ExceedingSaturatesToMax()
    {
        Assert.Equal(sbyte.MaxValue, NumberBaseHelper<sbyte>.CreateSaturating<NativeCULong>(new NativeCULong(0x80u)));
    }

    [Fact]
    public void CreateSaturating_ToShort_FitsRoundTrips()
    {
        Assert.Equal((short)0x7FFF, NumberBaseHelper<short>.CreateSaturating<NativeCULong>(new NativeCULong(0x7FFFu)));
    }

    [Fact]
    public void CreateSaturating_ToShort_ExceedingSaturatesToMax()
    {
        Assert.Equal(short.MaxValue, NumberBaseHelper<short>.CreateSaturating<NativeCULong>(new NativeCULong(0x8000u)));
    }

    [Fact]
    public void CreateSaturating_ToInt_WithinRange_RoundTrips()
    {
        // Branch closure for the `v > int.MaxValue ? int.MaxValue : (int)v` ternary —
        // the existing ExceedingSaturatesToMax test covers the true branch; this covers
        // the value-fits-no-saturation branch.
        Assert.Equal(0x7FFFFFFE, NumberBaseHelper<int>.CreateSaturating<NativeCULong>(new NativeCULong(0x7FFFFFFEu)));
    }

    [Fact]
    public void CreateSaturating_ToInt_LargeValueSaturatesToMax()
    {
        Assert.Equal(int.MaxValue, NumberBaseHelper<int>.CreateSaturating<NativeCULong>(new NativeCULong(0x80000000u)));
    }

    [Fact]
    public void CreateSaturating_ToLong_FitsRoundTrips()
    {
        Assert.Equal(0x7FFFFFFFL, NumberBaseHelper<long>.CreateSaturating<NativeCULong>(new NativeCULong(0x7FFFFFFFu)));
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.Has64BitStorage))]
    public void CreateSaturating_ToLong_ExceedingSaturatesToMaxOn64BitStorage()
    {
#pragma warning disable CS8778
        NativeCULong tooBig = new NativeCULong((nuint)0x8000_0000_0000_0000UL);
#pragma warning restore CS8778
        Assert.Equal(long.MaxValue, NumberBaseHelper<long>.CreateSaturating<NativeCULong>(tooBig));
    }

    [Fact]
    public void CreateSaturating_ToNint_FitsRoundTrips()
    {
        Assert.Equal((nint)42, NumberBaseHelper<nint>.CreateSaturating<NativeCULong>(new NativeCULong(42u)));
    }

    [Fact]
    public void CreateSaturating_ToInt128_RoundTrips()
    {
        Assert.Equal((Int128)0xFFFFFFFFu, NumberBaseHelper<Int128>.CreateSaturating<NativeCULong>(new NativeCULong(0xFFFFFFFFu)));
    }

    //
    // CreateTruncating: wraps to TOther's low-order bits.
    //

    [Fact]
    public void CreateTruncating_ToByte_WrapsToLowOctet()
    {
        Assert.Equal((byte)0x42, NumberBaseHelper<byte>.CreateTruncating<NativeCULong>(new NativeCULong(0x12345642)));
    }

    [Theory]
    [InlineData(0x142u, (byte)0x42)] // sbyte = unchecked((sbyte)0x42) = 0x42
    [InlineData(0x180u, (byte)0x80)] // sbyte = unchecked((sbyte)0x80) = -128
    public void CreateTruncating_ToSbyte_WrapsToLowOctet(uint source, byte expectedAsByte)
    {
        Assert.Equal(unchecked((sbyte)expectedAsByte), NumberBaseHelper<sbyte>.CreateTruncating<NativeCULong>(new NativeCULong(source)));
    }

    [Fact]
    public void CreateTruncating_ToShort_WrapsToLowWord()
    {
        Assert.Equal(unchecked((short)0x5678), NumberBaseHelper<short>.CreateTruncating<NativeCULong>(new NativeCULong(0x12345678u)));
    }

    [Fact]
    public void CreateTruncating_ToInt_LargeValueWrapsToLow32Bits()
    {
        // 0x80000000 truncated and reinterpreted as int = int.MinValue.
        Assert.Equal(int.MinValue, NumberBaseHelper<int>.CreateTruncating<NativeCULong>(new NativeCULong(0x80000000u)));
    }

    [Fact]
    public void CreateTruncating_ToLong_RoundTrips()
    {
        Assert.Equal(0xFFFFFFFFL, NumberBaseHelper<long>.CreateTruncating<NativeCULong>(new NativeCULong(0xFFFFFFFFu)));
    }

    [Fact]
    public void CreateTruncating_ToNint_RoundTrips()
    {
        Assert.Equal((nint)42, NumberBaseHelper<nint>.CreateTruncating<NativeCULong>(new NativeCULong(42u)));
    }

    [Fact]
    public void CreateTruncating_ToInt128_RoundTrips()
    {
        Assert.Equal((Int128)0xFFFFFFFFu, NumberBaseHelper<Int128>.CreateTruncating<NativeCULong>(new NativeCULong(0xFFFFFFFFu)));
    }
}
