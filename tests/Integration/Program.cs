using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using KerckhoffsLabs.Runtime.InteropServices;

// Portable, framework-dependent consumer (no RID). Asserts that the package delivers the correct
// C unsigned long width at runtime per OS — 4 bytes on Windows, pointer width on Unix — and that
// the size flows into a struct that embeds NativeCULong. Exit code is the verdict.

bool win = OperatingSystem.IsWindows();
int expectedCul = win ? 4 : IntPtr.Size;
int expectedPair = expectedCul * 2;
int expectedMechanismInfo = expectedCul * 3;

int culMarshal = Marshal.SizeOf<NativeCULong>();
int culUnsafe = Unsafe.SizeOf<NativeCULong>();
int pairMarshal = Marshal.SizeOf<Pair>();
int pairUnsafe = Unsafe.SizeOf<Pair>();
int miMarshal = Marshal.SizeOf<MechanismInfo>();
int miUnsafe = Unsafe.SizeOf<MechanismInfo>();

Console.WriteLine($"OS={(win ? "windows" : "unix")} RID={RuntimeInformation.RuntimeIdentifier} Arch={RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"NativeCULong:  Marshal={culMarshal} Unsafe={culUnsafe} (expected {expectedCul})");
Console.WriteLine($"Pair:          Marshal={pairMarshal} Unsafe={pairUnsafe} (expected {expectedPair})");
Console.WriteLine($"MechanismInfo: Marshal={miMarshal} Unsafe={miUnsafe} (expected {expectedMechanismInfo})");

bool ok = culMarshal == expectedCul && culUnsafe == expectedCul
       && pairMarshal == expectedPair && pairUnsafe == expectedPair
       && miMarshal == expectedMechanismInfo && miUnsafe == expectedMechanismInfo;
Console.WriteLine(ok ? "PASS" : "FAIL");
return ok ? 0 : 1;

[StructLayout(LayoutKind.Sequential)]
internal struct Pair
{
    public NativeCULong A;
    public NativeCULong B;
}

// The exact struct the README's quick start shows, kept in sync with it: the README claims this
// is "12 bytes on Windows, 24 bytes on 64-bit Unix", and this is what proves that on both.
[StructLayout(LayoutKind.Sequential)]
internal struct MechanismInfo
{
    public NativeCULong MinKeySize;
    public NativeCULong MaxKeySize;
    public NativeCULong Flags;
}
