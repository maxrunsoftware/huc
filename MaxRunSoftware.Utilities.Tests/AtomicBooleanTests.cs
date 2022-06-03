namespace MaxRunSoftware.Utilities.Tests;

public class AtomicBooleanTests
{
    [Fact]
    public void EqualsWork()
    {
        var ab1 = new AtomicBoolean(true);
        var ab2 = new AtomicBoolean(false);

        var b1 = true;
        var b2 = false;

        Assert.True(ab1.Equals(b1));
        Assert.True(ab2.Equals(b2));
        Assert.True(b1.Equals(ab1));
        Assert.True(b2.Equals(ab2));
        Assert.True(ab1.Equals(new AtomicBoolean(true)));

        Assert.False(ab1.Equals(b2));
        Assert.False(ab2.Equals(b1));
        Assert.False(b1.Equals(ab2));
        Assert.False(b2.Equals(ab1));
        Assert.False(ab1.Equals(ab2));
        Assert.False(ab2.Equals(ab1));

        Assert.False(true);
    }
}
