using Flare.Icons;
using Merito.Shared;

namespace Merito.Client.Layout;

/// <summary>One section of the app as it appears in the side menu and the bottom bar.</summary>
/// <param name="Href">Base-relative route.</param>
/// <param name="Label">Label under or beside the icon.</param>
/// <param name="Icon">Section icon.</param>
public sealed record NavItem(string Href, string Label, FlareIcon Icon);

/// <summary>The sections each role sees; parents manage, children act.</summary>
public static class AppNavigation
{
    private static readonly NavItem[] Parent =
    [
        new("", "Главная", MaterialDesign3Icons.Regular.Home),
        new("review", "Проверка", MaterialDesign3Icons.Regular.TaskAlt),
        new("catalog", "Каталог", MaterialDesign3Icons.Regular.Checklist),
        new("shop", "Покупки", MaterialDesign3Icons.Regular.Storefront),
        new("history", "История", MaterialDesign3Icons.Regular.History),
    ];

    private static readonly NavItem[] Child =
    [
        new("", "Главная", MaterialDesign3Icons.Regular.Home),
        new("tasks", "Сделал", MaterialDesign3Icons.Regular.TaskAlt),
        new("shop", "Магазин", MaterialDesign3Icons.Regular.Storefront),
        new("history", "История", MaterialDesign3Icons.Regular.History),
    ];

    /// <summary>Sections for a role, in display order.</summary>
    public static IReadOnlyList<NavItem> For(FamilyRole role) => role == FamilyRole.Parent ? Parent : Child;
}
