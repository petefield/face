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
