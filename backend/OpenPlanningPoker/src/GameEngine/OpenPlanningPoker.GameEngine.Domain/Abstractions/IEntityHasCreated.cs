namespace OpenPlanningPoker.GameEngine.Domain.Abstractions;

public interface IEntityHasCreated
{
    void SetCreated(DateTimeOffset createdOn, Guid createdBy);
}