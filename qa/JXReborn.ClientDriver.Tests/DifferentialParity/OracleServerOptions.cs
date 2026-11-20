namespace JXReborn.ClientDriver.Tests.DifferentialParity;

/// <summary>
/// Environment-driven connection and execution-mode settings for differential parity runs
/// (<see cref="ParityExecutionMode.Live"/>, <see cref="ParityExecutionMode.Record"/>, or <see cref="ParityExecutionMode.Replay"/>).
/// </summary>
public sealed record OracleServerOptions(
    string Host,
    int BishopPort,
    string Account,
    string Password,
    string RoleName,
    string Profile = "minimal",
    string? ContainerName = null,
    ParityExecutionMode Mode = ParityExecutionMode.Live,
    string? PackageArchiveSha256 = null)
{
    public bool LegacyNewbieBoostEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("LEGACY_ORACLE_NEWBIE_BOOST"), "1", StringComparison.Ordinal);

    public bool ShouldRunLiveOracle => Mode is ParityExecutionMode.Live or ParityExecutionMode.Record;

    public bool ShouldRecordBaseline => Mode == ParityExecutionMode.Record;

    public bool IsReplayMode => Mode == ParityExecutionMode.Replay;

    public static OracleServerOptions? TryFromEnvironment() =>
        TryFromEnvironment(Environment.GetEnvironmentVariable);

    public static OracleServerOptions? TryFromEnvironment(Func<string, string?> getEnv)
    {
        ArgumentNullException.ThrowIfNull(getEnv);

        ParityExecutionMode mode = ParseMode(getEnv("LEGACY_PARITY_MODE"));
        string? host = getEnv("LEGACY_ORACLE_HOST");
        string account = getEnv("LEGACY_ORACLE_ACCOUNT") ?? "parity_test1";
        string password = getEnv("LEGACY_ORACLE_PASSWORD") ?? "pass123";
        string roleName = getEnv("LEGACY_ORACLE_ROLE_NAME") ?? "ParityHero";
        string profile = (getEnv("LEGACY_ORACLE_PROFILE") ?? "minimal").Trim().ToLowerInvariant();
        string? container = getEnv("LEGACY_ORACLE_CONTAINER");
        string? packageSha256 = getEnv("LEGACY_ORACLE_PACKAGE_SHA256");

        if (mode == ParityExecutionMode.Replay)
        {
            return new OracleServerOptions(
                Host: string.IsNullOrWhiteSpace(host) ? string.Empty : host.Trim(),
                BishopPort: 0,
                Account: account,
                Password: password,
                RoleName: roleName,
                Profile: profile,
                ContainerName: null,
                Mode: ParityExecutionMode.Replay,
                PackageArchiveSha256: string.IsNullOrWhiteSpace(packageSha256) ? null : packageSha256.Trim().ToLowerInvariant());
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            if (mode == ParityExecutionMode.Record)
            {
                throw new InvalidOperationException(
                    "LEGACY_PARITY_MODE=Record requires LEGACY_ORACLE_HOST to be configured against a live T=0 legacy oracle container.");
            }

            return null;
        }

        int port = int.TryParse(getEnv("LEGACY_ORACLE_PORT"), out int parsedPort) && parsedPort > 0
            ? parsedPort
            : 5622;

        return new OracleServerOptions(
            Host: host.Trim(),
            BishopPort: port,
            Account: account,
            Password: password,
            RoleName: roleName,
            Profile: profile,
            ContainerName: string.IsNullOrWhiteSpace(container) ? null : container.Trim(),
            Mode: mode,
            PackageArchiveSha256: string.IsNullOrWhiteSpace(packageSha256) ? null : packageSha256.Trim().ToLowerInvariant());
    }

    private static ParityExecutionMode ParseMode(string? rawMode)
    {
        if (string.IsNullOrWhiteSpace(rawMode))
        {
            return ParityExecutionMode.Live;
        }

        return rawMode.Trim().ToLowerInvariant() switch
        {
            "live" => ParityExecutionMode.Live,
            "record" => ParityExecutionMode.Record,
            "replay" => ParityExecutionMode.Replay,
            _ => throw new InvalidOperationException(
                $"Unsupported LEGACY_PARITY_MODE value '{rawMode}'. Expected 'Live', 'Record', or 'Replay'.")
        };
    }
}
