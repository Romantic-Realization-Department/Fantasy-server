using System.Reflection;
using Fantasy.Server.Domain.Player.Controller;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Fantasy.Test.Player.Controller;

public class PlayerControllerRouteTests
{
    private static readonly MethodInfo[] Actions = typeof(PlayerController)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

    [Fact]
    public void PlayerController에_PATCH_액션이_없다()
    {
        var hasPatch = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .Any(method => method.Equals("PATCH", StringComparison.OrdinalIgnoreCase));

        hasPatch.Should().BeFalse();
    }

    [Fact]
    public void PlayerController는_GET과_POST만_노출한다()
    {
        var httpMethods = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .Distinct();

        httpMethods.Should().BeEquivalentTo(["GET", "POST"]);
    }

    [Fact]
    public void 루트_경로에_GET_로드와_POST_생성이_있다()
    {
        var rootMethods = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .Where(a => a.Template == null)
            .SelectMany(a => a.HttpMethods);

        rootMethods.Should().BeEquivalentTo(["GET", "POST"]);
    }

    [Fact]
    public void 하위_경로_템플릿은_loadout과_skill_unlock이다()
    {
        var templates = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .Select(a => a.Template)
            .Where(t => t != null);

        templates.Should().BeEquivalentTo(["loadout", "skill/unlock"]);
    }
}
