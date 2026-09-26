using Flare.Abstractions;

namespace Merito.Client.Dialogs;

/// <summary>Opens <see cref="ConfirmDialog"/>; a tap on the scrim or Escape answers "no".</summary>
public static class ConfirmDialogExtensions
{
    // Flare 0.42.0 renders IDialogService.ConfirmAsync with scrim and Escape dismissal switched off.
    /// <summary>Asks a yes/no question; true only when the confirming button was pressed.</summary>
    public static async Task<bool> AskAsync(this IDialogService dialogs, string title, string message,
        string confirmText, string cancelText = "Отмена", bool danger = false)
    {
        var result = await dialogs.ShowAsync<ConfirmDialog>(title, new DialogParameters()
            .Add(nameof(ConfirmDialog.Message), message)
            .Add(nameof(ConfirmDialog.ConfirmText), confirmText)
            .Add(nameof(ConfirmDialog.CancelText), cancelText)
            .Add(nameof(ConfirmDialog.Danger), danger), new DialogOptions { Size = DialogSize.Xs });
        return !result.Cancelled;
    }
}
