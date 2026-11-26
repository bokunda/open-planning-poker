using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

namespace OpenPlanningPoker.UserManagement.GraphQL.Localization;

/// <summary>
/// Error messages in the caller's language (Accept-Language), looked up by the error code (OPP-24). Wraps the
/// resolver of <c>ApplicationError.message</c>, so the schema stays the same and every error is covered.
/// </summary>
public sealed class ApplicationErrorMessageInterceptor : TypeInterceptor
{
    /// <summary>
    /// <c>ApplicationError.Message</c> is nullable in the shared package, but the <c>Error</c> interface of the
    /// mutation conventions declares <c>message: String!</c>, which Hot Chocolate 16 enforces. The middleware below
    /// always returns a message (falling back to the code), so the field is declared non-null.
    /// </summary>
    public override void OnAfterInitialize(ITypeDiscoveryContext discoveryContext, TypeSystemConfiguration configuration)
    {
        if (configuration is not ObjectTypeConfiguration { Name: "ApplicationError" } type) return;
        var message = type.Fields.FirstOrDefault(field => field.Name == "message");
        if (message is not null) message.Type = TypeReference.Parse("String!");
    }

    public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, TypeSystemConfiguration configuration)
    {
        if (configuration is not ObjectTypeConfiguration { Name: "ApplicationError" } type) return;
        var message = type.Fields.FirstOrDefault(field => field.Name == "message");
        message?.MiddlewareConfigurations.Add(new FieldMiddlewareConfiguration(next => async context =>
        {
            await next(context);
            if (context.Parent<object>() is ApplicationError error)
            {
                context.Result = ServerText.ErrorMessage(error.Code, context.Result as string ?? error.Message);
            }
        }));
    }
}
