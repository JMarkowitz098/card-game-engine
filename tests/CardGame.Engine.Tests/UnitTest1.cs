namespace CardGame.Engine.Tests;

public class UnitTest1
{
    [Fact]
    public void Add_TwoPositiveNumbers_ReturnsSum()
    {
        // Arrange
        var sut = new Class1();

        // Act
        int result = sut.Add(2, 2);

        // Assert
        Assert.Equal(4, result);
    }
}