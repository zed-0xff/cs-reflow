using System.Text;
using Xunit.Sdk;

internal static class StringAssert
{
    private const string ExpectedLabel = "Expected: ";
    private const string ActualLabel = "Actual  : ";

    internal static void Equal(string expected, string actual)
    {
        if (expected == null || actual == null)
        {
            Assert.Equal(expected, actual);
            return;
        }

        var expectedLines = expected.Replace("\r\n", "\r").Split(['\n', '\r']);
        var actualLines = actual.Replace("\r\n", "\r").Split(['\n', '\r']);

        if (expectedLines.Length == 1 && actualLines.Length == 1)
        {
            Assert.Equal(expected, actual);
            return;
        }

        var minLines = Math.Min(expectedLines.Length, actualLines.Length);

        // Compare each line content
        for (var i = 0; i < minLines; i++)
        {
            var expectedLine = expectedLines[i];
            var actualLine = actualLines[i];

            if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
            {
                // Find the position where characters start to differ
                var mismatchIndex = GetMismatchIndex(expectedLine, actualLine);

                var content = GetContent(expectedLines, i, expectedLine, actualLine, mismatchIndex);

                throw new XunitException($"StingAssert.Equal() Failure: line {i + 1}, pos {mismatchIndex + 1} differ:\n{content}");
            }
        }

        // Check line count mismatch
        if (expectedLines.Length != actualLines.Length)
        {
            if (expectedLines.Length > actualLines.Length)
            {
                throw new XunitException($"Line count mismatch: Expected has more lines. Actual had {expectedLines.Length} lines, but Expected has {actualLines.Length}. First missing line in Expected is line {expectedLines.Length + 1}: '{expectedLines[actualLines.Length]}'");
            }
            else
            {
                throw new XunitException($"Line count mismatch: Actual has more lines. Expected had {actualLines.Length} lines, but Actual has {expectedLines.Length}. First extra line is line {actualLines.Length + 1}: '{actualLines[expectedLines.Length]}'");
            }
        }

        // Final string equality check to handle different line endings
        Assert.Equal(expected, actual);
    }

    private static string GetContent(string[] expectedLines, int i, string expected, string actual, int mismatchIndex)
    {
        var bob = new StringBuilder();
        var start = Math.Max(0, i - 2);
        var end = Math.Min(expectedLines.Length - 1, i + 2);

        for (var k = start; k <= end; k++)
        {
            if (k < i)
            {
                bob.Append(' ', ExpectedLabel.Length);
                bob.AppendLine($"[{k + 1}] {expectedLines[k]}");
            }
            else if (k == i)
            {
                var labelLength = ($"[{k + 1}]{ExpectedLabel} ").Length;
                bob.Append(' ', labelLength + mismatchIndex);
                bob.AppendLine($"↓ (pos {mismatchIndex + 1})");
                bob.AppendLine($"{ExpectedLabel}[{k + 1}] {expected}");
                bob.AppendLine($"{ActualLabel}[{k + 1}] {actual}");
                bob.Append(' ', labelLength + mismatchIndex);
                bob.AppendLine($"↑ (pos {mismatchIndex + 1})");
            }
            else
            {
                bob.Append(' ', 10);
                bob.AppendLine($"[{k + 1}] {expectedLines[k]}");
            }
        }

        return bob.ToString();
    }

    private static int GetMismatchIndex(string expected, string actual)
    {
        for (var j = 0; j < Math.Min(expected.Length, actual.Length); j++)
        {
            if (expected[j] != actual[j])
            {
                return j;
            }
        }

        return -1;
    }
}
