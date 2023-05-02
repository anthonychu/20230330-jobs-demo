using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiController : ControllerBase
{
    private HttpClient httpClient;
    private string jobUrl = Environment.GetEnvironmentVariable("JOB_URL") ?? "";
    private string apiVersion = "2022-11-01-preview";

    public ApiController(IHttpClientFactory httpClientFactory)
    {
        this.httpClient = httpClientFactory.CreateClient();
    }

    [Route("environments")]
    [HttpGet]
    public async Task<IActionResult> EnvironmentsAsync()
    {
        return Ok(Data.Statuses);
    }

    [Route("test")]
    [HttpGet]
    public async Task<IActionResult> TestAsync()
    {
        return Ok(await StartJob("Tailwind Traders"));
    }

    private async Task<dynamic> StartJob(string customerName)
    {
        Console.WriteLine("Starting job");
        
        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com/.default" }));

        var postJson = await GetStartTemplate(customerName, token);

        var startResponse = await httpClient.PostAsync($"{jobUrl}/start?api-version={apiVersion}",
            new StringContent(postJson.ToString(), MediaTypeHeaderValue.Parse("application/json")));

        return startResponse.Content.ReadFromJsonAsync<dynamic>();
    }

    private async Task<JsonObject> GetStartTemplate(string customerName, AccessToken token)
    {
        // call Azure API
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        var getResponse = await httpClient.GetAsync($"{jobUrl}?api-version={apiVersion}");
        var contentJson = await getResponse.Content.ReadFromJsonAsync<JsonObject>();
        var env = contentJson!["properties"]!["template"]!["containers"]![0]!["env"]?.AsArray();
        if (env == null)
        {
            env = new JsonArray();
            contentJson!["properties"]!["template"]!["containers"]![0]!["env"] = env;
        }

        var customerNameElement = env.FirstOrDefault(x => x!["name"]!.ToString() == "CUSTOMER_NAME");
        if (customerNameElement != null)
        {
            customerNameElement!["value"] = customerName;
        }
        else
        {
            env!.Add(new JsonObject
            {
                ["name"] = "CUSTOMER_NAME",
                ["value"] = customerName
            });
        }

        var postJson = new JsonObject();
        postJson["template"] = new JsonObject();
        postJson["template"]!["containers"] = JsonNode.Parse(contentJson!["properties"]!["template"]!["containers"]!.ToString());

        System.Console.WriteLine(postJson.ToString());

        return postJson;
    }

    [Route("environments/{name}")]
    [HttpPut]
    public async Task<IActionResult> CreateEnvironmentAsync(string name)
    {
        var environment = Data.Statuses.FirstOrDefault(x => x.CustomerName == name);
        if (environment == null)
        {
            environment = new Status
            {
                CustomerName = name,
                Steps = new[]
                {
                    new Step
                    {
                        Name = "Queued",
                        Status = "pending"
                    },
                }
            };
            Data.Statuses = Data.Statuses.Prepend(environment);
        }
        await StartJob(name);

        return Ok(environment);
    }

    [Route("status")]
    [HttpPost]
    public async Task<IActionResult> Status([FromBody] Status? status = null)
    {
        if (status == null)
        {
            return BadRequest("Status is null");
        }

        var environment = Data.Statuses.FirstOrDefault(x => x.CustomerName == status.CustomerName);
        if (environment == null)
        {
            return Ok($"Environment {status.CustomerName} not found");
        }

        environment.Steps = status.Steps;

        return Ok("OK");
    }

    [Route("history")]
    [HttpGet]
    public async Task<IActionResult> HistoryAsync()
    {
        var credential = new DefaultAzureCredential();
        var token = await credential.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com/.default" }));

        // call Azure API
        var jobUrl = Environment.GetEnvironmentVariable("CRONJOB_URL");
        var apiVersion = "2022-11-01-preview";
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        var getResponse = await httpClient.GetAsync($"{jobUrl}/executions?api-version={apiVersion}");
        var contentJson = await getResponse.Content.ReadFromJsonAsync<JsonObject>();

        return Ok(contentJson);
    }
}


public class Step
{
    public string Status { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class Status
{
    public string CustomerName { get; set; } = string.Empty;
    public Step[] Steps { get; set; } = Array.Empty<Step>();
}

public static class Data
{
    public static IEnumerable<Status> Statuses { get; set; } = new List<Status>
    {
        new Status
        {
            CustomerName = "Adventure Works Cycles",
            Steps = new[]
            {
                new Step
                {
                    Name = "Provisioned",
                    Status = "success"
                },
            }
        },new Status
        {
            CustomerName = "Fourth Coffee",
            Steps = new[]
            {
                new Step
                {
                    Name = "Provisioned",
                    Status = "success"
                },
            }
        },new Status
        {
            CustomerName = "Tailwind Traders",
            Steps = new[]
            {
                new Step
                {
                    Name = "Provisioned",
                    Status = "success"
                },
            }
        },new Status
        {
            CustomerName = "Tailspin Toys",
            Steps = new[]
            {
                new Step
                {
                    Name = "Provisioned",
                    Status = "success"
                },
            }
        },
    };
}