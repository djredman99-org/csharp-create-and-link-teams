namespace GitHubTeamManager.Config;

public class GitHubOptions
{
    public static readonly string SectionName = "GitHub";

    public string APIToken { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string APIBaseUrl { get; set; } = string.Empty;
}
