using System.Text.Json.Serialization;

namespace Merito.Shared;

/// <summary>The part a person plays in a family; decides which actions the API allows.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<FamilyRole>))]
public enum FamilyRole
{
    /// <summary>Manages the catalogs, confirms submissions and grants or deducts points.</summary>
    Parent = 0,

    /// <summary>Submits done tasks, spends points in the shop and sees only their own history.</summary>
    Child = 1,
}
