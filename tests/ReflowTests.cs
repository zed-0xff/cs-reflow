using System.Collections;
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
                    return dataPath;

                var parent = Directory.GetParent(basePath);
                if (parent == null)
                    break;

                basePath = parent.FullName;
            }
            throw new DirectoryNotFoundException("data directory not found");
        }
    }

    public static void CheckData(string fname)
    {
        var inputPath = Path.Combine(ReflowTests.DataPath, fname.Trim());
        var expectedPath = inputPath + ".out";

        var input = File.ReadAllText(inputPath);
        var expectedOutput = File.ReadAllText(expectedPath);

        // Act
        var controlFlowUnflattener = new ControlFlowUnflattener(input);
        if (controlFlowUnflattener.Methods.Count == 0)
            controlFlowUnflattener = new ControlFlowUnflattener(input, dummyClassWrap: true);

        controlFlowUnflattener.AddComments = false;
        controlFlowUnflattener.Verbosity = -2;

        string actualOutput = "";
        foreach (var kv in controlFlowUnflattener.Methods)
        {
            controlFlowUnflattener.Reset();
            actualOutput += controlFlowUnflattener.ReflowMethod(kv.Value);
            if (!actualOutput.EndsWith("\n"))
                actualOutput += "\n";
            actualOutput += "\n";
        }

        var actualPath = fname + ".out.actual";
        if (expectedOutput.TrimEnd() != actualOutput.TrimEnd())
        {
            // write to file
            File.WriteAllText(actualPath, actualOutput);
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
}

public class real_code
{
    public static IEnumerable<object[]> Files =>
        Directory.GetFiles(Path.Combine(ReflowTests.DataPath, "real"), "*.cs")
        .Select(f => new object[] {
                Path.GetRelativePath(ReflowTests.DataPath, f).PadRight(30)
                });

    [Theory]
    [MemberData(nameof(Files))]
    public void check(string fname)
    {
        ReflowTests.CheckData(fname);
    }
}

public class synthetic
{
    public static IEnumerable<object[]> Files =>
        Directory.GetFiles(Path.Combine(ReflowTests.DataPath, "synthetic"), "*.cs")
        .Select(f => new object[] {
                Path.GetRelativePath(ReflowTests.DataPath, f).PadRight(30)
                });

    [Theory]
    [MemberData(nameof(Files))]
    public void check(string fname)
    {
        ReflowTests.CheckData(fname);
    }
}
