# KerckhoffsLabs.Runtime.InteropServices

[![NuGet](https://img.shields.io/nuget/v/KerckhoffsLabs.Runtime.InteropServices)](https://www.nuget.org/packages/KerckhoffsLabs.Runtime.InteropServices)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![codecov](https://codecov.io/gh/KerckhoffsLabs/KerckhoffsLabs.Runtime.InteropServices/graph/badge.svg?token=9N30Z15QRA)](https://codecov.io/gh/KerckhoffsLabs/KerckhoffsLabs.Runtime.InteropServices)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=KerckhoffsLabs_KerckhoffsLabs.Runtime.InteropServices&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=KerckhoffsLabs_KerckhoffsLabs.Runtime.InteropServices)

Provides platform-native interop types for use at the managed/unmanaged boundary.

## Types

### `NativeCULong`

Represents the `unsigned long` type from C and C++. Mirrors the platform contract of the BCL's [`CULong`](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.culong):

| Platform         | Storage |
|------------------|---------|
| Windows (32/64)  | 32-bit  |
| Unix 32-bit      | 32-bit  |
| Unix 64-bit      | 64-bit  |

Unlike `CULong`, `NativeCULong` implements the full generic math interface hierarchy (`IBinaryInteger<T>`, `IUnsignedNumber<T>`, etc.), `ISpanFormattable`, and `IUtf8SpanFormattable`, making it usable directly in generic numeric code and P/Invoke marshalling without an unwrap step.

#### How the per-platform storage is delivered

A managed value type has a single fixed size per build, so one assembly cannot be 4 bytes on
Windows x64 and 8 bytes on Unix-LP64. The package targets a single `net10.0` and ships two builds
of the same assembly:

- `lib/net10.0` — the **nuint** build: the compile-time reference, and the runtime asset for Unix
  (LP64) and 32-bit Windows.
- `runtimes/win-x64/lib/net10.0`, `runtimes/win-arm64/lib/net10.0` — the **uint** build that
  64-bit Windows resolves at runtime via the RID graph.

**Consumers just reference `net10.0`** — no Windows-specific target framework, no `RuntimeIdentifier`.
The correct `unsigned long` width is selected at runtime per OS, and that size flows correctly into
structs that embed `NativeCULong`. Validated on both the JIT and NativeAOT, on Windows x64 and Linux x64.

> **Note (compile vs. runtime).** There is a single compile-time reference (`lib/net10.0`, the
> `nuint` build), so on 64-bit Windows you *compile* against an 8-byte `NativeCULong` but *run*
> against the 4-byte build. This is sound because a sequential/blittable struct's layout is resolved
> at type-load (and at AOT publish, which is RID-specific) — never baked at compile time — so
> `sizeof`, `Marshal.SizeOf`, `Unsafe.SizeOf`, and any embedding struct all reflect the loaded build.
> The one thing this *can't* do is a compile-time constant whose value depends on the width, so
> `NativeCULong` exposes none.

## Requirements

- .NET 10.0 or later

## Installation

```
dotnet add package KerckhoffsLabs.Runtime.InteropServices
```

## License

MIT — see [LICENSE](LICENSE).
