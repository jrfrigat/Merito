using Flare.Abstractions;
using Merito.Client.Services;
using Microsoft.AspNetCore.Components;

namespace Merito.Client.Dialogs;

/// <summary>
/// A dialog body that performs its own API call: the dialog stays open and shows the server's message
/// when the call is refused, and closes with a result once it succeeds.
/// </summary>
public abstract class DialogBase : ComponentBase
{
    /// <summary>The live dialog, cascaded by the dialog provider.</summary>
    [CascadingParameter] public FlareDialogInstance Dialog { get; set; } = default!;

    /// <summary>API client.</summary>
    [Inject] protected ApiClient Api { get; set; } = default!;

    /// <summary>The current session.</summary>
    [Inject] protected Session Session { get; set; } = default!;

    /// <summary>Message of the last refused call.</summary>
    protected string? Error { get; set; }

    /// <summary>True while a call is running.</summary>
    protected bool Busy { get; private set; }

    /// <summary>Runs <paramref name="action"/>; closes the dialog on success, keeps it open with the message on failure.</summary>
    protected async Task SubmitAsync(Func<Task> action)
    {
        if (Busy) return;
        Busy = true;
        Error = null;
        try
        {
            await action();
            Dialog.Close();
        }
        catch (ApiException e)
        {
            Error = e.Message;
        }
        finally
        {
            Busy = false;
        }
    }
}
