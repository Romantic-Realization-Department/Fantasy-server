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
    public void PlayerController는_POST_메서드만_노출한다()
    {
        var httpMethods = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .Distinct();

        httpMethods.Should().OnlyContain(method => method == "POST");
    }

    [Fact]
    public void PlayerController_라우트_템플릿은_정확히_세_개다()
    {
        var templates = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .Select(a => a.Template);

        templates.Should().BeEquivalentTo(["init", "loadout", "skill/unlock"]);
    }
}
