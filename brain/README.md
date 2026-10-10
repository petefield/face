# Brain

A minimal .NET 10 Web API that forwards a prompt to the OpenAI chat completions endpoint and returns the response.

## Endpoint

`POST /prompt`

```json
{ "prompt": "What is the capital of France?" }
```

Response:

```json
{ "response": "The capital of France is Paris." }
```

Speech runs fire-and-forget: `/prompt` returns after the AI response and face
emotion update without waiting for playback. Speech failures are logged, and
speech is cancelled when the app shuts down, not when the request ends.

## Configuration

Settings live under the `OpenAI` section (`appsettings.json`, user-secrets, or environment variables):

| Key                           | Description                                               | Default              |
|-------------------------------|-------------------------------------------------------------|-----------------------|
| `OpenAI:ApiKey`                | Your OpenAI API key (required)                              | *(none)*              |
| `OpenAI:Model`                 | Model to use for completions                                 | `gpt-4o`              |
| `OpenAI:SystemPromptFile`      | Path to a text file containing the system prompt             | `system-prompt.txt`   |

### Setting the API key

Do **not** commit your API key to `appsettings.json`. Use one of:

```bash
# user-secrets (recommended for local dev)
dotnet user-secrets set OpenAI:ApiKey "sk-..."

# or an environment variable
export OpenAI__ApiKey="sk-..."
```

### System prompt

Edit `system-prompt.txt` (copied to the output directory on build) to change the system prompt sent with every request. Point `OpenAI:SystemPromptFile` at a different file to use an alternate prompt; relative paths are resolved against the app's content root.

## Run

```bash
dotnet run
```

## Weather skill

The automatically registered `get_weather` tool uses the
[Xweather Weather API](https://www.xweather.com/docs/weather-api) for current
observations and daily forecasts. An Xweather subscription with access to the
observations and forecasts endpoints is required.

Configure credentials using user-secrets from this directory (never commit them):

```bash
dotnet user-secrets set "Skills:xweather:ClientId" "<your-client-id>"
dotnet user-secrets set "Skills:xweather:ClientSecret" "<your-client-secret>"
dotnet user-secrets set "Skills:xweather:DefaultLocation" "london,gb"
```

Environment variables `Skills__xweather__ClientId`,
`Skills__xweather__ClientSecret`, and `Skills__xweather__DefaultLocation` are
also supported. The default location is optional; without it, the user must
specify a location. Locations can be a city with state/country, a postal code
supported by Xweather, or latitude,longitude.

Example prompts:

- "What is the weather in London now?"
- "What is the forecast here for the next three days?"
- "Will it rain in Paris tomorrow? Use Fahrenheit."

Tool arguments are `location`, `report` (`current` or `forecast`), `units`
(`metric` or `imperial`), and, for forecasts only, `days` (1-7, default 3) and
`startDate` (`yyyy-MM-dd`, default today in the requested location). Current
conditions default to Celsius and km/h. Results include observation/forecast
timestamps, temperatures, wind, humidity (current), and precipitation probability
(forecast); unavailable fields remain null, not zero. Historical weather is not
supported, and forecast range/availability depends on the subscription.

Missing configuration, invalid arguments, provider errors, empty results, and
timeouts return explicit failures to the model. Caller cancellation propagates.
HTTP request logging is disabled for the Xweather client because authentication
uses query parameters.

### Tests

From the repository root, run `dotnet test brain.tests/brain.tests.csproj`.
The weather tests use stub HTTP responses; no live API credentials are needed.
