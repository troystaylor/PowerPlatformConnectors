# Microsoft 365 Copilot
The Microsoft 365 Copilot connector provides comprehensive access to Microsoft 365 Copilot data and insights through Microsoft Graph APIs, enabling organizations to programmatically access Copilot interaction data, meeting insights, transcripts, recordings, and administrative settings while supporting real-time notifications and content grounding capabilities.

## Publisher: Troy Taylor

## Prerequisites
You will need an active Microsoft 365 Copilot license and a Microsoft Entra app registration with appropriate API permissions. Global Administrator access is required for initial setup and consent.

## Obtaining Credentials
You will need to register an application in Microsoft Entra ID (Azure AD) and obtain the following:
1. **Client ID** - From your app registration
2. **Client Secret** - Generated for your app registration
3. **Tenant ID** - Your organization's Azure AD tenant identifier

Configure the following redirect URI in your app registration: `https://global.consent.azure-apim.net/redirect`

The following API permissions are required:
- `Copilot-Analytics.Read.All` (Application)
- `OnlineMeetings.Read.All` (Application)
- `OnlineMeetingTranscript.Read.All` (Application)
- `OnlineMeetingRecording.Read.All` (Application)
- `User.Read.All` (Application)
- `Sites.Read.All` (Application)
- `Files.Read.All` (Application)
- `Copilot.ReadWrite` (Delegated)
- `OnlineMeetings.ReadWrite` (Delegated)
- `User.Read` (Delegated)

## Supported Operations

### Content & Grounding
**Retrieve Grounding Data**: Query SharePoint, OneDrive, or Copilot connectors content for AI response grounding with natural language queries and filtering support.

**Get Copilot Interactions**: Access user Copilot interaction history across Microsoft 365 applications including user prompts and Copilot responses.

### Meeting Intelligence  
**Get Meeting AI Insights**: Extract AI-generated meeting notes, action items, and participant mentions with comprehensive meeting analysis.

**Get Meeting Transcripts**: Access Teams meeting transcripts for content analysis and summaries.

**Get Meeting Recordings**: Retrieve Teams meeting recordings for review and analysis.

**List Online Meetings**: Discover meetings for further analysis or insights retrieval.

### Administration & Monitoring
**Get Copilot Admin Settings**: View current admin settings that control sentiment-related prompt responses in Teams meetings.

**Update Copilot Admin Settings**: Configure admin settings to control whether users can receive responses to sentiment-related prompts in Teams meetings (limited mode).

**Create Notification Subscriptions**: Set up webhooks for real-time Copilot interaction monitoring with customizable change types and expiration.

**List Users**: Utility operation for user discovery and ID resolution within the organization.

## Known Issues and Limitations
- Meeting Insights are only available for meetings where AI features were enabled
- Interaction History is limited to the past 30 days by default
- Admin Settings operations require Global Administrator or Copilot Administrator role
- Grounding Data results depend on content indexing and user permissions
- Some operations marked as "(Preview)" use beta endpoints that may change without notice
- Rate limits apply: 10,000 requests per 10 minutes for general APIs, with additional throttling for Copilot-specific endpoints

## Configuration

### 1. Update API Properties
Edit `apiProperties.json` and replace `your-client-id-here` with your Azure app registration client ID:

```json
{
  "properties": {
    "connectionParameters": {
      "token": {
        "type": "oauthSetting",
        "oAuthSettings": {
          "clientId": "your-actual-client-id"
        }
      }
    }
  }
}
```

### 2. OAuth 2.0 Flow
The connector uses the **Authorization Code flow** with PKCE as recommended by Microsoft Graph:
- Users authenticate with their Microsoft 365 credentials
- Azure AD handles the OAuth flow and token management
- Connector automatically refreshes tokens as needed
- Follows Microsoft Graph authentication best practices

### 3. Update Settings
Edit `settings.json` to configure your target environment:

```json
{
  "environment": "your-environment-guid-here",
  "powerAppsUrl": "https://make.powerapps.com",
  "flowUrl": "https://flow.microsoft.com"
}
```

## Deployment

### Using PAC CLI (Power Apps CLI)

1. **Install PAC CLI** (if not already installed):
   ```powershell
   dotnet tool install --global Microsoft.PowerApps.CLI.Tool
   ```

2. **Authenticate to your environment**:
   ```powershell
   pac auth create --url https://yourorg.crm.dynamics.com
   ```

3. **Deploy the connector**:
   ```powershell
   pac connector create --settings-file settings.json
   ```

### Manual Deployment

1. Navigate to Power Apps maker portal: https://make.powerapps.com
2. Select **Data** > **Custom connectors**
3. Click **New custom connector** > **Import from OpenAPI file**
4. Upload the `apiDefinition.swagger.json` file
5. Configure the security settings with your Azure app registration details
6. Test the connection and save

## Usage Examples

### Power Automate Flow Examples

#### 1. Export Daily Copilot Interactions
```
Trigger: Recurrence (Daily)
Action: Get Copilot Interactions
- User ID: [Select from dynamic content]
- Filter: createdDateTime ge '[previous day]' and createdDateTime lt '[current day]'
```

#### 2. Process Meeting Insights
```
Trigger: When a new meeting ends
Action: Get Meeting AI Insights
- User ID: [Meeting organizer ID]
- Meeting ID: [From trigger]
Action: Parse JSON (Action Items)
Action: Create tasks in project management system
```

#### 3. Compliance Export
```
Trigger: Manual trigger
Action: Get Copilot Interactions
- Filter by application: "appClass eq 'IPM.SkypeTeams.Message.Copilot.Teams'"
Action: Export to SharePoint document library
```

### Power Apps Examples

#### Meeting Insights Dashboard
```
OnStart:
- Call GetMeetingAIInsights for current user
- Display meeting notes in gallery control
- Show action items in data table
- Present mention events in timeline
```

## API Endpoints Reference

### Core Operations

| Operation | Endpoint | Description |
|-----------|----------|-------------|
| Get Copilot Interactions | `/v1.0/copilot/users/{userId}/interactionHistory/getAllEnterpriseInteractions` | Retrieve user's Copilot interactions |
| Get Meeting AI Insights | `/beta/copilot/users/{userId}/onlineMeetings/{onlineMeetingId}/aiInsights` | Get AI insight metadata |
| Get Specific AI Insight | `/beta/copilot/users/{userId}/onlineMeetings/{onlineMeetingId}/aiInsights/{aiInsightId}` | Get detailed insights |
| Get Meeting Transcripts | `/v1.0/users/{userId}/onlineMeetings/getAllTranscripts` | Export meeting transcripts |
| Get Meeting Recordings | `/v1.0/users/{userId}/onlineMeetings/getAllRecordings` | Export meeting recordings |

### Response Examples

#### Copilot Interaction
```json
{
  "id": "interaction-123",
  "userId": "user-456",
  "appClass": "IPM.SkypeTeams.Message.Copilot.Teams",
  "prompt": "Summarize the Q3 financial results",
  "response": "Q3 results show 15% revenue growth...",
  "createdDateTime": "2025-09-12T10:30:00Z"
}
```

#### Meeting AI Insight
```json
{
  "meetingNotes": [
    {
      "title": "Project Status Update",
      "text": "Discussion on current project milestones",
      "subpoints": [
        {
          "title": "Development Progress",
          "text": "Backend API development is 80% complete"
        }
      ]
    }
  ],
  "actionItems": [
    {
      "title": "Review API documentation",
      "text": "Complete review by end of week",
      "ownerDisplayName": "John Smith"
    }
  ]
}
```

## Security and Compliance

### Data Handling
- All data remains within the Microsoft 365 service boundary
- Existing security, compliance, and privacy policies are automatically respected
- Permission trimming ensures users only access data they're authorized to see

### Governance Features
- Sensitivity labels are honored
- Conditional access policies apply
- Audit logging for all API calls
- Data Loss Prevention (DLP) integration

### Best Practices
- Use service principal authentication for production scenarios
- Implement proper error handling and retry logic
- Cache responses appropriately to minimize API calls
- Follow principle of least privilege for permissions

## Troubleshooting

### Common Issues

#### Authentication Errors
- Verify Azure app registration configuration
- Ensure admin consent is granted for all required permissions
- Check redirect URI configuration

#### Permission Denied
- Confirm user has Microsoft 365 Copilot license
- Verify required Graph API permissions are granted
- Check tenant admin policies for API access

#### No Data Returned
- Ensure user has Copilot interactions in the specified timeframe
- Verify meeting insights are available (may take up to 4 hours after meeting ends)
- Check filters and query parameters

#### Rate Limiting
- Implement exponential backoff retry logic
- Respect throttling headers in API responses
- Consider caching frequently accessed data

### API Limitations

#### Meeting AI Insights
- Only available for private scheduled meetings
- Insights may take up to 4 hours to be available after meeting ends
- Application-level permissions not supported (delegated permissions required)

#### Copilot Interactions
- Requires Microsoft 365 Copilot license for each user
- Export limited to interactions user has permission to access
- Some interaction types may not be available in all tenants

## Support and Documentation

### Official Documentation
- [Microsoft 365 Copilot APIs Overview](https://learn.microsoft.com/en-us/microsoft-365-copilot/extensibility/copilot-apis-overview)
- [Meeting AI Insights API](https://learn.microsoft.com/en-us/microsoftteams/platform/graph-api/meeting-transcripts/meeting-insights)
- [Teams Export APIs](https://learn.microsoft.com/en-us/microsoftteams/export-teams-content)

### Community Resources
- [Microsoft 365 Developer Community](https://techcommunity.microsoft.com/t5/microsoft-365-developer-platform/ct-p/Microsoft365DeveloperPlatform)
- [Power Platform Community](https://powerplatform.microsoft.com/en-us/community/)

### Feedback and Issues
For issues with this connector, please check:
1. Microsoft Graph API status page
2. Power Platform service health
3. Azure AD authentication status

## Version History

### Version 1.0.0 (September 2025)
- Initial release
- Support for Copilot Interaction Export API
- Support for Meeting AI Insights API  
- Support for Meeting Transcripts and Recordings export
- OAuth 2.0 authentication with Azure AD
- Dynamic user selection with dropdown
- Comprehensive error handling and response schemas

## License

This connector is provided under the MIT License. See LICENSE file for details.

## Contributing

Contributions are welcome! Please ensure any changes follow the Power Platform connector best practices and include appropriate documentation updates.