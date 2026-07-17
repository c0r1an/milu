using Milu.Web.Infrastructure.Modules;

namespace Milu.Web.Tests.Modules;

public sealed class ModuleCatalogTests
{
    [Fact]
    public void TryGet_FindsModuleCaseInsensitively()
    {
        var catalog = new ModuleCatalog([new TestModule("sample")]);

        var found = catalog.TryGet("SAMPLE", out var module);

        Assert.True(found);
        Assert.Equal("sample", module.Key);
    }

    [Fact]
    public void Constructor_RejectsDuplicateModuleKeys()
    {
        var modules = new IMiluModule[]
        {
            new TestModule("sample"),
            new TestModule("SAMPLE")
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ModuleCatalog(modules));

        Assert.Contains("sample", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TestModule(string key) : IMiluModule
    {
        public string Key => key;
        public string DisplayName => key;
        public string AreaName => key;
        public string FolderName => key;
        public Version Version => new(1, 0, 0);
    }
}
