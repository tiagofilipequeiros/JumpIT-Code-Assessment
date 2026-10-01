using System.Globalization;

namespace Backend.Services;

// Builds a short human-readable summary of what changed, e.g. "Price 10.00 → 12.50; Stock 5 → 7".
public class ChangeList
{
    private readonly List<string> _changes = [];

    public void Add<T>(string field, T oldValue, T newValue)
    {
        if (!EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            // Invariant culture: metrics read the same on every server ("12.50", never "12,50").
            _changes.Add(string.Create(CultureInfo.InvariantCulture, $"{field} {oldValue} → {newValue}"));
        }
    }

    public override string ToString() => _changes.Count == 0 ? "No changes" : string.Join("; ", _changes);
}
