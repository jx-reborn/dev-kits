using System.Text.Json.Serialization;

namespace JXReborn.ClientDriver;

/// <summary>
/// <c>KLogin</c> authentication and role selection state snapshot.
/// </summary>
public sealed record LoginStateSnapshot(
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("result")] int Result,
    [property: JsonPropertyName("num_roles")] int NumRoles,
    [property: JsonPropertyName("selected_role_tcvn3_hex")] string SelectedRoleTcvn3Hex,
    [property: JsonPropertyName("roles")] IReadOnlyList<RoleSlotSnapshot> Roles)
{
    [JsonIgnore]
    public string SelectedRoleName => HexTcvn3Decoder.Decode(SelectedRoleTcvn3Hex);
}
