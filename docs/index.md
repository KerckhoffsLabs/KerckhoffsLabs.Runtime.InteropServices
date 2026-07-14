<img class="hero-logo" src="images/logo-lockup.png" alt="KerckhoffsLabs" />

# KerckhoffsLabs.Runtime.InteropServices

Platform-native interop types for use at the **managed/unmanaged boundary**.

This site hosts the generated API reference. Browse the [**API documentation**](api/index.md)
for the full surface.

## `NativeCULong`

Represents the `unsigned long` type from C and C++, mirroring the platform contract of the
BCL's [`CULong`](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.culong):
32-bit on Windows and 32-bit Unix, 64-bit on 64-bit Unix.

Unlike `CULong`, `NativeCULong` implements the full generic-math interface hierarchy
(`IBinaryInteger<T>`, `IUnsignedNumber<T>`, …), `ISpanFormattable`, and `IUtf8SpanFormattable`,
so it drops directly into generic numeric code and P/Invoke marshalling with no unwrap step.

The package ships a single `net10.0` reference and selects the correct `unsigned long` width per
OS at runtime — no Windows-specific target framework or `RuntimeIdentifier` required. Validated on
both the JIT and NativeAOT. See [**How the per-platform storage is delivered**](platform-storage.md)
for how that works and what it means when you compile on 64-bit Windows.
