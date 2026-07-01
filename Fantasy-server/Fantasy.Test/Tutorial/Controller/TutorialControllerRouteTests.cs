using System.Reflection;
using Fantasy.Server.Domain.Tutorial.Controller;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace Fantasy.Test.Tutorial.Controller;

public class TutorialControllerRouteTests
{
    private static readonly MethodInfo[] Actions = typeof(TutorialController)
        .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

    [Fact]
    public void TutorialController는_GET과_POST만_노출한다()
    {
        var httpMethods = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .Distinct();

        httpMethods.Should().BeEquivalentTo(["GET", "POST"]);
    }

    [Fact]
    public void POST_경로_템플릿은_tutorialId_complete이다()
    {
        var templates = Actions
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .Where(a => a.HttpMethods.Contains("POST"))
            .Select(a => a.Template);

        templates.Should().BeEquivalentTo(["{tutorialId}/complete"]);
    }
}
