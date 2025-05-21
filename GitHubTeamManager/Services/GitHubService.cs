using System.ComponentModel;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
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

    private List<Team> _teamCache = null;
    private User _defaultUser = null;

    private async Task<IReadOnlyList<Team>> getTeams()
    {
        if (_teamCache == null)
        {
            _teamCache = [];
            _teamCache.AddRange(await _gitHubClient.Organization.Team.GetAll(this._organizationName));
        }
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
        Team returnedTeam = await _gitHubClient.Organization.Team.Create(this._organizationName, newTeam);
        _teamCache.Add(returnedTeam);
        if (_defaultUser == null)
        {
            IReadOnlyList<User> users = await this._gitHubClient.Organization.Team.GetAllMembers(returnedTeam.Id);
            if (users != null && users.Count > 0)
            {
                _defaultUser = users[0];
            }
        }
        // intentionally not an else to cover corner cases
        if (_defaultUser != null)
        {
            await this._gitHubClient.Organization.Team.RemoveMembership(returnedTeam.Id, _defaultUser.Login);
        }        
        return returnedTeam;

    }


    public async Task<Team?> GetTeamByNameAsync(string name)
    {
        IReadOnlyList<Team> teams = await this.getTeams();
        return teams.FirstOrDefault(t => t.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Team?> GetTeamByIdAsync(long id)
    {
        IReadOnlyList<Team> teams = await this.getTeams();
        return teams.FirstOrDefault(x => x.Id == id);
    }

    public async Task<bool> IsTeamLinkedToGroupAsync(string teamSlug)
    {
        // Make a GET request to check if the team is linked to an external group
        var response = await _httpClient.GetAsync($"/orgs/{_organizationName}/teams/{teamSlug}/external-groups");

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();

            // Parse the response to check if a link exists
            var jsonDocument = JsonDocument.Parse(responseContent);
            var root = jsonDocument.RootElement;

            // Check if the "group_id" field exists in the response
            return root.TryGetProperty("group_id", out _);
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // If the endpoint returns 404, assume no link exists
            return false;
        }
        else
        {
            // Handle other unexpected status codes
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to check team link. Status: {response.StatusCode}, Error: {errorContent}");
        }
    }
    public async Task RemoveAllMembers(long teamId)
    {
        // this is a brute force way of removing the default added user, and makes having groups that are the same name as IDP but do not track a BadIdea(TM)
        // it would be better to only do this on new groups but this will allow it to 'fix' groups that were previously created
        IReadOnlyList<User> users = await _gitHubClient.Organization.Team.GetAllMembers(teamId);
        foreach (var user in users)
        {
            Console.WriteLine($"Removing user {user.Name}");
            _ = await _gitHubClient.Organization.Team.RemoveMembership(user.Id, user.Name);
        }


    }
    public async Task LinkTeamToGroupAsync(string teamSlug, long scimGroupId)
    {
        var linkRequest = new
        {
            group_id = scimGroupId,
        };

        var json = JsonSerializer.Serialize(linkRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PatchAsync(
            $"/orgs/{_organizationName}/teams/{teamSlug}/external-groups",
            content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to link team to group. Status: {response.StatusCode}, Error: {errorContent}");
        }



    }
    // turns out the scim interfaces dont work they give guid ids, not int like the link proc expects
    public async Task<List<ExternalGroup>> GetGroupsAsync()
    {
        var response = await _httpClient.GetAsync($"/orgs/{_organizationName}/external-groups");
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            GroupList<ExternalGroup> list = JsonSerializer.Deserialize<GroupList<ExternalGroup>>(responseContent);
            return list.groups;
        }
        else
        {
            throw new HttpRequestException("Failed to read external groups for org.");
        }
    }
}

public class GroupList<T>
{
    public List<T> groups { get; set; } = [];
}

public class ExternalGroup
{
    public long group_id { get; set; } = 0;
    public string group_name { get; set; } = string.Empty;
}