// Licensed under the MIT License

namespace KerckhoffsLabs.Runtime.InteropServices.Tests;

/// <summary>
/// Storage-width predicates for <c>NativeCULong</c>. Centralized so that the
/// rule (32-bit on Windows or x86, 64-bit on Unix LP64) is expressed once
/// and any future refinement applies everywhere.
/// </summary>
internal static class PlatformLayout
{
    // NativeCULong's storage width is fixed at COMPILE time, not by the runtime OS: the
    // WINDOWS symbol (defined by the net*-windows target framework) selects uint storage,
    // every other target uses nuint. So the predicate must mirror the compilation the test
    // assembly was built for — keying off OperatingSystem.IsWindows() at runtime would be
    // wrong whenever the build OS and the target framework disagree.
    internal static bool Has32BitStorage =>
#if WINDOWS
        true;
#else
        IntPtr.Size == 4;
#endif
    internal static bool Has64BitStorage => !Has32BitStorage;

    internal static bool NativeIntConstructorCanOverflow => IntPtr.Size != 4 && Has32BitStorage;
    internal static bool NativeIntConstructorCannotOverflow => !NativeIntConstructorCanOverflow;
}
