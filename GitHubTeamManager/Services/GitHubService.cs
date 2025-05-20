using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Octokit;

namespace GitHubTeamManager.Services;

public class GitHubService
{
    private readonly GitHubClient _gitHubClient;
    private readonly HttpClient _httpClient;
    private readonly string _organizationName;

    public GitHubService(string token, string enterpriseSlug, string scimToken)
    {
        _gitHubClient = new GitHubClient(new ProductHeaderValue("GitHubTeamManager"))
        {
            Credentials = new Credentials(token)
        };
        _organizationName = enterpriseSlug;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com")
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubTeamManager", "1.0"));
    }

    public async Task<Team> CreateTeamAsync(string orgName, string name, string description = "")
    {
        var newTeam = new NewTeam(name)
        {
            Description = description,
            Privacy = TeamPrivacy.Closed
        };

        return await _gitHubClient.Organization.Team.Create(orgName, newTeam);
    }

    public async Task<Team?> GetTeamByNameAsync(string orgName, string name)
    {
        var teams = await _gitHubClient.Organization.Team.GetAll(orgName);
        return teams.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task LinkTeamToGroupAsync(int teamId, string scimGroupId)
    {
        var team = await _gitHubClient.Organization.Team.Get(teamId);
        if (team == null)
        {
            throw new ArgumentException($"Team with ID {teamId} not found");
        }

        var linkRequest = new
        {
            group_id = scimGroupId,
            group_name = team.Name
        };

        var json = JsonSerializer.Serialize(linkRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PatchAsync(
            $"/orgs/{_organizationName}/teams/{team.Slug}/external-groups",
            content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to link team to group. Status: {response.StatusCode}, Error: {errorContent}");
        }
    }
}
