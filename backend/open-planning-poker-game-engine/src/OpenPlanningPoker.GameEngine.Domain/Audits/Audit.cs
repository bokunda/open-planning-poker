namespace OpenPlanningPoker.GameEngine.Domain.Audits;

public sealed class Audit : Entity<Guid>
{
    internal Audit(string name, string description, AuditType type, ObjectType objectType, Guid objectId)
    {
        Name = name;
        Description = description;
        Type = type;
        ObjectType = objectType;
        ObjectId = objectId;
    }

    public string Name { get; set; }
    public string Description { get; set; }
    public AuditType Type { get; set; }
    public ObjectType ObjectType { get; set; }
    public Guid ObjectId { get; set; }

    public static Audit Create(string name, string description, AuditType type, ObjectType objectType, Guid objectId)
        => new (name, description, type, objectType, objectId);
}