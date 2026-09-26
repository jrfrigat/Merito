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

    /// <summary>The result data a dialog closes with after it deleted its record.</summary>
    public const string Deleted = "deleted";

    /// <summary>Dialog service, for confirmations asked from inside a dialog.</summary>
    [Inject] protected IDialogService Dialogs { get; set; } = default!;

    /// <summary>Asks to confirm, runs <paramref name="delete"/> and closes the dialog with <see cref="Deleted"/>.</summary>
    protected async Task DeleteAsync(string title, Func<Task> delete)
    {
        if (Busy || !await ConfirmDialog.AskDeleteAsync(Dialogs, title))
            return;
        Busy = true;
        Error = null;
        try
        {
            await delete();
            Dialog.Close(DialogResult.Ok(Deleted));
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
