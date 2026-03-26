namespace CalqFramework.Dev.Tests;

/// <summary>
///     Tests for GitHelper utility methods.
/// </summary>
public class GitHelperTest {
    [Theory]
    [InlineData("issues/42", 42)]
    [InlineData("issues/1", 1)]
    [InlineData("feature/123", 123)]
    public void ExtractIssueId_NumericSuffix_ReturnsId(string branch, int expected) {
        int? result = GitHelper.ExtractIssueId(branch);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("main")]
    [InlineData("feature/no-number")]
    public void ExtractIssueId_NoNumericSuffix_ReturnsNull(string branch) {
        int? result = GitHelper.ExtractIssueId(branch);
        Assert.Null(result);
    }
}
