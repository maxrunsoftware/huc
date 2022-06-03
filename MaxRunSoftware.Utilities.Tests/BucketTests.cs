namespace MaxRunSoftware.Utilities.Tests;

public class BucketReadOnlyTests
{
    [Fact]
    public void Testing()
    {
        var cgf = new CacheGenFunc();

        var bro = new BucketCache<string, string>(cgf.GenVal);
        Assert.Equal(0, cgf.TimesCalled);
        Assert.Empty(bro.Keys);

        var val = bro["a"];
        Assert.Equal("Va", val);
        Assert.Equal(1, cgf.TimesCalled);
        Assert.Single(bro.Keys);

        val = bro["a"];
        Assert.Equal("Va", val);
        Assert.Equal(1, cgf.TimesCalled);
        Assert.Single(bro.Keys);

        val = bro["b"];
        Assert.Equal("Vb", val);
        Assert.Equal(2, cgf.TimesCalled);
        Assert.Equal(2, bro.Keys.Count());

        val = bro["a"];
        Assert.Equal("Va", val);
        Assert.Equal(2, cgf.TimesCalled);
        Assert.Equal(2, bro.Keys.Count());

        val = bro["n"];
        Assert.Null(val);
        Assert.Equal(3, cgf.TimesCalled);
        Assert.Equal(3, bro.Keys.Count());

        val = bro["n"];
        Assert.Null(val);
        Assert.Equal(3, cgf.TimesCalled);
        Assert.Equal(3, bro.Keys.Count());

        try
        {
            val = bro[null];
            Assert.True(false, "Expecting exception to be thrown");
        }
        catch (Exception)
        {
            Assert.True(true);
        }
    }

    private class CacheGenFunc
    {
        public int TimesCalled { get; set; }
        public string GenVal(string key)
        {
            TimesCalled++;
            if (key == "n") return null;
            return "V" + key;
        }
    }


}
