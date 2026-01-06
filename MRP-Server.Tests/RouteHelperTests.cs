using MRP_Server.Http.Helpers;
using Xunit;

namespace MRP_Server.Tests;

public class RouteHelperTests
{
    [Theory]
    [InlineData("/api/media/1", "/api/media/", 1)]
    [InlineData("/api/media/42", "/api/media/", 42)]
    [InlineData("/API/MEDIA/7", "/api/media/", 7)] // case-insensitive
    [InlineData("/api/media/0005", "/api/media/", 5)]
    public void TryGetIdAfterPrefix_Valid_ReturnsTrueAndId(string path, string prefix, int expectedId)
    {
        var ok = RouteHelper.TryGetIdAfterPrefix(path, prefix, out var id);

        Assert.True(ok);
        Assert.Equal(expectedId, id);
    }

    [Theory]
    [InlineData("/api/media/", "/api/media/")]            // missing id
    [InlineData("/api/media", "/api/media/")]             // not starting with prefix
    [InlineData("/api/media/ ", "/api/media/")]           // whitespace
    [InlineData("/api/media/abc", "/api/media/")]         // not int
    [InlineData("/api/media/0", "/api/media/")]           // id must be > 0
    [InlineData("/api/media/-1", "/api/media/")]          // negative
    [InlineData("/api/media/12/extra", "/api/media/")]    // extra segment
    [InlineData("/api/media/12/", "/api/media/")]         // trailing slash -> rest contains '/' after Trim? (still safe to treat as invalid)
    public void TryGetIdAfterPrefix_Invalid_ReturnsFalse(string path, string prefix)
    {
        var ok = RouteHelper.TryGetIdAfterPrefix(path, prefix, out var id);

        Assert.False(ok);
        Assert.Equal(0, id);
    }

    [Theory]
    [InlineData("/api/media/123/rate", "/api/media/", "rate", 123)]
    [InlineData("/API/MEDIA/9/RATE", "/api/media/", "rate", 9)] // case-insensitive
    [InlineData("/api/media/0004/rate", "/api/media/", "rate", 4)]
    public void TryGetIdBeforeSuffix_Valid_ReturnsTrueAndId(string path, string prefix, string suffix, int expectedId)
    {
        var ok = RouteHelper.TryGetIdBeforeSuffix(path, prefix, suffix, out var id);

        Assert.True(ok);
        Assert.Equal(expectedId, id);
    }

    [Theory]
    [InlineData("/api/media/123", "/api/media/", "rate")]              // missing suffix
    [InlineData("/api/media/123/rate/extra", "/api/media/", "rate")]   // extra segment
    [InlineData("/api/media/abc/rate", "/api/media/", "rate")]         // non-int
    [InlineData("/api/media/0/rate", "/api/media/", "rate")]           // id must be >0
    [InlineData("/api/medias/1/rate", "/api/media/", "rate")]          // wrong prefix
    public void TryGetIdBeforeSuffix_Invalid_ReturnsFalse(string path, string prefix, string suffix)
    {
        var ok = RouteHelper.TryGetIdBeforeSuffix(path, prefix, suffix, out var id);

        Assert.False(ok);
        Assert.Equal(0, id);
    }
}
