using System.Reflection;
using Fantasy.Server.Domain.Weapon.Controller;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Fantasy.Test.Weapon.Controller;

public class WeaponControllerRouteTests
{
    private static readonly MethodInfo[] Actions = typeof(WeaponController)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

    [Fact]
    public void WeaponController는_POST만_노출한다()
    {
        var httpMethods = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .Distinct();

        httpMethods.Should().BeEquivalentTo(["POST"]);
    }

    [Fact]
    public void POST_경로_템플릿은_upgrade_synthesize_awaken이다()
    {
        var templates = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .Select(a => a.Template);

        templates.Should().BeEquivalentTo(
            ["{weaponId:int}/upgrade", "{weaponId:int}/synthesize", "{weaponId:int}/awaken"]);
    }
}
