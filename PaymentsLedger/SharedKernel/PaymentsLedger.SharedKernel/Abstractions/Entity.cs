namespace PaymentsLedger.SharedKernel.Abstractions;

public abstract class Entity : IEquatable<Entity>
{
    protected Entity() { }

    protected Entity(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (Entity)obj;
        return Id.Equals(other.Id);
    }

    public bool Equals(Entity? other) => Equals((object?)other);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
