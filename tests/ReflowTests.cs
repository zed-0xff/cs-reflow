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

    [Fact]
    public void ProcessData_TransformsInput_Correctly()
    {
        // Arrange
        var inputPath = Path.Combine(DataPath, "Bytes_afff.cs");
        var expectedPath = Path.Combine(DataPath, "Bytes_afff.out");

        var input = File.ReadAllText(inputPath);
        var expectedOutput = File.ReadAllText(expectedPath);

        // Act
        var controlFlowUnflattener = new ControlFlowUnflattener(input);
        var methodName = "Bytes_afff";
        var actualOutput = controlFlowUnflattener.ReflowMethod(methodName);

        // Assert
        Assert.Equal(expectedOutput, actualOutput);
    }
}
