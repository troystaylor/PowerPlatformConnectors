using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Script : ScriptBase
{
    private static readonly string SERVER_NAME = "Microsoft365CopilotMcpServer";
    private static readonly string SERVER_VERSION = "1.0.0";
    
    // Tool definitions - Microsoft 365 Copilot tools
    // Each tool corresponds to an operation in the Swagger definition
    private static readonly JArray AVAILABLE_TOOLS = new JArray
    {
        new JObject
        {
            ["name"] = "retrieve_grounding_data",
            ["description"] = "Retrieve relevant text extracts from SharePoint, OneDrive, or Copilot connectors content for grounding AI responses. Supports natural language queries with filtering and access control.",
            ["inputSchema"] = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["queryString"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Natural language query to retrieve relevant content (max 1,500 characters)",
                        ["maxLength"] = 1500
                    },
                    ["dataSource"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Data source to search: sharePoint, oneDriveBusiness, or externalItem (Copilot connectors)",
                        ["enum"] = new JArray { "sharePoint", "oneDriveBusiness", "externalItem" }
                    },
                    ["filterExpression"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Optional KQL expression to filter results before query execution"
                    },
                    ["maximumResults"] = new JObject
                    {
                        ["type"] = "integer",
                        ["description"] = "Number of results to return (1-25)",
                        ["minimum"] = 1,
                        ["maximum"] = 25,
                        ["default"] = 25
                    }
                },
                ["required"] = new JArray { "queryString", "dataSource" }
            }
        },
        new JObject
        {
            ["name"] = "get_copilot_interactions",
            ["description"] = "Get Microsoft 365 Copilot interaction data for a specific user, including user prompts and Copilot responses across Microsoft 365 apps like Teams, Word, and Outlook.",
            ["inputSchema"] = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["userId"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The unique identifier of the user whose Copilot interactions to retrieve"
                    },
                    ["filter"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Filter interactions by application class (e.g., 'appClass eq \\'IPM.SkypeTeams.Message.Copilot.Teams\\'' or 'appClass eq \\'IPM.SkypeTeams.Message.Copilot.BizChat\\'')"
                    },
                    ["topCount"] = new JObject
                    {
                        ["type"] = "integer",
                        ["description"] = "Number of interactions to return (recommended: 100 for optimal performance)",
                        ["default"] = 100
                    }
                },
                ["required"] = new JArray { "userId" }
            }
        },
        new JObject
        {
            ["name"] = "get_meeting_ai_insights",
            ["description"] = "Get AI-generated insights from a Teams meeting including notes, action items, and participant mentions. Provides comprehensive meeting analysis.",
            ["inputSchema"] = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["userId"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The unique identifier of the user who organized the meeting"
                    },
                    ["onlineMeetingId"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The unique identifier of the online meeting"
                    },
                    ["aiInsightId"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Optional: specific AI insight ID to retrieve detailed insights for a particular insight object"
                    }
                },
                ["required"] = new JArray { "userId", "onlineMeetingId" }
            }
        },
        new JObject
        {
            ["name"] = "get_meeting_transcripts",
            ["description"] = "Get all available Teams meeting transcripts for a user. Useful for accessing meeting content and creating summaries.",
            ["inputSchema"] = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["userId"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The unique identifier of the meeting organizer"
                    },
                    ["filter"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Filter transcripts by organizer or date range"
                    },
                    ["topCount"] = new JObject
                    {
                        ["type"] = "integer",
                        ["description"] = "Number of transcripts to return",
                        ["default"] = 25
                    }
                },
                ["required"] = new JArray { "userId" }
            }
        },
        new JObject
        {
            ["name"] = "get_meeting_recordings",
            ["description"] = "Get all available Teams meeting recordings for a user. Access to recorded meetings for review and analysis.",
            ["inputSchema"] = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["userId"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The unique identifier of the meeting organizer"
                    },
                    ["filter"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Filter recordings by organizer or date range"
                    },
                    ["topCount"] = new JObject
                    {
                        ["type"] = "integer",
                        ["description"] = "Number of recordings to return",
                        ["default"] = 25
                    }
                },
                ["required"] = new JArray { "userId" }
            }
        },
        new JObject
        {
            ["name"] = "list_online_meetings",
            ["description"] = "List online meetings for a specific user to help identify meeting IDs for further analysis or insights retrieval.",
            ["inputSchema"] = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["userId"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The unique identifier of the user whose meetings to retrieve"
                    },
                    ["filter"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Filter meetings by criteria (e.g., startDateTime)"
                    },
                    ["topCount"] = new JObject
                    {
                        ["type"] = "integer",
                        ["description"] = "Number of meetings to return",
                        ["default"] = 25
                    }
                },
                ["required"] = new JArray { "userId" }
            }
        },
        new JObject
        {
            ["name"] = "create_copilot_notification_subscription",
            ["description"] = "Create a subscription to receive change notifications for Copilot AI interactions. Set up webhooks for real-time interaction monitoring.",
            ["inputSchema"] = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["notificationUrl"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The webhook URL that will receive change notifications",
                        ["format"] = "uri"
                    },
                    ["resource"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "The resource path to monitor for changes",
                        ["enum"] = new JArray { "/copilot/interactionHistory/getAllEnterpriseInteractions", "/copilot/users/{userId}/interactionHistory/getAllEnterpriseInteractions" }
                    },
                    ["changeType"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Types of changes to monitor (comma-separated)",
                        ["default"] = "created,updated,deleted"
                    },
                    ["expirationHours"] = new JObject
                    {
                        ["type"] = "integer",
                        ["description"] = "How many hours from now the subscription should expire (max 1 hour)",
                        ["maximum"] = 1,
                        ["default"] = 1
                    }
                },
                ["required"] = new JArray { "notificationUrl", "resource" }
            }
        },
        new JObject
        {
            ["name"] = "get_copilot_admin_settings",
            ["description"] = "Get current Copilot admin settings that control whether users can receive responses to sentiment-related prompts in Teams meetings.",
            ["inputSchema"] = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    // No parameters needed
                }
            }
        },
        new JObject
        {
            ["name"] = "update_copilot_admin_settings",
            ["description"] = "Update Copilot admin settings to control whether users can receive responses to sentiment-related prompts in Teams meetings (limited mode).",
            ["inputSchema"] = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["isEnabledForGroup"] = new JObject
                    {
                        ["type"] = "boolean",
                        ["description"] = "Enable limited mode for users in a specific group. When true, users can ask questions but Copilot won't respond to sentiment-related queries."
                    },
                    ["groupId"] = new JObject
                    {
                        ["type"] = "string",
                        ["description"] = "Microsoft Entra group ID to apply the limited mode setting to (required if isEnabledForGroup is true)"
                    }
                },
                ["required"] = new JArray { "isEnabledForGroup" }
            }
        },
        new JObject
        {
            ["name"] = "list_users",
            ["description"] = "List users in the organization to help identify user IDs for other Copilot operations. This is a utility tool for user discovery.",
            ["inputSchema"] = new JObject
            {
                ["type"] = "object",
                ["properties"] = new JObject
                {
                    ["topCount"] = new JObject
                    {
                        ["type"] = "integer",
                        ["description"] = "Number of users to return",
                        ["default"] = 100
                    }
                }
            }
        }
    };

    // Tool implementations - each tool corresponds to a Microsoft Graph API call
    private async Task<JObject> ExecuteRetrieveGroundingDataTool(JObject arguments)
    {
        var queryString = arguments.GetValue("queryString")?.ToString();
        var dataSource = arguments.GetValue("dataSource")?.ToString();
        var filterExpression = arguments.GetValue("filterExpression")?.ToString();
        var maxResults = arguments.GetValue("maximumResults")?.ToObject<int?>() ?? 25;

        if (string.IsNullOrEmpty(queryString) || string.IsNullOrEmpty(dataSource))
        {
            throw new ArgumentException("queryString and dataSource are required");
        }

        var requestBody = new JObject
        {
            ["queryString"] = queryString,
            ["dataSource"] = dataSource,
            ["maximumNumberOfResults"] = maxResults
        };

        if (!string.IsNullOrEmpty(filterExpression))
        {
            requestBody["filterExpression"] = filterExpression;
        }

        var response = await MakeGraphApiCall("/beta/copilot/retrieval", "POST", requestBody);
        var retrievalHits = response["retrievalHits"] as JArray;

        if (retrievalHits != null && retrievalHits.Count > 0)
        {
            var results = retrievalHits.Take(10).Select(hit =>
            {
                var extracts = hit["extracts"] as JArray;
                var extractTexts = extracts?.Select(e => e["text"]?.ToString()).Where(t => !string.IsNullOrEmpty(t));
                var webUrl = hit["webUrl"]?.ToString();
                var resourceType = hit["resourceType"]?.ToString();

                return $"**Source**: {webUrl}\n**Type**: {resourceType}\n**Content**: {string.Join(" ", extractTexts?.Take(3) ?? new string[0])}";
            });

            return new JObject
            {
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = $"Retrieved {retrievalHits.Count} relevant items from {dataSource}:\n\n" + string.Join("\n\n---\n\n", results)
                    }
                }
            };
        }

        return new JObject
        {
            ["content"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = $"No relevant content found in {dataSource} for query: '{queryString}'"
                }
            }
        };
    }

    private async Task<JObject> ExecuteGetCopilotInteractionsTool(JObject arguments)
    {
        var userId = arguments.GetValue("userId")?.ToString();
        var filter = arguments.GetValue("filter")?.ToString();
        var topCount = arguments.GetValue("topCount")?.ToObject<int?>() ?? 100;

        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("userId is required");
        }

        var endpoint = $"/v1.0/copilot/users/{userId}/interactionHistory/getAllEnterpriseInteractions";
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(filter))
            queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
        
        queryParams.Add($"$top={topCount}");
        
        if (queryParams.Count > 0)
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

        var response = await MakeGraphApiCall(endpoint, "GET");
        var interactions = response["value"] as JArray;

        if (interactions != null && interactions.Count > 0)
        {
            var interactionList = interactions.Take(10).Select(interaction =>
            {
                var appClass = interaction["appClass"]?.ToString();
                var interactionType = interaction["interactionType"]?.ToString();
                var createdDateTime = DateTime.Parse(interaction["createdDateTime"]?.ToString()).ToString("MMM dd, yyyy h:mm tt");
                var bodyContent = interaction["body"]?["content"]?.ToString();
                var from = interaction["from"]?["user"]?["displayName"]?.ToString() ?? 
                          interaction["from"]?["application"]?["displayName"]?.ToString();

                return $"**{interactionType}** by {from}\n**App**: {appClass}\n**Time**: {createdDateTime}\n**Content**: {(string.IsNullOrEmpty(bodyContent) ? "" : bodyContent.Substring(0, Math.Min(200, bodyContent.Length)))}...";
            });

            return new JObject
            {
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = $"Copilot Interactions for User ({interactions.Count} total):\n\n" + string.Join("\n\n---\n\n", interactionList)
                    }
                }
            };
        }

        return new JObject
        {
            ["content"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = $"No Copilot interactions found for user {userId}"
                }
            }
        };
    }

    private async Task<JObject> ExecuteGetMeetingAiInsightsTool(JObject arguments)
    {
        var userId = arguments.GetValue("userId")?.ToString();
        var onlineMeetingId = arguments.GetValue("onlineMeetingId")?.ToString();
        var aiInsightId = arguments.GetValue("aiInsightId")?.ToString();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(onlineMeetingId))
        {
            throw new ArgumentException("userId and onlineMeetingId are required");
        }

        string endpoint;
        if (!string.IsNullOrEmpty(aiInsightId))
        {
            endpoint = $"/beta/copilot/users/{userId}/onlineMeetings/{onlineMeetingId}/aiInsights/{aiInsightId}";
        }
        else
        {
            endpoint = $"/beta/copilot/users/{userId}/onlineMeetings/{onlineMeetingId}/aiInsights";
        }

        var response = await MakeGraphApiCall(endpoint, "GET");

        if (!string.IsNullOrEmpty(aiInsightId))
        {
            // Single detailed insight
            var meetingNotes = response["meetingNotes"] as JArray;
            var actionItems = response["actionItems"] as JArray;
            var mentions = response["viewpoint"]?["mentionEvents"] as JArray;

            var notesText = meetingNotes?.Take(5).Select(note => $"• {note["title"]}: {note["text"]}").ToList() ?? new List<string>();
            var actionText = actionItems?.Take(5).Select(action => $"• {action["title"]} (Owner: {action["ownerDisplayName"]})").ToList() ?? new List<string>();
            var mentionText = mentions?.Take(5).Select(mention => 
            {
                var utterance = mention["transcriptUtterance"]?.ToString();
                var truncated = string.IsNullOrEmpty(utterance) ? "" : utterance.Substring(0, Math.Min(100, utterance.Length));
                return $"• {mention["speaker"]?["user"]?["displayName"]}: {truncated}...";
            }).ToList() ?? new List<string>();

            return new JObject
            {
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = $"**AI Insights for Meeting**\n\n**Meeting Notes:**\n{string.Join("\n", notesText)}\n\n**Action Items:**\n{string.Join("\n", actionText)}\n\n**Key Mentions:**\n{string.Join("\n", mentionText)}"
                    }
                }
            };
        }
        else
        {
            // List of insights
            var insights = response["value"] as JArray;
            if (insights != null && insights.Count > 0)
            {
                var insightList = insights.Select(insight =>
                {
                    var id = insight["id"]?.ToString();
                    var createdTime = DateTime.Parse(insight["createdDateTime"]?.ToString()).ToString("MMM dd, h:mm tt");
                    return $"**Insight ID**: {id}\n**Created**: {createdTime}";
                });

                return new JObject
                {
                    ["content"] = new JArray
                    {
                        new JObject
                        {
                            ["type"] = "text",
                            ["text"] = $"Available AI Insights ({insights.Count} total):\n\n" + string.Join("\n\n", insightList) + "\n\n*Use the insight ID with this tool to get detailed insights.*"
                        }
                    }
                };
            }

            return new JObject
            {
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = "No AI insights found for this meeting"
                    }
                }
            };
        }
    }

    private async Task<JObject> ExecuteGetMeetingTranscriptsTool(JObject arguments)
    {
        var userId = arguments.GetValue("userId")?.ToString();
        var filter = arguments.GetValue("filter")?.ToString();
        var topCount = arguments.GetValue("topCount")?.ToObject<int?>() ?? 25;

        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("userId is required");
        }

        var endpoint = $"/v1.0/users/{userId}/onlineMeetings/getAllTranscripts";
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(filter))
            queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
        
        queryParams.Add($"$top={topCount}");
        
        if (queryParams.Count > 0)
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

        var response = await MakeGraphApiCall(endpoint, "GET");
        var transcripts = response["value"] as JArray;

        if (transcripts != null && transcripts.Count > 0)
        {
            var transcriptList = transcripts.Take(10).Select(transcript =>
            {
                var id = transcript["id"]?.ToString();
                var meetingId = transcript["meetingId"]?.ToString();
                var createdTime = DateTime.Parse(transcript["createdDateTime"]?.ToString()).ToString("MMM dd, yyyy h:mm tt");
                var contentUrl = transcript["transcriptContentUrl"]?.ToString();

                return $"**Transcript ID**: {(string.IsNullOrEmpty(id) ? "N/A" : id.Substring(0, Math.Min(8, id.Length)))}...\n**Meeting**: {(string.IsNullOrEmpty(meetingId) ? "N/A" : meetingId.Substring(0, Math.Min(8, meetingId.Length)))}...\n**Created**: {createdTime}\n**URL**: {contentUrl}";
            });

            return new JObject
            {
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = $"Meeting Transcripts ({transcripts.Count} total):\n\n" + string.Join("\n\n---\n\n", transcriptList)
                    }
                }
            };
        }

        return new JObject
        {
            ["content"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = $"No meeting transcripts found for user {userId}"
                }
            }
        };
    }

    private async Task<JObject> ExecuteGetMeetingRecordingsTool(JObject arguments)
    {
        var userId = arguments.GetValue("userId")?.ToString();
        var filter = arguments.GetValue("filter")?.ToString();
        var topCount = arguments.GetValue("topCount")?.ToObject<int?>() ?? 25;

        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("userId is required");
        }

        var endpoint = $"/v1.0/users/{userId}/onlineMeetings/getAllRecordings";
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(filter))
            queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
        
        queryParams.Add($"$top={topCount}");
        
        if (queryParams.Count > 0)
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

        var response = await MakeGraphApiCall(endpoint, "GET");
        var recordings = response["value"] as JArray;

        if (recordings != null && recordings.Count > 0)
        {
            var recordingList = recordings.Take(10).Select(recording =>
            {
                var id = recording["id"]?.ToString();
                var meetingId = recording["meetingId"]?.ToString();
                var createdTime = DateTime.Parse(recording["createdDateTime"]?.ToString()).ToString("MMM dd, yyyy h:mm tt");
                var contentUrl = recording["recordingContentUrl"]?.ToString();

                return $"**Recording ID**: {(string.IsNullOrEmpty(id) ? "N/A" : id.Substring(0, Math.Min(8, id.Length)))}...\n**Meeting**: {(string.IsNullOrEmpty(meetingId) ? "N/A" : meetingId.Substring(0, Math.Min(8, meetingId.Length)))}...\n**Created**: {createdTime}\n**URL**: {contentUrl}";
            });

            return new JObject
            {
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = $"Meeting Recordings ({recordings.Count} total):\n\n" + string.Join("\n\n---\n\n", recordingList)
                    }
                }
            };
        }

        return new JObject
        {
            ["content"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = $"No meeting recordings found for user {userId}"
                }
            }
        };
    }

    private async Task<JObject> ExecuteListOnlineMeetingsTool(JObject arguments)
    {
        var userId = arguments.GetValue("userId")?.ToString();
        var filter = arguments.GetValue("filter")?.ToString();
        var topCount = arguments.GetValue("topCount")?.ToObject<int?>() ?? 25;

        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentException("userId is required");
        }

        var endpoint = $"/v1.0/users/{userId}/onlineMeetings";
        var queryParams = new List<string>();
        
        if (!string.IsNullOrEmpty(filter))
            queryParams.Add($"$filter={Uri.EscapeDataString(filter)}");
        
        queryParams.Add($"$top={topCount}");
        
        if (queryParams.Count > 0)
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

        var response = await MakeGraphApiCall(endpoint, "GET");
        var meetings = response["value"] as JArray;

        if (meetings != null && meetings.Count > 0)
        {
            var meetingList = meetings.Take(10).Select(meeting =>
            {
                var id = meeting["id"]?.ToString();
                var subject = meeting["subject"]?.ToString();
                var startTime = DateTime.Parse(meeting["startDateTime"]?.ToString()).ToString("MMM dd, yyyy h:mm tt");
                var joinUrl = meeting["joinWebUrl"]?.ToString();
                var organizerName = meeting["organizer"]?["identity"]?["user"]?["displayName"]?.ToString();

                return $"**Meeting**: {subject}\n**ID**: {(string.IsNullOrEmpty(id) ? "N/A" : id.Substring(0, Math.Min(8, id.Length)))}...\n**Start**: {startTime}\n**Organizer**: {organizerName}\n**Join URL**: {joinUrl}";
            });

            return new JObject
            {
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = $"Online Meetings ({meetings.Count} total):\n\n" + string.Join("\n\n---\n\n", meetingList)
                    }
                }
            };
        }

        return new JObject
        {
            ["content"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = $"No online meetings found for user {userId}"
                }
            }
        };
    }

    private async Task<JObject> ExecuteCreateCopilotNotificationSubscriptionTool(JObject arguments)
    {
        var notificationUrl = arguments.GetValue("notificationUrl")?.ToString();
        var resource = arguments.GetValue("resource")?.ToString();
        var changeType = arguments.GetValue("changeType")?.ToString() ?? "created,updated,deleted";
        var expirationHours = arguments.GetValue("expirationHours")?.ToObject<int?>() ?? 1;

        if (string.IsNullOrEmpty(notificationUrl) || string.IsNullOrEmpty(resource))
        {
            throw new ArgumentException("notificationUrl and resource are required");
        }

        var expirationDateTime = DateTime.UtcNow.AddHours(expirationHours).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        var requestBody = new JObject
        {
            ["changeType"] = changeType,
            ["notificationUrl"] = notificationUrl,
            ["resource"] = resource,
            ["expirationDateTime"] = expirationDateTime
        };

        var response = await MakeGraphApiCall("/v1.0/subscriptions", "POST", requestBody);

        return new JObject
        {
            ["content"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = $"**Copilot Notification Subscription Created**\n\n**Subscription ID**: {response["id"]}\n**Resource**: {resource}\n**Notification URL**: {notificationUrl}\n**Change Types**: {changeType}\n**Expires**: {DateTime.Parse(response["expirationDateTime"]?.ToString()).ToString("MMM dd, yyyy h:mm tt")} UTC\n\n*Webhook will receive notifications for Copilot interaction changes.*"
                }
            }
        };
    }

    private async Task<JObject> ExecuteGetCopilotAdminSettingsTool(JObject arguments)
    {
        var response = await MakeGraphApiCall("/beta/admin/copilot/limitedMode", "GET");

        var isEnabledForGroup = response["isEnabledForGroup"]?.ToObject<bool>() ?? false;
        var groupId = response["groupId"]?.ToString();

        return new JObject
        {
            ["content"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = $"**Copilot Admin Settings (Limited Mode)**\n\n**Limited Mode Enabled**: {isEnabledForGroup}\n**Group ID**: {groupId ?? "Not specified"}\n\n*Limited mode controls whether users can receive responses to sentiment-related prompts in Teams meetings.*"
                }
            }
        };
    }

    private async Task<JObject> ExecuteUpdateCopilotAdminSettingsTool(JObject arguments)
    {
        var isEnabledForGroup = arguments.GetValue("isEnabledForGroup")?.ToObject<bool?>() ?? false;
        var groupId = arguments.GetValue("groupId")?.ToString();

        var requestBody = new JObject
        {
            ["isEnabledForGroup"] = isEnabledForGroup
        };

        if (!string.IsNullOrEmpty(groupId))
        {
            requestBody["groupId"] = groupId;
        }

        var response = await MakeGraphApiCall("/beta/admin/copilot/limitedMode", "PATCH", requestBody);

        return new JObject
        {
            ["content"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = $"**Copilot Admin Settings Updated**\n\n**Limited Mode Enabled**: {response["isEnabledForGroup"]}\n**Group ID**: {response["groupId"] ?? "Not specified"}\n\n*Settings have been applied successfully.*"
                }
            }
        };
    }

    private async Task<JObject> ExecuteListUsersTool(JObject arguments)
    {
        var topCount = arguments.GetValue("topCount")?.ToObject<int?>() ?? 100;

        var endpoint = $"/v1.0/users?$select=id,displayName,userPrincipalName&$top={topCount}";
        var response = await MakeGraphApiCall(endpoint, "GET");
        var users = response["value"] as JArray;

        if (users != null && users.Count > 0)
        {
            var userList = users.Take(20).Select(user =>
            {
                var id = user["id"]?.ToString();
                var displayName = user["displayName"]?.ToString();
                var userPrincipalName = user["userPrincipalName"]?.ToString();

                return $"**{displayName}** ({userPrincipalName})\nID: {id}";
            });

            return new JObject
            {
                ["content"] = new JArray
                {
                    new JObject
                    {
                        ["type"] = "text",
                        ["text"] = $"Organization Users ({users.Count} total, showing first 20):\n\n" + string.Join("\n\n", userList)
                    }
                }
            };
        }

        return new JObject
        {
            ["content"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = "No users found in the organization"
                }
            }
        };
    }

    // Helper method to make Microsoft Graph API calls
    private async Task<JObject> MakeGraphApiCall(string endpoint, string method = "GET", JObject requestBody = null)
    {
        try
        {
            var url = $"https://graph.microsoft.com{endpoint}";
            
            // Create HTTP request message
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            
            // Set required headers
            request.Headers.Add("Accept", "application/json");
            
            // Add request body for POST/PATCH requests
            if (requestBody != null && (method == "POST" || method == "PATCH"))
            {
                request.Content = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json");
            }
            
            // Use the connector's HTTP client (handles authentication automatically)
            var response = await this.Context.SendAsync(request, this.CancellationToken).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                return JObject.Parse(content);
            }
            else
            {
                throw new HttpRequestException($"Microsoft Graph API Error ({response.StatusCode}): {content}");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse Microsoft Graph API response: {ex.Message}");
        }
        catch (HttpRequestException)
        {
            throw; // Re-throw HTTP exceptions as-is
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Microsoft Graph API call failed: {ex.Message}");
        }
    }

    // ****** DO NOT MODIFY BELOW THIS LINE ******
    // Server capabilities - MCP protocol configuration
    // The code below implements the MCP protocol specification and should not be changed
    private static readonly string PROTOCOL_VERSION = "2025-06-18";
    private static bool _isInitialized = false;
    private static readonly JObject SERVER_CAPABILITIES = new JObject
    {
        ["tools"] = new JObject
        {
            ["listChanged"] = true
        }
    };
    
    private static string[] GetToolNames()
    {
        return AVAILABLE_TOOLS.Select(tool => tool["name"]?.ToString()).Where(name => !string.IsNullOrEmpty(name)).ToArray();
    }

    private static string ConvertToMethodName(string toolName)
    {
        if (string.IsNullOrEmpty(toolName)) return "";
        
        var parts = toolName.Split('_');
        var result = new StringBuilder();
        
        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part))
            {
                result.Append(char.ToUpper(part[0]));
                if (part.Length > 1)
                {
                    result.Append(part.Substring(1).ToLower());
                }
            }
        }
        
        return result.ToString();
    }

    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        try
        {
            var operationId = GetOperationId();
            
            if (operationId == "InvokeServer")
            {
                return await HandleMcpRequestAsync().ConfigureAwait(false);
            }
            else
            {
                return CreateJsonRpcErrorResponse(null, -32601, "Method not found", $"Unknown operation ID '{operationId}'");
            }
        }
        catch (JsonException ex)
        {
            return CreateJsonRpcErrorResponse(null, -32700, "Parse error", ex.Message);
        }
        catch (Exception ex)
        {
            return CreateJsonRpcErrorResponse(null, -32603, "Internal error", ex.Message);
        }
    }
    
    private async Task<HttpResponseMessage> HandleMcpRequestAsync()
    {
        var requestBody = await ParseRequestBodyAsync().ConfigureAwait(false);
        
        if (requestBody.Count == 0 || string.IsNullOrEmpty(GetStringProperty(requestBody, "method", "")))
        {
            return await HandleInitializedAsync().ConfigureAwait(false);
        }
        
        var method = GetStringProperty(requestBody, "method", "");
        var requestId = GetRequestId(requestBody);
        
        switch (method)
        {
            case "initialize":
                return await HandleInitializeAsync(requestBody, requestId).ConfigureAwait(false);
            case "notifications/initialized":
                return await HandleInitializedAsync().ConfigureAwait(false);
            case "tools/list":
                return await HandleToolsListAsync(requestId).ConfigureAwait(false);
            case "tools/call":
                return await HandleToolsCallAsync(requestBody, requestId).ConfigureAwait(false);
            default:
                return CreateJsonRpcErrorResponse(requestId, -32601, "Method not found", $"Unknown method '{method}'");
        }
    }
    
    private async Task<HttpResponseMessage> HandleInitializeAsync(JObject requestBody, object requestId)
    {
        try
        {
            var paramsObj = requestBody["params"] as JObject;
            var clientVersion = GetStringProperty(paramsObj, "protocolVersion", "");
            
            if (string.IsNullOrEmpty(clientVersion))
            {
                return CreateJsonRpcErrorResponse(requestId, -32602, "Invalid params", "protocolVersion is required");
            }
            
            var initializeResult = new JObject
            {
                ["protocolVersion"] = PROTOCOL_VERSION,
                ["capabilities"] = SERVER_CAPABILITIES,
                ["serverInfo"] = new JObject
                {
                    ["name"] = SERVER_NAME,
                    ["version"] = SERVER_VERSION
                },
                ["instructions"] = "Microsoft 365 Copilot MCP Server - Access Copilot interaction data, meeting insights, transcripts, recordings, and administrative settings through Microsoft Graph APIs. Supports grounding data retrieval from SharePoint, OneDrive, and Copilot connectors."
            };
            
            return CreateJsonRpcSuccessResponse(requestId, initializeResult);
        }
        catch (Exception ex)
        {
            return CreateJsonRpcErrorResponse(requestId, -32603, "Internal error", ex.Message);
        }
    }
    
    private async Task<HttpResponseMessage> HandleInitializedAsync()
    {
        _isInitialized = true;
        
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var confirmationResponse = new JObject
        {
            ["status"] = "initialized",
            ["message"] = "Microsoft 365 Copilot MCP server initialization complete - ready to handle tool requests",
            ["serverName"] = SERVER_NAME,
            ["serverVersion"] = SERVER_VERSION,
            ["protocolVersion"] = PROTOCOL_VERSION,
            ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ["capabilities"] = new JObject
            {
                ["tools"] = new JArray(GetToolNames())
            }
        };
        
        response.Content = CreateJsonContent(confirmationResponse.ToString());
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return response;
    }
    
    private async Task<HttpResponseMessage> HandleToolsListAsync(object requestId)
    {
        if (!_isInitialized)
        {
            return CreateJsonRpcErrorResponse(requestId, -32002, "Server not initialized", "Must call initialize first");
        }
        
        try
        {
            var result = new JObject
            {
                ["tools"] = AVAILABLE_TOOLS
            };
            
            return CreateJsonRpcSuccessResponse(requestId, result);
        }
        catch (Exception ex)
        {
            return CreateJsonRpcErrorResponse(requestId, -32603, "Internal error", ex.Message);
        }
    }

    private async Task<HttpResponseMessage> HandleToolsCallAsync(JObject requestBody, object requestId)
    {
        if (!_isInitialized)
        {
            return CreateJsonRpcErrorResponse(requestId, -32002, "Server not initialized", "Must call initialize first");
        }
        
        try
        {
            var paramsObj = requestBody["params"] as JObject;
            if (paramsObj == null)
            {
                return CreateJsonRpcErrorResponse(requestId, -32602, "Invalid params", "params object is required");
            }
            
            var toolName = GetStringProperty(paramsObj, "name", "");
            if (string.IsNullOrEmpty(toolName))
            {
                return CreateJsonRpcErrorResponse(requestId, -32602, "Invalid params", "tool name is required");
            }
            
            // Dynamically route tool calls to their implementations
            var methodName = "Execute" + ConvertToMethodName(toolName) + "Tool";
            var method = this.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (method == null)
            {
                return CreateJsonRpcErrorResponse(requestId, -32602, "Invalid params", $"Unknown tool: {toolName}");
            }
            
            var arguments = paramsObj["arguments"] as JObject ?? new JObject();
            
            // Check if method is async
            if (method.ReturnType == typeof(Task<JObject>))
            {
                var task = method.Invoke(this, new object[] { arguments }) as Task<JObject>;
                var result = await task;
                return CreateJsonRpcSuccessResponse(requestId, result);
            }
            else
            {
                var result = method.Invoke(this, new object[] { arguments }) as JObject;
                return CreateJsonRpcSuccessResponse(requestId, result);
            }
        }
        catch (Exception ex)
        {
            return CreateJsonRpcErrorResponse(requestId, -32603, "Internal error", ex.Message);
        }
    }
    
    private string GetOperationId()
    {
        string operationId = this.Context.OperationId;
        
        if (string.IsNullOrEmpty(operationId))
        {
            return "InvokeServer";
        }
        
        if (operationId != "InvokeServer" && IsBase64String(operationId))
        {
            try 
            {
                byte[] data = Convert.FromBase64String(operationId);
                operationId = System.Text.Encoding.UTF8.GetString(data);
            }
            catch (FormatException) 
            {
                // If Base64 decoding fails, use the original value
            }
        }
        
        return operationId;
    }
    
    private bool IsBase64String(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        
        return s.Length % 4 == 0 && 
               System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", System.Text.RegularExpressions.RegexOptions.None);
    }
    
    private async Task<JObject> ParseRequestBodyAsync()
    {
        var contentAsString = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);
        return JObject.Parse(contentAsString);
    }
    
    private object GetRequestId(JObject requestBody)
    {
        var id = requestBody["id"];
        if (id == null) return null;
        if (id.Type == JTokenType.String) return id.ToString();
        if (id.Type == JTokenType.Integer) return id.ToObject<int>();
        if (id.Type == JTokenType.Float) return id.ToObject<double>();
        return id.ToString();
    }
    
    private HttpResponseMessage CreateJsonRpcSuccessResponse(object id, JObject result)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var jsonRpcResponse = new JObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id != null ? JToken.FromObject(id) : null,
            ["result"] = result
        };
        
        response.Content = CreateJsonContent(jsonRpcResponse.ToString());
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return response;
    }
    
    private HttpResponseMessage CreateJsonRpcErrorResponse(object id, int code, string message, string data = null)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var errorObject = new JObject
        {
            ["code"] = code,
            ["message"] = message
        };
        
        if (!string.IsNullOrEmpty(data))
        {
            errorObject["data"] = data;
        }
        
        var jsonRpcResponse = new JObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id != null ? JToken.FromObject(id) : null,
            ["error"] = errorObject
        };
        
        response.Content = CreateJsonContent(jsonRpcResponse.ToString());
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return response;
    }
    
    private string GetStringProperty(JObject json, string propertyName, string defaultValue = "")
    {
        if (json == null) return defaultValue;
        return json[propertyName]?.ToString() ?? defaultValue;
    }
}