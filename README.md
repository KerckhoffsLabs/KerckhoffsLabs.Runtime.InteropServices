![KerckhoffsLabs.Runtime.InteropServices](https://raw.githubusercontent.com/KerckhoffsLabs/KerckhoffsLabs.Runtime.InteropServices/main/docs/images/logo-lockup.png)

# KerckhoffsLabs.Runtime.InteropServices

**Platform-native interop types for the managed/unmanaged boundary.**

[![NuGet](https://img.shields.io/nuget/v/KerckhoffsLabs.Runtime.InteropServices)](https://www.nuget.org/packages/KerckhoffsLabs.Runtime.InteropServices)
[![Docs](https://img.shields.io/badge/docs-online-2ea44f)](https://kerckhoffslabs.github.io/KerckhoffsLabs.Runtime.InteropServices/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/KerckhoffsLabs/KerckhoffsLabs.Runtime.InteropServices/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![codecov](https://codecov.io/gh/KerckhoffsLabs/KerckhoffsLabs.Runtime.InteropServices/graph/badge.svg?token=9N30Z15QRA)](https://codecov.io/gh/KerckhoffsLabs/KerckhoffsLabs.Runtime.InteropServices)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=KerckhoffsLabs_KerckhoffsLabs.Runtime.InteropServices&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=KerckhoffsLabs_KerckhoffsLabs.Runtime.InteropServices)

## Overview

C's `unsigned long` is 32-bit on Windows, 32-bit on 32-bit Unix, and **64-bit on 64-bit Unix**. Get
that width wrong at a P/Invoke boundary and you don't get a compile error — you get silent stack
corruption, or a struct whose fields are all shifted by four bytes.

The .NET BCL ships [`CULong`](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.culong)
to model this, but it is a thin wrapper: to do anything numeric with it you unwrap to `Value`, do
the arithmetic, and wrap back. `NativeCULong` honours the same platform contract while behaving like
a real integer.

- **Correct width on every platform** — 32-bit on Windows and 32-bit Unix, 64-bit on 64-bit Unix.
- **Full generic math** — implements `IBinaryInteger<T>`, `IUnsignedNumber<T>` and `IMinMaxValue<T>`,
  so it drops straight into `where T : IBinaryInteger<T>` code with no unwrap step.
- **Formatting and parsing** — `ISpanFormattable`, `IUtf8SpanFormattable` and `ISpanParsable<T>`.
- **Checked and unchecked conversions** — every lossy cast ships as a pair, so `checked` contexts
  throw `OverflowException` instead of silently truncating.
- **Blittable** — embed it directly in `[StructLayout(LayoutKind.Sequential)]` structs and let the
  runtime lay them out correctly per-OS.
- **One target framework** — consumers reference plain `net10.0`. No Windows-specific TFM, no
  `RuntimeIdentifier`. Validated on both the JIT and NativeAOT.

## Installation

```
dotnet add package KerckhoffsLabs.Runtime.InteropServices
```

Requires .NET 10.0 or later.

## Quick start

Use `NativeCULong` wherever a native API says `unsigned long`:

```csharp
using System.Runtime.InteropServices;
using KerckhoffsLabs.Runtime.InteropServices;

internal static class Native
{
    // C: unsigned long C_Initialize(void *pInitArgs);
    [DllImport("pkcs11")]
    internal static extern NativeCULong C_Initialize(nint pInitArgs);

    // C: unsigned long C_GetMechanismInfo(unsigned long slotId, CK_MECHANISM_INFO *info);
    [DllImport("pkcs11")]
    internal static extern NativeCULong C_GetMechanismInfo(NativeCULong slotId, ref MechanismInfo info);
}

// C: typedef struct { unsigned long ulMinKeySize, ulMaxKeySize, flags; } CK_MECHANISM_INFO;
// Laid out correctly on every platform — 12 bytes on Windows, 24 bytes on 64-bit Unix.
[StructLayout(LayoutKind.Sequential)]
internal struct MechanismInfo
{
    public NativeCULong MinKeySize;
    public NativeCULong MaxKeySize;
    public NativeCULong Flags;
}

NativeCULong rv = Native.C_Initialize(nint.Zero);
if (rv != default)
{
    throw new InvalidOperationException($"C_Initialize failed: 0x{rv:X}");
}
```

> **Note.** These samples use `DllImport`, which needs no project-level opt-in. `NativeCULong` also
> works with the source-generated `[LibraryImport]`, but — as for any custom struct passed by value —
> that requires your project to set `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` *and* apply
> `[assembly: DisableRuntimeMarshalling]`, or the generator reports `SYSLIB1062` and `SYSLIB1051`.

Conversions are explicit, and the `checked` variants refuse to lose data:

```csharp
NativeCULong count = (NativeCULong)16u;   // always exact
nuint raw = count.Value;                  // the underlying storage
ulong wide = (ulong)count;                // always exact

// A checked context — including the project-wide CheckForOverflowUnderflow
// setting — routes to the checked operator, which throws instead of wrapping.
NativeCULong bad = checked((NativeCULong)(-1));   // OverflowException
```

And because it is a real generic-math integer, it works in numeric code with no unwrap step:

```csharp
static T Sum<T>(ReadOnlySpan<T> values) where T : IBinaryInteger<T>
{
    T total = T.Zero;
    foreach (T value in values)
    {
        total += value;
    }
    return total;
}

NativeCULong slots = Sum<NativeCULong>([(NativeCULong)3u, (NativeCULong)4u]);
Console.WriteLine(slots);   // 7
```

## Supported platforms

| Platform                    | C `unsigned long` | `NativeCULong` storage | Runtime asset                |
|-----------------------------|-------------------|------------------------|------------------------------|
| Windows x64 / arm64 (LLP64) | 32-bit            | 32-bit                 | `runtimes/win-*/lib/net10.0` |
| Windows 32-bit              | 32-bit            | 32-bit                 | `lib/net10.0`                |
| Unix 32-bit (ILP32)         | 32-bit            | 32-bit                 | `lib/net10.0`                |
| Unix 64-bit (LP64)          | 64-bit            | 64-bit                 | `lib/net10.0`                |

You reference plain `net10.0`, and the right asset is selected for you at runtime.

## Documentation

- [**API reference**](https://kerckhoffslabs.github.io/KerckhoffsLabs.Runtime.InteropServices/api/) — the full generated surface.
- [**How the per-platform storage is delivered**](https://kerckhoffslabs.github.io/KerckhoffsLabs.Runtime.InteropServices/platform-storage.html) —
  how a single-target package ships two builds of one assembly, and what it means that on 64-bit
  Windows you *compile* against an 8-byte type but *run* against a 4-byte one.

## License

MIT — see [LICENSE](https://github.com/KerckhoffsLabs/KerckhoffsLabs.Runtime.InteropServices/blob/main/LICENSE).

## Support

Bug reports and feature requests belong in
[GitHub issues](https://github.com/KerckhoffsLabs/KerckhoffsLabs.Runtime.InteropServices/issues).

## About

Built and maintained by [KerckhoffsLabs](https://github.com/KerckhoffsLabs) and contributors.
