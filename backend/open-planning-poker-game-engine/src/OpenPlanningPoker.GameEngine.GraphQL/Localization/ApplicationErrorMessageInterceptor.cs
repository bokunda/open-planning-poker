using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Definitions;
using OpenPlanningPoker.GameEngine.Application.Localization;

namespace OpenPlanningPoker.GameEngine.GraphQL.Localization;

/// <summary>
/// Error messages in the caller's language (Accept-Language), looked up by the error code (OPP-24). Wraps the
/// resolver of <c>ApplicationError.message</c>, so the schema stays the same and every error is covered.
/// </summary>
public sealed class ApplicationErrorMessageInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(ITypeCompletionContext completionContext, DefinitionBase definition)
    {
        if (definition is not ObjectTypeDefinition { Name: "ApplicationError" } type) return;
        var message = type.Fields.FirstOrDefault(field => field.Name == "message");
        message?.MiddlewareDefinitions.Add(new FieldMiddlewareDefinition(next => async context =>
        {
            await next(context);
            if (context.Parent<object>() is ApplicationError error)
            {
                context.Result = ServerText.ErrorMessage(error.Code, context.Result as string ?? error.Message);
            }
        }));
    }
}
