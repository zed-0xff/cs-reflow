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
        var methodName = name;
        var actualOutput = controlFlowUnflattener.ReflowMethod(methodName);

        // Assert
        Assert.Equal(expectedOutput, actualOutput);
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
}
