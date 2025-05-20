namespace GitHubTeamManager.Config;

public class AppSettings
{
    public string GitHubToken { get; set; } = string.Empty;
    public string EnterpriseSlug { get; set; } = string.Empty;
    public string GitHubOrganization { get; set; } = string.Empty;
    public string SCIMToken { get; set; } = string.Empty;
    public string SCIMBaseUrl { get; set; } = string.Empty;
}
