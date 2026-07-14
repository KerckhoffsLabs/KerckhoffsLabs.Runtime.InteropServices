# How the per-platform storage is delivered

`NativeCULong` mirrors the platform contract of C's `unsigned long`, which is **not** the same width
everywhere:

| Platform                     | C `unsigned long` | `NativeCULong` storage |
|------------------------------|-------------------|------------------------|
| Windows (32-bit and 64-bit)  | 32-bit            | 32-bit                 |
| Unix 32-bit (ILP32)          | 32-bit            | 32-bit                 |
| Unix 64-bit (LP64)           | 64-bit            | 64-bit                 |

64-bit Windows is the awkward case: it is LLP64, so a pointer is 8 bytes but `long` is only 4. Every
other supported target has `unsigned long` equal to pointer width.

## Two builds of one assembly

A managed value type has a single fixed size per build, so one assembly cannot be 4 bytes on Windows
x64 and 8 bytes on Unix-LP64. The package targets a single `net10.0` and ships **two builds of the
same assembly**:

- `lib/net10.0` — the **`nuint`** build. This is the compile-time reference, and the runtime asset
  for Unix (LP64) and 32-bit Windows.
- `runtimes/win-x64/lib/net10.0` and `runtimes/win-arm64/lib/net10.0` — the **`uint`** build, which
  64-bit Windows resolves at runtime via the RID graph.

**Consumers just reference `net10.0`** — no Windows-specific target framework, no
`RuntimeIdentifier`. The correct `unsigned long` width is selected at runtime per OS, and that size
flows correctly into structs that embed `NativeCULong`. This is validated on both the JIT and
NativeAOT, on Windows x64 and Linux x64.

Only 64-bit Windows needs the override. The `lib/` (`nuint`) fallback is correct for every other RID
by construction: the wide build is pointer-width, and C's `unsigned long` equals pointer width on
every target *except* LLP64. So `lib/` is already right for Unix LP64 (8 == 8) and for all 32-bit
targets including `win-x86` (4 == 4).

## Compile-time vs. runtime

> [!IMPORTANT]
> There is a single compile-time reference (`lib/net10.0`, the `nuint` build), so on 64-bit Windows
> you **compile** against an 8-byte `NativeCULong` but **run** against the 4-byte build.

This is sound because the layout of a sequential/blittable struct is resolved by the runtime at
type-load — and by ILC at NativeAOT publish, which is RID-specific — and is **never** baked in at C#
compile time. So `sizeof`, `Marshal.SizeOf`, `Unsafe.SizeOf`, and any struct embedding
`NativeCULong` all reflect the *loaded* (per-RID) build, not the reference assembly.

The one thing this scheme cannot support is a compile-time constant whose value depends on the
storage width, because `const` values *are* inlined from the reference assembly and would therefore
be wrong on Windows. Accordingly, `NativeCULong` exposes none.
