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

        string actualOutput = "";
        foreach (var kv in controlFlowUnflattener.Methods)
        {
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
    public void ProcessData0()
    {
        checkData("Bytes_afff");
    }

    [Fact]
    public void ProcessData1()
    {
        checkData("get_icon_a661");
    }

    [Fact]
    public void ProcessData2()
    {
        checkData("goto_default_case");
    }

    [Fact]
    public void invert_if()
    {
        checkData("invert_if");
    }

    [Fact]
    public void ProcessData4()
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
}
