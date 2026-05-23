// Licensed under the MIT License
// Coverage for INumberBase<NativeCULong>.Create{Checked,Saturating,Truncating} when
// converting INTO NativeCULong from some other numeric type. CreateXxx in NativeCULong.cs
// follows the BCL primitive dispatch pattern (see UInt128.cs in dotnet/runtime): identity
// check, then the private TryConvertFromXxx worker, then TOther.TryConvertToXxx, or throw
// NotSupportedException. Tests of CreateXxx exercise the full chain — including the
// private workers — without needing direct access to them.
//
// To-direction tests (TOther <- NativeCULong) live in NativeCULongTests.ConversionsTo.cs.

using System.Numerics;
using KerckhoffsLabs.Runtime.InteropServices;

namespace KerckhoffsLabs.Runtime.InteropServices.Tests;

public partial class NativeCULongTests
{
    //
    // CreateChecked: throws on out-of-range.
    //

    [Fact]
    public void CreateChecked_FromByte_AlwaysFits()
    {
        Assert.Equal(new NativeCULong(0x00), NumberBaseHelper<NativeCULong>.CreateChecked<byte>(0x00));
        Assert.Equal(new NativeCULong(0xFF), NumberBaseHelper<NativeCULong>.CreateChecked<byte>(0xFF));
    }

    [Theory]
    [InlineData(0, 0x00000000u)]
    [InlineData(int.MaxValue, 0x7FFFFFFFu)]
    public void CreateChecked_FromInt_PositiveRoundTrips(int value, uint expected)
    {
        Assert.Equal(new NativeCULong(expected), NumberBaseHelper<NativeCULong>.CreateChecked<int>(value));
    }

    [Fact]
    public void CreateChecked_FromInt_NegativeThrows()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<int>(-1));
    }

    [Fact]
    public void CreateChecked_FromUInt_RoundTrips()
    {
        // uint is in NativeCULong's TryConvertFromChecked type table; always exact.
        Assert.Equal(new NativeCULong(0xDEADBEEFu), NumberBaseHelper<NativeCULong>.CreateChecked<uint>(0xDEADBEEFu));
    }

    [Fact]
    public void CreateChecked_FromLong_WithinUInt32Range()
    {
        Assert.Equal(new NativeCULong(0xFFFFFFFF), NumberBaseHelper<NativeCULong>.CreateChecked<long>(0xFFFFFFFFL));
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.Has32BitStorage))]
    public void CreateChecked_FromLong_ExceedingUInt32MaxThrowsOn32BitStorage()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<long>(0x1_0000_0000L));
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.Has64BitStorage))]
    public void CreateChecked_FromLong_ExceedingUInt32MaxFitsOn64BitStorage()
    {
#pragma warning disable CS8778 // intentional 64-bit-storage literal; gated at runtime by Has64BitStorage
        Assert.Equal(new NativeCULong((nuint)0x1_0000_0000UL), NumberBaseHelper<NativeCULong>.CreateChecked<long>(0x1_0000_0000L));
#pragma warning restore CS8778
    }

    [Fact]
    public void CreateChecked_FromLong_NegativeThrows()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<long>(-1L));
    }

    [Fact]
    public void CreateChecked_FromUlong_WithinRange_RoundTrips()
    {
        Assert.Equal(new NativeCULong(0u), NumberBaseHelper<NativeCULong>.CreateChecked<ulong>(0UL));
        Assert.Equal(new NativeCULong(0xFFFFFFFFu), NumberBaseHelper<NativeCULong>.CreateChecked<ulong>(0xFFFFFFFFUL));
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.Has32BitStorage))]
    public void CreateChecked_FromUlong_ExceedingStorage_ThrowsOn32BitStorage()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<ulong>(0x1_0000_0000UL));
    }

    [Fact]
    public void CreateChecked_FromNUint_Identity()
    {
        nuint v = 42;
        Assert.Equal(new NativeCULong(v), NumberBaseHelper<NativeCULong>.CreateChecked<nuint>(v));
    }

    //
    // CreateSaturating: clamps to MinValue/MaxValue on out-of-range.
    //

    [Fact]
    public void CreateSaturating_FromInt_NegativeSaturatesToMin()
    {
        Assert.Equal(NativeCULong.MinValue, NumberBaseHelper<NativeCULong>.CreateSaturating<int>(-1));
    }

    [Fact]
    public void CreateSaturating_FromInt_PositiveRoundTrips()
    {
        Assert.Equal(new NativeCULong(0x7FFFFFFF), NumberBaseHelper<NativeCULong>.CreateSaturating<int>(int.MaxValue));
    }

    [Fact]
    public void CreateSaturating_FromUlong_WithinRange_RoundTrips()
    {
        Assert.Equal(new NativeCULong(0xFFFFFFFFu), NumberBaseHelper<NativeCULong>.CreateSaturating<ulong>(0xFFFFFFFFUL));
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.Has32BitStorage))]
    public void CreateSaturating_FromUlong_ExceedingStorage_SaturatesToMaxOn32BitStorage()
    {
        Assert.Equal(NativeCULong.MaxValue, NumberBaseHelper<NativeCULong>.CreateSaturating<ulong>(0x1_0000_0000UL));
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.Has32BitStorage))]
    public void CreateSaturating_FromLong_ExceedingUInt32MaxSaturatesOn32BitStorage()
    {
        Assert.Equal(NativeCULong.MaxValue, NumberBaseHelper<NativeCULong>.CreateSaturating<long>(0x1_0000_0000L));
    }

    [Fact]
    public void CreateSaturating_FromLong_NegativeSaturatesToMin()
    {
        Assert.Equal(NativeCULong.MinValue, NumberBaseHelper<NativeCULong>.CreateSaturating<long>(-1L));
    }

    //
    // CreateTruncating: wraps on out-of-range (low-order bits preserved).
    //

    [Fact]
    public void CreateTruncating_FromInt_NegativeWraps()
    {
        // -1 has every bit set in two's complement; truncating to NativeCULong's
        // storage width yields the all-ones value, which is MaxValue.
        Assert.Equal(NativeCULong.MaxValue, NumberBaseHelper<NativeCULong>.CreateTruncating<int>(-1));
    }

    [Fact]
    public void CreateTruncating_FromUInt_RoundTrips()
    {
        // uint width <= storage width on every platform (4 == 4 on Windows/32-bit, 4 < 8 on
        // 64-bit Unix); truncation is exact.
        Assert.Equal(new NativeCULong(0xDEADBEEFu), NumberBaseHelper<NativeCULong>.CreateTruncating<uint>(0xDEADBEEFu));
    }

    [Fact]
    public void CreateTruncating_FromUlong_RoundTrips()
    {
        Assert.Equal(new NativeCULong(0xFFFFFFFFu), NumberBaseHelper<NativeCULong>.CreateTruncating<ulong>(0xFFFFFFFFUL));
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.Has32BitStorage))]
    public void CreateTruncating_FromLong_HighBitsDroppedOn32BitStorage()
    {
        Assert.Equal(new NativeCULong(0x00000001), NumberBaseHelper<NativeCULong>.CreateTruncating<long>(0x1_0000_0001L));
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.Has32BitStorage))]
    public void CreateTruncating_FromUlong_HighBitsDroppedOn32BitStorage()
    {
        Assert.Equal(new NativeCULong(0x00000001u), NumberBaseHelper<NativeCULong>.CreateTruncating<ulong>(0x1_0000_0001UL));
    }

    //
    // Floating-point and decimal source semantics. The smoke matrix at the bottom covers
    // type-table membership (zero conversion); these tests pin the actual non-zero
    // conversion semantics inherited from NativeType.CreateXxx. Truncation toward zero,
    // NaN/negative handling, and the Checked/Saturating divergence all come from the BCL
    // today — if that delegation is ever replaced with custom logic in the From workers,
    // these tests catch any divergence.
    //

    // CreateChecked: truncates the fractional part on in-range values.
    [Fact]
    public void CreateChecked_FromFloat_TruncatesFractional()
    {
        Assert.Equal(new NativeCULong(3u), NumberBaseHelper<NativeCULong>.CreateChecked<float>(3.5f));
    }

    [Fact]
    public void CreateChecked_FromDouble_TruncatesFractional()
    {
        Assert.Equal(new NativeCULong(3u), NumberBaseHelper<NativeCULong>.CreateChecked<double>(3.5));
    }

    [Fact]
    public void CreateChecked_FromHalf_TruncatesFractional()
    {
        Assert.Equal(new NativeCULong(3u), NumberBaseHelper<NativeCULong>.CreateChecked<Half>((Half)3.5));
    }

    [Fact]
    public void CreateChecked_FromDecimal_TruncatesFractional()
    {
        Assert.Equal(new NativeCULong(3u), NumberBaseHelper<NativeCULong>.CreateChecked<decimal>(3.5m));
    }

    // CreateChecked: throws on NaN or on negatives whose truncated integer is below 0.
    [Fact]
    public void CreateChecked_FromFloat_NaN_Throws()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<float>(float.NaN));
    }

    [Fact]
    public void CreateChecked_FromFloat_Negative_Throws()
    {
        // BCL truncates toward zero first, then range-checks: -0.5f truncates to 0 (in
        // range) and would not throw, so use a value whose truncated integer is below 0.
        Assert.Throws<OverflowException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<float>(-1.5f));
    }

    [Fact]
    public void CreateChecked_FromDouble_NaN_Throws()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<double>(double.NaN));
    }

    [Fact]
    public void CreateChecked_FromDouble_Negative_Throws()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<double>(-1.5));
    }

    [Fact]
    public void CreateChecked_FromHalf_NaN_Throws()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<Half>(Half.NaN));
    }

    [Fact]
    public void CreateChecked_FromHalf_Negative_Throws()
    {
        Assert.Throws<OverflowException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<Half>((Half)(-1.5)));
    }

    [Fact]
    public void CreateChecked_FromDecimal_Negative_Throws()
    {
        // decimal has no NaN, so only the negative path applies.
        Assert.Throws<OverflowException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<decimal>(-1.5m));
    }

    // CreateSaturating: NaN saturates to MinValue (IEEE convention); negatives saturate
    // to MinValue.
    [Fact]
    public void CreateSaturating_FromFloat_NaN_SaturatesToMin()
    {
        Assert.Equal(NativeCULong.MinValue, NumberBaseHelper<NativeCULong>.CreateSaturating<float>(float.NaN));
    }

    [Fact]
    public void CreateSaturating_FromFloat_Negative_SaturatesToMin()
    {
        Assert.Equal(NativeCULong.MinValue, NumberBaseHelper<NativeCULong>.CreateSaturating<float>(-1.5f));
    }

    [Fact]
    public void CreateSaturating_FromDouble_NaN_SaturatesToMin()
    {
        Assert.Equal(NativeCULong.MinValue, NumberBaseHelper<NativeCULong>.CreateSaturating<double>(double.NaN));
    }

    [Fact]
    public void CreateSaturating_FromDouble_Negative_SaturatesToMin()
    {
        Assert.Equal(NativeCULong.MinValue, NumberBaseHelper<NativeCULong>.CreateSaturating<double>(-1.5));
    }

    [Fact]
    public void CreateSaturating_FromHalf_NaN_SaturatesToMin()
    {
        Assert.Equal(NativeCULong.MinValue, NumberBaseHelper<NativeCULong>.CreateSaturating<Half>(Half.NaN));
    }

    [Fact]
    public void CreateSaturating_FromHalf_Negative_SaturatesToMin()
    {
        Assert.Equal(NativeCULong.MinValue, NumberBaseHelper<NativeCULong>.CreateSaturating<Half>((Half)(-1.5)));
    }

    [Fact]
    public void CreateSaturating_FromDecimal_Negative_SaturatesToMin()
    {
        // decimal has no NaN, so only the negative path applies.
        Assert.Equal(NativeCULong.MinValue, NumberBaseHelper<NativeCULong>.CreateSaturating<decimal>(-1.5m));
    }

    // CreateTruncating: truncates the fractional part on in-range values. (NaN/negative
    // handling for Truncating is implementation-defined per IEEE; pin only the in-range
    // truncation behavior, which is the well-defined common case.)
    [Fact]
    public void CreateTruncating_FromFloat_TruncatesFractional()
    {
        Assert.Equal(new NativeCULong(3u), NumberBaseHelper<NativeCULong>.CreateTruncating<float>(3.5f));
    }

    [Fact]
    public void CreateTruncating_FromDouble_TruncatesFractional()
    {
        Assert.Equal(new NativeCULong(3u), NumberBaseHelper<NativeCULong>.CreateTruncating<double>(3.5));
    }

    [Fact]
    public void CreateTruncating_FromHalf_TruncatesFractional()
    {
        Assert.Equal(new NativeCULong(3u), NumberBaseHelper<NativeCULong>.CreateTruncating<Half>((Half)3.5));
    }

    [Fact]
    public void CreateTruncating_FromDecimal_TruncatesFractional()
    {
        Assert.Equal(new NativeCULong(3u), NumberBaseHelper<NativeCULong>.CreateTruncating<decimal>(3.5m));
    }

    //
    // Identity shortcut: CreateXxx<NativeCULong>(value) hits the typeof(TOther) ==
    // typeof(NativeCULong) fast path that skips dispatch entirely.
    //

    [Fact]
    public void CreateChecked_FromNativeCULong_Identity()
    {
        NativeCULong v = new NativeCULong(0xDEADBEEFu);
        Assert.Equal(v, NumberBaseHelper<NativeCULong>.CreateChecked<NativeCULong>(v));
    }

    [Fact]
    public void CreateSaturating_FromNativeCULong_Identity()
    {
        NativeCULong v = new NativeCULong(0xDEADBEEFu);
        Assert.Equal(v, NumberBaseHelper<NativeCULong>.CreateSaturating<NativeCULong>(v));
    }

    [Fact]
    public void CreateTruncating_FromNativeCULong_Identity()
    {
        NativeCULong v = new NativeCULong(0xDEADBEEFu);
        Assert.Equal(v, NumberBaseHelper<NativeCULong>.CreateTruncating<NativeCULong>(v));
    }

    //
    // Unsupported source: NotSupportedException. NativeCULong's From-table covers the
    // standard numeric primitives; for anything else (here, System.Numerics.Complex, an
    // INumberBase implementation that NativeCULong doesn't recognize and that has no
    // NativeCULong entry in its own To-table) both halves of the dispatch return false
    // and the public CreateXxx throws.
    //

    [Fact]
    public void CreateChecked_UnsupportedSource_Throws()
    {
        Assert.Throws<NotSupportedException>(() => NumberBaseHelper<NativeCULong>.CreateChecked<Complex>(default));
    }

    [Fact]
    public void CreateSaturating_UnsupportedSource_Throws()
    {
        Assert.Throws<NotSupportedException>(() => NumberBaseHelper<NativeCULong>.CreateSaturating<Complex>(default));
    }

    [Fact]
    public void CreateTruncating_UnsupportedSource_Throws()
    {
        Assert.Throws<NotSupportedException>(() => NumberBaseHelper<NativeCULong>.CreateTruncating<Complex>(default));
    }

    //
    // From-table membership smoke matrix: every recognized source type, every direction.
    // Each TryConvertFromXxx worker has its own typeof OR-chain; a regression that drops
    // a type from one chain would only surface for that specific direction. If a row
    // below throws NotSupportedException, the line points directly at the missing entry.
    //
    // Keep this list in sync with the 17-type typeof OR-chain in the TryConvertFromXxx
    // workers in NativeCULong.cs (each chain spans byte/sbyte/short/ushort/int/uint/long/
    // ulong/nint/nuint/Int128/UInt128/char/decimal/float/Half/double).
    //

    [Fact]
    public void Create_AllSupportedSources_RoundTripZero()
    {
        NativeCULong zero = default;

        // CreateChecked
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<byte>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<sbyte>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<short>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<ushort>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<int>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<uint>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<long>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<ulong>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<nint>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<nuint>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<Int128>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<UInt128>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<char>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<decimal>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<float>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<Half>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateChecked<double>(default));

        // CreateSaturating
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<byte>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<sbyte>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<short>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<ushort>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<int>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<uint>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<long>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<ulong>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<nint>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<nuint>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<Int128>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<UInt128>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<char>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<decimal>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<float>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<Half>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateSaturating<double>(default));

        // CreateTruncating
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<byte>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<sbyte>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<short>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<ushort>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<int>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<uint>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<long>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<ulong>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<nint>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<nuint>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<Int128>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<UInt128>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<char>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<decimal>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<float>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<Half>(default));
        Assert.Equal(zero, NumberBaseHelper<NativeCULong>.CreateTruncating<double>(default));
    }
}
