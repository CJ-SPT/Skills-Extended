using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillsExtended.Web.Pages;

public static class AuthorizationChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var pages = typeof(Home).Assembly.GetTypes()
            .Where(type => type.GetCustomAttributes<RouteAttribute>().Any())
            .ToArray();
        check(pages.Length > 0, "Discover production web routes for authorization checks");

        // Match SPT's Administrator policy and cookie principal claim.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationCore(options => options.AddPolicy("Administrator",
            policy => policy.RequireClaim("isAdministrator", "true")));
        services.AddSingleton<NavigationManager, OfflineNavigation>();
        await using var provider = services.BuildServiceProvider();
        var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "ordinary-user")], "Cookies"));
        var admin = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "administrator"), new Claim("isAdministrator", "true")], "Cookies"));

        foreach (var page in pages)
        {
            var attributes = page.GetCustomAttributes<AuthorizeAttribute>().ToArray();
            check(attributes.Any(attribute => attribute.Policy == "Administrator")
                && !page.IsDefined(typeof(AllowAnonymousAttribute), true),
                $"{page.Name} requires the SPT Administrator policy");
            var policy = await AuthorizationPolicy.CombineAsync(policyProvider, attributes);
            check(policy is not null && (await authorization.AuthorizeAsync(admin, null, policy)).Succeeded,
                $"SPT administrator is allowed on {page.Name}");

            foreach (var (principal, name) in new[] { (anonymous, "Anonymous visitor"), (user, "Non-admin user") })
            {
                await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
                var html = await renderer.Dispatcher.InvokeAsync(async () =>
                {
                    var root = await renderer.RenderComponentAsync<RouteHost>(ParameterView.FromDictionary(
                        new Dictionary<string, object?> { [nameof(RouteHost.Page)] = page, [nameof(RouteHost.User)] = principal }));
                    return root.ToHtmlString();
                });
                // No editor services are registered: rendering protected pages or their
                // layout would fail before loading configuration or profile data.
                check(html == "Access denied", $"{name} cannot render {page.Name} or its editor layout");
            }
        }
    }

    public sealed class RouteHost : ComponentBase
    {
        [Parameter] public Type Page { get; set; } = null!;
        [Parameter] public ClaimsPrincipal User { get; set; } = null!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<Task<AuthenticationState>>>(0);
            builder.AddAttribute(1, "Value", Task.FromResult(new AuthenticationState(User)));
            builder.AddAttribute(2, "ChildContent", (RenderFragment)(content =>
            {
                content.OpenComponent<AuthorizeRouteView>(0);
                content.AddAttribute(1, "RouteData", new RouteData(Page, new Dictionary<string, object?>()));
                content.AddAttribute(2, "NotAuthorized", (RenderFragment<AuthenticationState>)(_ => denied => denied.AddContent(0, "Access denied")));
                content.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }
}
