using Merito.Shared;

namespace Merito.Server.Tests;

public sealed class PointTextTests
{
    [Theory]
    [InlineData(0, "баллов")]
    [InlineData(1, "балл")]
    [InlineData(2, "балла")]
    [InlineData(5, "баллов")]
    [InlineData(11, "баллов")]
    [InlineData(21, "балл")]
    [InlineData(22, "балла")]
    [InlineData(25, "баллов")]
    [InlineData(-1, "балл")]
    [InlineData(-5, "баллов")]
    public void TheWordFollowsTheNumber(int value, string word) =>
        Assert.Equal(word, PointText.Word(value));

    [Fact]
    public void TheCountJoinsTheNumberAndWord() => Assert.Equal("21 балл", PointText.Of(21));
}
