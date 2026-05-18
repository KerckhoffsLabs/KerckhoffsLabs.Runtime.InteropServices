# KerckhoffsLabs.Runtime.InteropServices

[![NuGet](https://img.shields.io/nuget/v/KerckhoffsLabs.Runtime.InteropServices)](https://www.nuget.org/packages/KerckhoffsLabs.Runtime.InteropServices)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![codecov](https://codecov.io/gh/KerckhoffsLabs/KerckhoffsLabs.Runtime.InteropServices/graph/badge.svg?token=9N30Z15QRA)](https://codecov.io/gh/KerckhoffsLabs/KerckhoffsLabs.Runtime.InteropServices)

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

## Requirements

- .NET 10.0 or later

## Installation

```
dotnet add package KerckhoffsLabs.Runtime.InteropServices
```

## License

MIT — see [LICENSE](LICENSE).
