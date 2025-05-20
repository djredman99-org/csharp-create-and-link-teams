using System.Net.Http.Headers;
using System.Text.Json;

namespace GitHubTeamManager.Services;

public class SCIMService
{
    private readonly HttpClient _httpClient;
    private readonly string _scimToken;
    private readonly string _enterpriseSlug;

    public SCIMService(string baseUrl, string scimToken, string enterpriseSlug)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
        _scimToken = scimToken;
        _enterpriseSlug = enterpriseSlug;
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", scimToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<List<SCIMGroup>> GetGroupsAsync()
    {
        var response = await _httpClient.GetAsync($"scim/v2/enterprises/{_enterpriseSlug}/Groups");
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync();
        var scimResponse = JsonSerializer.Deserialize<SCIMListResponse<SCIMGroup>>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        
        return scimResponse?.Resources ?? new List<SCIMGroup>();
    }
}

public class SCIMListResponse<T>
{
    public List<T> Resources { get; set; } = new();
    public int TotalResults { get; set; }
    public int ItemsPerPage { get; set; }
    public int StartIndex { get; set; }
}

public class SCIMGroup
{
    public string Schemas { get; set; } = "urn:ietf:params:scim:schemas:core:2.0:Group";
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<SCIMGroupMember> Members { get; set; } = new();
    public Meta Meta { get; set; } = new();
}

public class SCIMGroupMember
{
    public string Value { get; set; } = string.Empty;
    public string Ref { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
}

public class Meta
{
    public string ResourceType { get; set; } = string.Empty;
    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; }
    public string Location { get; set; } = string.Empty;
}
