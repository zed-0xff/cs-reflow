#pragma warning disable CS8981 // The type name 'synthetic' only contains lower-cased ascii characters. Such names may become reserved for the language.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

using System.Collections;
using System.Diagnostics;
using System.IO;
using Xunit;

public class ReflowTests
{
    public static string DataPath
    {
        get
        {
            var basePath = Directory.GetCurrentDirectory();
            for (int i = 0; i < 10; i++)
            {
                var dataPath = Path.Combine(basePath, "data");
                if (Directory.Exists(dataPath))
                    return Directory.GetParent(dataPath)!.Parent!.FullName;

                var parent = Directory.GetParent(basePath);
                if (parent == null)
                    break;

                basePath = parent.FullName;
            }
            throw new DirectoryNotFoundException("data directory not found");
        }
    }

    static void runColordiff(string fname_out, string fname_actual)
    {
        Console.WriteLine($"colordiff -u {fname_out} {fname_actual}");

        var pathname_out = Path.Combine(ReflowTests.DataPath, fname_out);
        var pathname_actual = Path.Combine(ReflowTests.DataPath, fname_actual);
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "colordiff",
                Arguments = $"-u \"{pathname_out}\" \"{pathname_actual}\"",
                UseShellExecute = false,
                CreateNoWindow = false
            }
        };

        process.Start();
        process.WaitForExit();
    }

    public static void CheckData(string fname)
    {
        TypeDB.Bitness = 32;

        fname = fname.Trim();
        var fname_out = fname + ".out";
        var inputPath = Path.Combine(ReflowTests.DataPath, fname);
        var expectedPath = Path.Combine(ReflowTests.DataPath, fname_out);

        var input = File.ReadAllText(inputPath);
        var expectedOutput = File.ReadAllText(expectedPath);

        // Act
        var controlFlowUnflattener = new ControlFlowUnflattener(input, verbosity: -2);
        if (controlFlowUnflattener.Methods.Count == 0)
            controlFlowUnflattener = new ControlFlowUnflattener(input, verbosity: -2, dummyClassWrap: true);

        controlFlowUnflattener.AddComments = false;

        string actualOutput = "";
        foreach (var kv in controlFlowUnflattener.Methods)
        {
            controlFlowUnflattener.Reset();
            actualOutput += controlFlowUnflattener.ReflowMethod(kv.Value);
            if (!actualOutput.EndsWith("\n"))
                actualOutput += "\n";
            actualOutput += "\n";
        }

        var fname_actual = fname_out + ".actual";
        var actualPath = Path.Combine(ReflowTests.DataPath, fname_actual);
        if (expectedOutput.TrimEnd() != actualOutput.TrimEnd())
        {
            // write to file
            File.WriteAllText(actualPath, actualOutput);
            runColordiff(fname_out, fname_actual);
        }
        else
        {
            // delete actual output file if it exists
            if (File.Exists(actualPath))
                File.Delete(actualPath);
        }

        // Assert
        StringAssert.Equal(expectedOutput.TrimEnd(), actualOutput.TrimEnd());
    }

    public static IEnumerable<object[]> GetFiles(string dirName)
    {
        string? filter = Environment.GetEnvironmentVariable("TEST_FILTER");

        bool was = false;
        foreach (var fname in Directory.GetFiles(Path.Combine(DataPath, dirName), "*.cs"))
        {
            if (filter != null && !fname.Contains(filter))
                continue;

            yield return new object[] { Path.GetRelativePath(DataPath, fname).PadRight(40) };
            was = true;
        }
        if (!was)
        {
            yield return new object[] { null }; // prevent Error Message: System.InvalidOperationException : No data found
        }
    }
}

public class real_code
{
    public static IEnumerable<object[]> GetFiles() => ReflowTests.GetFiles("tests/data/real");

    [Theory]
    [MemberData(nameof(GetFiles))]
    public void check(string fname)
    {
        if (fname != null)
            ReflowTests.CheckData(fname);
    }
}

public class synthetic
{
    public static IEnumerable<object[]> GetFiles() => ReflowTests.GetFiles("tests/data/synthetic");

    [Theory]
    [MemberData(nameof(GetFiles))]
    public void check(string fname)
    {
        if (fname != null)
            ReflowTests.CheckData(fname);
    }
}
