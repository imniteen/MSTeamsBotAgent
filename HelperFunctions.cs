using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProactiveBot.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProactiveBot
{
    public class HelperFunctions
    {
        public async static Task InvokeWorkflow(
            string userQuery,
            string myproductPluginKey = null,
            string myproductPluginName = null,
            bool isAction = false,
            string workflowName = null,
            string token = null)
        {
            string url = $"https://aimlivt-business-api.myproduct.com/ExploreAIService/v3/myproductcopilot/query";
            var jsonData = new
            {
                data = new
                {
                    queryText = userQuery,
                    isUserQuery = false,
                    requestId = "",
                    stream = true
                }
            };

            JObject jsonObject = JObject.FromObject(jsonData);
            if (jsonObject["data"] != null)
            {
                jsonObject["data"]["userSelections"] = new JArray();
                if (!string.IsNullOrEmpty(myproductPluginKey))
                {
                    jsonObject["data"]["userSelections"] = JArray.FromObject(new List<string>()
                    {
                        myproductPluginKey
                    });
                }

                List<NameValueJObject> additionalContext = new List<NameValueJObject>();
                additionalContext.Add(new NameValueJObject()
                {
                    name = "isCorpusQuery",
                    value = true
                });
                additionalContext.Add(new NameValueJObject()
                {
                    name = "envelopResponseType",
                    value = "seq"
                });

                if (!string.IsNullOrEmpty(workflowName))
                {
                    additionalContext.Add(new NameValueJObject()
                    {
                        name = "workflowName",
                        value = workflowName
                    });
                }

                string actionJsonString = @"
                    {
                        'name': 'Action',
                        'value': {
                            'plugin_id': null,
                            'label': null,
                            'plugin_name': 'get_data',
                            'action_name': null,
                            'parameters': [
                                {
                                    'name': 'query_text',
                                    'description': null,
                                    'value': null,
                                    'type': 'string'
                                },
                                {
                                    'name': 'additional_context_filters',
                                    'description': null,
                                    'value': null,
                                    'type': 'object'
                                },
                                {
                                    'name': 'rephrased_query',
                                    'description': null,
                                    'value': null,
                                    'type': 'string'
                                }
                            ]
                        }
                    }";
                JObject actionJsonObj = JObject.Parse(actionJsonString);
                actionJsonObj["value"]["plugin_name"] = myproductPluginName;

                NameValueJObject nameValueObject = new NameValueJObject
                {
                    name = actionJsonObj["name"].ToString(),
                    value = actionJsonObj["value"]
                };
                additionalContext.Add(nameValueObject);

                jsonObject["data"]["additionalContext"] = JArray.FromObject(additionalContext);
            }

            string jsonString = JsonConvert.SerializeObject(jsonObject);

            StringContent content = new(jsonString, Encoding.UTF8, "application/json");

            using HttpClient httpClient = new();
            httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("IcmAuthToken", $"{token}");
            HttpRequestMessage request = new(HttpMethod.Post, url)
            {
                Content = content
            };

            
            using (HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                default(CancellationToken)))
            {
                using (Stream stream = await response.Content.ReadAsStreamAsync(default(CancellationToken)))
                {
                    using (StreamReader reader = new(stream))
                    {
                    }
                }
            }
        }

        public static string GetWorkflowName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Find the position of the last '/' and the ':' delimiter
            int startIndex = input.IndexOf('/') + 1; // Start after the '/'
            int endIndex = input.IndexOf(':', startIndex); // Find ':' after the '/'

            if (startIndex > 0)
            {
                if (endIndex > startIndex)
                {
                    // Extract substring between '/' and ':'
                    return input.Substring(startIndex, endIndex - startIndex).Trim();
                }
                else
                {
                    // Extract from '/' to the end if ':' is not found
                    return input.Substring(startIndex).Trim();
                }
            }

            return string.Empty;
        }
    }
}
