using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GitHubTeamManager.Config;
using Microsoft.Extensions.Options;
using Octokit;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace GitHubTeamManager.Services;

public class GitHubService
{
    private readonly GitHubClient _gitHubClient;
    private readonly HttpClient _httpClient;
    private readonly string _organizationName;

    IReadOnlyList<Team>? _teamCache = null;



    private async Task<IReadOnlyList<Team>> getTeams()
    {
        _teamCache ??= await _gitHubClient.Organization.Team.GetAll(this._organizationName);
        return _teamCache;
    }

    // Copilot oddity, its setting a credential then setting the api header as well
    public GitHubService(IOptions<GitHubOptions> gitHubOptions)
    {
        GitHubOptions ghOptions = gitHubOptions.Value;
        _organizationName = ghOptions.Organization;
        _gitHubClient = new GitHubClient(new ProductHeaderValue("GitHubTeamManager"))
        {
            Credentials = new Credentials(ghOptions.APIToken)
        };


        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(ghOptions.APIBaseUrl)
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ghOptions.APIToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubTeamManager", "1.0"));
    }

    public async Task<Team> CreateTeamAsync(string name, string description = "")
    {
        var newTeam = new NewTeam(name)
        {
            Description = description,
            Privacy = TeamPrivacy.Closed
        };

        return await _gitHubClient.Organization.Team.Create(this._organizationName, newTeam);
    }


    public async Task<Team?> GetTeamByNameAsync(string name)
    {
        IReadOnlyList<Team> teams = await this.getTeams();
        return teams.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Team?> GetTeamByIdAsync(long id)
    {
        IReadOnlyList<Team> teams = await this.getTeams();
        return teams.FirstOrDefault(x=>x.Id == id);
    }
    public async Task LinkTeamToGroupAsync(long teamId, string scimGroupId)
    {
        Team? team = await this.GetTeamByIdAsync(teamId) ?? throw new ArgumentException($"Team with ID {teamId} not found");

        var linkRequest = new
        {
            group_id = scimGroupId,            
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
