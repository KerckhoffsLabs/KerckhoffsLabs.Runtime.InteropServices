// Licensed under the MIT License

using System.Runtime.InteropServices;
using KerckhoffsLabs.Runtime.InteropServices;
using static KerckhoffsLabs.Runtime.InteropServices.Tests.PlatformLayout;

namespace KerckhoffsLabs.Runtime.InteropServices.Tests;

public partial class NativeCULongTests
{
    [Fact]
    public void Ctor_Empty()
    {
        NativeCULong value = new NativeCULong();
        Assert.Equal(0u, value.Value);
    }

    [Theory]
    [InlineData(0u, 0u)]
    [InlineData(42u, 42u)]
    [InlineData(uint.MaxValue, uint.MaxValue)] // pins uint→nuint widening on 64-bit storage
    public void Ctor_UInt(uint value, uint expected)
    {
        NativeCULong v = new NativeCULong(value);
        Assert.Equal(expected, (uint)v.Value);
    }

    [Fact]
    public void Ctor_NUInt()
    {
        NativeCULong value = new NativeCULong((nuint)42);
        Assert.Equal(42u, value.Value);
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.NativeIntConstructorCanOverflow))]
    public void Ctor_NUInt_OutOfRange()
    {
        Assert.Throws<OverflowException>(() => new NativeCULong(unchecked(((nuint)uint.MaxValue) + 1)));
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.NativeIntConstructorCannotOverflow))]
    public void Ctor_NUInt_LargeValue()
    {
        nuint largeValue = unchecked(((nuint)uint.MaxValue) + 1);
        NativeCULong value = new NativeCULong(largeValue);
        Assert.Equal(largeValue, value.Value);
    }

    // The second column is deliberately heterogeneous (NativeCULong, string, uint, null) to
    // exercise Equals(object); TheoryData<,,> still gives type safety on the value/expected
    // columns. object is the right element type here, so xUnit1042 does not apply.
    public static TheoryData<NativeCULong, object, bool> EqualsData() => new()
    {
        { new NativeCULong(789), new NativeCULong(789), true },
        { new NativeCULong(789), new NativeCULong(0), false },
        { new NativeCULong(0), new NativeCULong(0), true },
        { new NativeCULong(789), null!, false },
        { new NativeCULong(789), "789", false },
        { new NativeCULong(789), 789u, false },
        { NativeCULong.MaxValue, NativeCULong.MaxValue, true },
        // Note: MaxValue paired with 0 lives in a dedicated test below — EqualsTest also
        // asserts GetHashCode *inequality* for unequal values, and on 64-bit storage that
        // is not guaranteed (MaxValue's hash XOR-folds to 0, colliding with 0's hash).
        { NativeCULong.MaxValue, new NativeCULong(1), false },
    };

    [Theory]
    [MemberData(nameof(EqualsData))]
    public void EqualsTest(NativeCULong value, object obj, bool expected)
    {
        if (obj is NativeCULong other)
        {
            Assert.Equal(expected, value.Equals(other));
            Assert.Equal(expected, value.GetHashCode().Equals(other.GetHashCode()));
        }
        Assert.Equal(expected, value.Equals(obj));
    }

    [ConditionalFact(typeof(PlatformLayout), nameof(PlatformLayout.Has64BitStorage))]
    public void GetHashCode_64BitMaxValueCollidesWithZero()
    {
        // nuint.GetHashCode() XOR-folds the two 32-bit halves into a 32-bit int. For
        // 0xFFFFFFFFFFFFFFFF that produces 0, the same hash as the integer 0. This is a
        // permitted collision (Object.GetHashCode allows unequal values to share a hash)
        // but is non-obvious for an integer type, so pin it: anyone tightening the hash
        // function must update or remove this test deliberately.
        Assert.Equal(NativeCULong.MinValue.GetHashCode(), NativeCULong.MaxValue.GetHashCode());
        Assert.NotEqual(NativeCULong.MinValue, NativeCULong.MaxValue);
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(4567, "4567")]
    [InlineData(uint.MaxValue, "4294967295")]
    public void ToStringTest(uint value, string expected)
    {
        NativeCULong NativeCULong = new NativeCULong(value);

        Assert.Equal(expected, NativeCULong.ToString());
    }

    [Fact]
    public unsafe void Size()
    {
        int size = Has32BitStorage ? 4 : 8;
#pragma warning disable xUnit2000 // The value under test here is the sizeof expression
        Assert.Equal(size, sizeof(NativeCULong));
#pragma warning restore xUnit2000
        Assert.Equal(size, Marshal.SizeOf<NativeCULong>());
    }

    [Fact]
    public void MinValueTest()
    {
        Assert.Equal(new NativeCULong(0x00000000), NativeCULong.MinValue);
    }

    [Fact]
    public void MaxValueTest()
    {
        if (Has64BitStorage)
        {
            Assert.Equal(unchecked(new NativeCULong((nuint)0xFFFFFFFFFFFFFFFF)), NativeCULong.MaxValue);
        }
        else
        {
            Assert.Equal(new NativeCULong(0xFFFFFFFF), NativeCULong.MaxValue);
        }
    }

}
