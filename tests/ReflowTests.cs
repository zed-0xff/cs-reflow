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

                var parent = Directory.GetParent(basePath);
                if (parent == null)
                    break;

                basePath = parent.FullName;
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

        var actualPath = Path.Combine(DataPath, $"{name}.out.actual");
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
    public void synthetic_try_catch()
    {
        checkData("try_catch");
    }

    [Fact]
    public void try_catch_real()
    {
        checkData("try_catch_real");
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
    public void synthetic_logic()
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

    [Fact]
    public void synthetic_do()
    {
        checkData("do");
    }

    [Fact]
    public void synthetic_if()
    {
        checkData("if");
    }

    [Fact]
    public void synthetic_if2()
    {
        checkData("if2");
    }

    [Fact]
    public void synthetic_while()
    {
        checkData("while");
    }

    [Fact]
    public void synthetic_unused_vars()
    {
        checkData("unused_vars");
    }

    [Fact]
    public void nested_whiles()
    {
        checkData("nested_whiles");
    }
}
