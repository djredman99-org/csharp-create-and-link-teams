# GitHub Team Manager

This application automates team management in GitHub Enterprise by fetching external groups through the GitHub Teams External Groups API (for EMU accounts). It performs two main functions:
1. Retrieves external groups from the GitHub Enterprise instance
2. Creates and links GitHub Teams to these external groups automatically
3. Ignores IdP groups if a Team already exists with that group name

## Prerequisites

- .NET 8.0 SDK or later
- GitHub Enterprise EMU account
- GitHub Personal Access Token with `admin:org` scope
- Organization admin access

## Configuration

Create an `appsettings.json` file in the project directory with the following structure:

```json
{
  "GitHub": {
    "Token": "your-github-token",
    "OrganizationName": "your-organization-name",
    "BaseUrl": "https://api.github.com"
  }
}
```

Replace the placeholder values with your actual configuration:

- `Token`: A GitHub Personal Access Token with `admin:org` scope
- `OrganizationName`: Your GitHub organization name
- `BaseUrl`: The base URL for GitHub's API (defaults to https://api.github.com)

## Building the Application

```bash
dotnet build
```

## Running the Application

```bash
dotnet run
```

The application will:
1. Connect to your IdP's SCIM endpoint
2. Fetch all available groups
3. For each group:
   - Check if a corresponding GitHub Team exists
   - Create a new team if it doesn't exist
   - Link the team with the IdP group

## Notes

- The application assumes that the SCIM group names should match the GitHub team names
- Teams are created with "Closed" privacy settings by default
- The application logs its progress to the console
