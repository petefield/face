using System.Net.Http.Json;
using System.Text.Json;
using Cli;

var baseAddress = ResolveBaseAddress(args);

using var http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromMinutes(2) };
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

Console.WriteLine($"Connected to Blinky at {baseAddress}");
Console.WriteLine("Type a message and press Enter. Commands: /reset, /exit");
Console.WriteLine();

while (!cts.IsCancellationRequested)
{
    Write("you> ", ConsoleColor.Cyan);
    var input = Console.ReadLine();

    if (input is null)
    {
        break;
    }

    input = input.Trim();
    if (input.Length == 0)
    {
        continue;
    }

    if (input is "/exit" or "/quit")
    {
        break;
    }

    if (input is "/reset")
    {
        await ResetAsync();
        continue;
    }

    await SendAsync(input);
}

Console.WriteLine("Bye.");
return 0;

async Task SendAsync(string prompt)
{
    try
    {
        using var response = await http.PostAsJsonAsync("/prompt", new PromptRequest(prompt), jsonOptions, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cts.Token);
            WriteLine($"error> {(int)response.StatusCode} {response.ReasonPhrase}: {body}", ConsoleColor.Red);
            return;
        }

        var result = await response.Content.ReadFromJsonAsync<PromptResponse>(jsonOptions, cts.Token);
        if (result is null)
        {
            WriteLine("error> empty response from brain.", ConsoleColor.Red);
            return;
        }

        Write("Blinky> ", ConsoleColor.Green);

        var responseText = result.Response;        

        Console.WriteLine(responseText);
        Console.WriteLine();
    }
    catch (OperationCanceledException)
    {
    }
    catch (Exception ex)
    {
        WriteLine($"error> {ex.Message}", ConsoleColor.Red);
    }
}

async Task ResetAsync()
{
    try
    {
        using var response = await http.PostAsync("/prompt/reset", content: null, cts.Token);
        if (response.IsSuccessStatusCode)
        {
            WriteLine("Conversation history cleared.", ConsoleColor.DarkGray);
        }
        else
        {
            WriteLine($"error> reset failed: {(int)response.StatusCode} {response.ReasonPhrase}", ConsoleColor.Red);
        }
    }
    catch (OperationCanceledException)
    {
    }
    catch (Exception ex)
    {
        WriteLine($"error> {ex.Message}", ConsoleColor.Red);
    }
}

static string ResolveBaseAddress(string[] args)
{
    if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
    {
        return args[0];
    }

    return Environment.GetEnvironmentVariable("BRAIN_URL") ?? "http://localhost:5233";
}

static void Write(string text, ConsoleColor color)
{
    var previous = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.Write(text);
    Console.ForegroundColor = previous;
}

static void WriteLine(string text, ConsoleColor color)
{
    Write(text + Environment.NewLine, color);
}
