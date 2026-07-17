using Milu.Web.Infrastructure.Routing;
using Microsoft.Extensions.Options;

namespace Milu.Web.Tests.Routing;

public sealed class MiluRouteParserTests
{
    private readonly MiluRouteParser _parser = new(
        Options.Create(new MiluOptions
        {
            StartModule = "sample"
        }));

    [Fact]
    public void Parse_UsesConfiguredStartModuleForEmptyPath()
    {
        var result = _parser.Parse(string.Empty);

        Assert.NotNull(result);
        Assert.False(result.IsAdmin);
        Assert.Equal("sample", result.Module);
        Assert.Equal("index", result.Controller);
        Assert.Equal("index", result.Action);
    }

    [Fact]
    public void Parse_RecognizesFrontendRouteAndKeyValueParameters()
    {
        var result = _parser.Parse("sample/index/hello/name/Ada/id/42");

        Assert.NotNull(result);
        Assert.False(result.IsAdmin);
        Assert.Equal("sample", result.Module);
        Assert.Equal("index", result.Controller);
        Assert.Equal("hello", result.Action);
        Assert.Equal("Ada", result.Parameters["name"]);
        Assert.Equal("42", result.Parameters["id"]);
    }

    [Fact]
    public void Parse_RecognizesAdminPrefix()
    {
        var result = _parser.Parse("admin/sample/index/index");

        Assert.NotNull(result);
        Assert.True(result.IsAdmin);
        Assert.Equal("sample", result.Module);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("sample/index/hello/name")]
    [InlineData("sample/index/hello/name/Ada/name/Bob")]
    [InlineData("sample/index/hello/action/Index")]
    [InlineData("sample/index/hello/name/Ada-Example")]
    public void Parse_RejectsAmbiguousOrInvalidRoutes(string path)
    {
        Assert.Null(_parser.Parse(path));
    }
}
