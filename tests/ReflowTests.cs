using System.IO;
using Xunit;

public class ReflowTests
{
    public string DataPath
    {
        get
        {
            var basePath = Directory.GetCurrentDirectory();
            for (int i = 0; i < 10; i++)
            {
                var dataPath = Path.Combine(basePath, "data");
                if (Directory.Exists(dataPath))
                    return dataPath;

                basePath = Directory.GetParent(basePath).FullName;
            }
            throw new DirectoryNotFoundException("data directory not found");
        }
    }

    void checkData(string name)
    {
        var inputPath = Path.Combine(DataPath, $"{name}.cs");
        var expectedPath = Path.Combine(DataPath, $"{name}.out");

        var input = File.ReadAllText(inputPath);
        var expectedOutput = File.ReadAllText(expectedPath);

        // Act
        var controlFlowUnflattener = new ControlFlowUnflattener(input);
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

        if (expectedOutput.TrimEnd() != actualOutput.TrimEnd())
        {
            // write to file
            var outputPath = Path.Combine(DataPath, $"{name}.out.actual");
            File.WriteAllText(outputPath, actualOutput);
        }

        // Assert
        Assert.Equal(expectedOutput.TrimEnd(), actualOutput.TrimEnd());
    }

    [Fact]
    public void Bytes_afff()
    {
        checkData("Bytes_afff");
    }

    [Fact]
    public void get_icon_a661()
    {
        checkData("get_icon_a661");
    }

    [Fact]
    public void goto_default_case()
    {
        checkData("goto_default_case");
    }

    [Fact]
    public void invert_if()
    {
        checkData("invert_if");
    }

    [Fact]
    public void try_catch()
    {
        checkData("try_catch");
    }

    [Fact]
    public void CheckBox_a47b()
    {
        checkData("CheckBox_a47b");
    }

    [Fact]
    public void loop()
    {
        checkData("loop");
    }

    [Fact]
    public void logic()
    {
        checkData("logic");
    }

    [Fact]
    public void unknown_expr()
    {
        checkData("unknown_expr");
    }

    [Fact]
    public void rsa_aaf9_0()
    {
        checkData("rsa_aaf9_0");
    }

    [Fact]
    public void rsa_aaf9_5()
    {
        checkData("rsa_aaf9_5");
    }

    [Fact]
    public void rsa_aa96_5()
    {
        checkData("rsa_aa96_5");
    }

    [Fact]
    public void labels()
    {
        checkData("labels");
    }

    [Fact]
    public void big()
    {
        checkData("big");
    }
}
