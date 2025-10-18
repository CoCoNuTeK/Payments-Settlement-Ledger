using System.Collections;

namespace PaymentsLedger.Blazor.Domain.Abstractions;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public bool Equals(ValueObject? other) => Equals((object?)other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 0;
            foreach (var component in GetEqualityComponents())
            {
                hash ^= component?.GetHashCode() ?? 0;
            }
            return hash;
        }
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}

