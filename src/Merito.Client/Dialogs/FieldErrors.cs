using Flare.Components;

namespace Merito.Client.Dialogs;

/// <summary>Messages of individual form fields, and focus of the first invalid editable field.</summary>
public sealed class FieldErrors
{
    private readonly Dictionary<string, string> _messages = new();

    /// <summary>The message of a field, or null.</summary>
    public string? this[string field]
    {
        get => _messages.GetValueOrDefault(field);
        set
        {
            if (string.IsNullOrWhiteSpace(value)) _messages.Remove(field);
            else _messages[field] = value;
        }
    }

    /// <summary>True when at least one field has a message.</summary>
    public bool HasErrors => _messages.Count > 0;

    /// <summary>Forgets every message.</summary>
    public void Clear() => _messages.Clear();

    /// <summary>Focuses the first field in the given order that has a message.</summary>
    public async Task FocusFirstAsync(params (string Field, FlareEditableFieldBase? Control)[] order)
    {
        foreach (var (field, control) in order)
        {
            if (control is not null && _messages.ContainsKey(field))
            {
                await control.FocusAsync();
                return;
            }
        }
    }
}
