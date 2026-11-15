using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Bookstore.Admin.Services;

public sealed record StaffNote(string Text, DateTime CreatedAtUtc);

// Notes the staff leave on a book. Kept in the panel's memory for now: shared by every user, lost on restart.
public sealed class StaffNoteStore(TimeProvider clock)
{
    private readonly ConcurrentDictionary<string, ImmutableList<StaffNote>> notes = new();

    public IReadOnlyList<StaffNote> GetNotes(string isbn) =>
        notes.TryGetValue(isbn, out ImmutableList<StaffNote>? list) ? list : [];

    public StaffNote Add(string isbn, string text)
    {
        StaffNote note = new(text.Trim(), clock.GetUtcNow().UtcDateTime);
        notes.AddOrUpdate(isbn, _ => [note], (_, list) => list.Add(note));
        return note;
    }
}
