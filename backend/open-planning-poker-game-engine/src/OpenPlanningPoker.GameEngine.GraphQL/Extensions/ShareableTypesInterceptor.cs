using HotChocolate.Configuration;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace OpenPlanningPoker.GameEngine.GraphQL.Extensions;

/// <summary>
/// Types that both subgraphs define the same way are @shareable, so the Fusion gateway can compose them:
/// <c>ApplicationError</c> from the shared package and <c>PingPayload</c> of the health-check mutation.
/// </summary>
public sealed class ShareableTypesInterceptor : TypeInterceptor
{
    private static readonly HashSet<string> SharedTypes = ["ApplicationError", "PingPayload"];

    public override void OnBeforeRegisterDependencies(ITypeDiscoveryContext discoveryContext, TypeSystemConfiguration configuration)
    {
        if (configuration is ObjectTypeConfiguration type && SharedTypes.Contains(type.Name))
        {
            type.Directives.Add(new DirectiveConfiguration(
                Shareable.Instance,
                TypeReference.CreateDirective(discoveryContext.TypeInspector.GetType(typeof(Shareable)))));
        }
    }
}
