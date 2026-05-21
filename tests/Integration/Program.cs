using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using KerckhoffsLabs.Runtime.InteropServices;

// Portable, framework-dependent consumer (no RID). Asserts that the package delivers the correct
// C unsigned long width at runtime per OS — 4 bytes on Windows, pointer width on Unix — and that
// the size flows into a struct that embeds NativeCULong. Exit code is the verdict.

bool win = OperatingSystem.IsWindows();
int expectedCul = win ? 4 : IntPtr.Size;
int expectedPair = expectedCul * 2;

int culMarshal = Marshal.SizeOf<NativeCULong>();
int culUnsafe = Unsafe.SizeOf<NativeCULong>();
int pairMarshal = Marshal.SizeOf<Pair>();
int pairUnsafe = Unsafe.SizeOf<Pair>();

Console.WriteLine($"OS={(win ? "windows" : "unix")} RID={RuntimeInformation.RuntimeIdentifier} Arch={RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"NativeCULong: Marshal={culMarshal} Unsafe={culUnsafe} (expected {expectedCul})");
Console.WriteLine($"Pair:         Marshal={pairMarshal} Unsafe={pairUnsafe} (expected {expectedPair})");

bool ok = culMarshal == expectedCul && culUnsafe == expectedCul
       && pairMarshal == expectedPair && pairUnsafe == expectedPair;
Console.WriteLine(ok ? "PASS" : "FAIL");
return ok ? 0 : 1;

[StructLayout(LayoutKind.Sequential)]
internal struct Pair
{
    public NativeCULong A;
    public NativeCULong B;
}
