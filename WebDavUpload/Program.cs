using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Cocona;

var app = CoconaApp.Create(); // is a shorthand for `CoconaApp.CreateBuilder().Build()`


app.AddCommand("upload", async (string url, string user, string password, string file) =>
{
    var client = new HttpClient();
    client.Timeout = TimeSpan.FromMinutes(120);
    var authToken = Encoding.UTF8.GetBytes($"{user}:{password}");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(authToken));
    await using var stream = File.OpenRead(file);
    var response = await client.PutAsync(url, new StreamContent(stream));
    if (!response.IsSuccessStatusCode)
    {
        throw new Exception($"Failed to upload file: {response.StatusCode}");
    }
});

app.AddCommand("mkdir", async ( string url, string user,string password) =>
{
    var client = new HttpClient();
    client.Timeout = TimeSpan.FromMinutes(120);
    var authToken = Encoding.UTF8.GetBytes($"{user}:{password}");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(authToken));
    var requestMessage = new HttpRequestMessage()
    {
        Method = new HttpMethod("MKCOL"),
        RequestUri = new Uri(url)
    };
    var response = await client.SendAsync(requestMessage);
    if (!response.IsSuccessStatusCode)
    {
        throw new Exception($"Failed to upload file: {response.StatusCode}");
    }
});


app.AddCommand("download", async (string url, string user, string password, string file) =>
{
    var client = new HttpClient();
    client.Timeout = TimeSpan.FromMinutes(120);
    var authToken = Encoding.UTF8.GetBytes($"{user}:{password}");
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
        Convert.ToBase64String(authToken));
    
    var response = await client.GetAsync(url);
    
    if (!response.IsSuccessStatusCode)
    {
        throw new Exception($"Failed to upload file: {response.StatusCode}");
    }

    await using var stream = File.OpenWrite(file);

    await response.Content.CopyToAsync(stream);

});

app.Run();
