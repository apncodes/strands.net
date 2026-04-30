using Microsoft.Extensions.DependencyInjection;
using Strands.AgentCore.Extensions;
using Strands.AgentCore.Session;
using Strands.AgentCore.Tools;
using Strands.Core;
using Xunit;

namespace Strands.AgentCore.Tests;

public sealed class DiTests
{
    [Fact]
    public void AddAgentCoreSessionManager_RegistersISessionManager()
    {
        var services = new ServiceCollection();
        services.AddAgentCoreSessionManager("mem-id-123");

        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<ISessionManager>();

        Assert.IsType<AgentCoreSessionManager>(manager);
    }

    [Fact]
    public void AddAgentCoreMemory_RegistersITool()
    {
        var services = new ServiceCollection();
        services.AddAgentCoreMemory("mem-id-456");

        var provider = services.BuildServiceProvider();
        var tools = provider.GetServices<ITool>().ToList();

        Assert.Single(tools);
        Assert.IsType<AgentCoreMemoryTool>(tools[0]);
    }

    [Fact]
    public void AddAgentCoreBrowser_RegistersITool()
    {
        var services = new ServiceCollection();
        services.AddAgentCoreBrowser();

        var provider = services.BuildServiceProvider();
        var tools = provider.GetServices<ITool>().ToList();

        Assert.Single(tools);
        Assert.IsType<AgentCoreBrowserTool>(tools[0]);
    }

    [Fact]
    public void AddAgentCoreCodeInterpreter_RegistersITool()
    {
        var services = new ServiceCollection();
        services.AddAgentCoreCodeInterpreter();

        var provider = services.BuildServiceProvider();
        var tools = provider.GetServices<ITool>().ToList();

        Assert.Single(tools);
        Assert.IsType<AgentCoreCodeInterpreterTool>(tools[0]);
    }

    [Fact]
    public void AddMultipleAgentCoreTools_AllRegisteredAsITool()
    {
        var services = new ServiceCollection();
        services
            .AddAgentCoreMemory("mem-id")
            .AddAgentCoreBrowser()
            .AddAgentCoreCodeInterpreter();

        var provider = services.BuildServiceProvider();
        var tools = provider.GetServices<ITool>().ToList();

        Assert.Equal(3, tools.Count);
        Assert.Contains(tools, t => t is AgentCoreMemoryTool);
        Assert.Contains(tools, t => t is AgentCoreBrowserTool);
        Assert.Contains(tools, t => t is AgentCoreCodeInterpreterTool);
    }
}
