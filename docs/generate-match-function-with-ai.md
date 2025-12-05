# Using AI to Build a Matchmaking Extend App and Generate `MakeMatches` Functions

## Introduction

Modern multiplayer games rely on matchmaking to create balanced, fair, and fun experiences.  
The AccelByte platform supports custom rules through a **Matchmaking Extend App**, where developers can override gRPC functions such as:

- `GetStatCodes`  
- `ValidateTicket`  
- `EnrichTicket`  
- `MakeMatches`  
- `BackfillMatches`  

Writing these functions by hand can be tedious. But with AI, you can speed up the process: AI can parse your **proto definitions**, read **rules.json**, and generate C# implementations that comply with the AccelByte contracts.

---

## Matchmaking Flow

From the provided proto and documentation:

1. **Client → Server**  
   - Send one `MakeMatchesRequest.parameters` (scope, rules.json, tickId)  
   - Send zero or more `MakeMatchesRequest.ticket` messages  
   - Close the stream  

2. **Server → Client**  
   - Emit zero or more `MatchResponse` objects  
   - Close the stream  

**Hard constraints**:  
- All tickets on a stream must belong to the same `match_pool`.  
- Discard tickets with 0 players or players exceeding `PlayersPerTeam` when Alliance is active.  
- Never split a ticket across teams.  

The **rules.json** drives behavior, typically containing:  
- **AllianceRule** – number of teams and players per team.  
- **MatchingRule** – attribute constraints.  
- **RegionLatencyMaxMs** – acceptable latency filters.  

---

## AI Workflow: Step by Step

Here's a repeatable workflow to use AI for building a matchmaking extend app.

### Step 1. Provide the Proto

Upload or paste your `matchfunction.proto` into your AI assistant.  
This gives AI the structure of messages (`Ticket`, `Match`, `MakeMatchesRequest`, etc.) and RPC signatures.

### Step 2. Add Context with Rules JSON

Give AI a sample `rules.json`:

```json
{
  "AllianceRule": {
    "teams": 2,
    "playersPerTeam": 3
  }
}
```

This tells AI what constraints the implementation should enforce.

### Step 3. Prompt for Implementation

Ask AI:

> "Generate a C# `MakeMatches` implementation that enforces the Alliance rule: X teams × Y players per team."

The AI will produce code that:  
- Parses `rules.json`  
- Collects tickets from the stream  
- Groups them into complete matches  
- Sends `MatchResponse` objects  

### Step 4. Insert Into Example Repo

Clone the [AccelByte reference repo](https://github.com/AccelByte/matchmaking-function-grpc-plugin-server-csharp):

```bash
git clone https://github.com/AccelByte/matchmaking-function-grpc-plugin-server-csharp
cd matchmaking-function-grpc-plugin-server-csharp
```

Replace the `MakeMatches` placeholder with your AI-generated function.  
Rebuild and run the server:

```bash
dotnet restore
dotnet run --project src/AccelByte.PluginArch.Demo.Server/AccelByte.PluginArch.Demo.Server.csproj
```

### Step 5. Iterate with AI

- To add **region preferences**: prompt AI to "extend MakeMatches with lowest-latency region selection."  
- To add **MMR balancing**: prompt AI to "distribute tickets evenly across teams by average MMR."  
- To add **backfill support**: prompt AI to "generate BackfillMatches handler that accepts a PartialMatch."  

AI will output updated code that you can drop in.

---

## Example: AI-Generated `MakeMatches` (Simplified)

```csharp
public override async Task MakeMatches(
    IAsyncStreamReader<MakeMatchesRequest> requestStream, 
    IServerStreamWriter<MatchResponse> responseStream, 
    ServerCallContext context)
{
    // Receive parameters
    await requestStream.MoveNext();
    var first = requestStream.Current;
    var parameters = first.Parameters;
    var allianceRule = ParseAlliance(parameters.Rules.Json);

    var candidates = new List<Ticket>();
    while (await requestStream.MoveNext())
    {
        var request = requestStream.Current;
        var ticket = request.Ticket;
        if (ticket != null)
        {
            if (ticket.Players.Count == 0 || 
                ticket.Players.Count > allianceRule.PlayersPerTeam)
            {
                continue;
            }
            candidates.Add(ticket);
        }
    }

    var teams = GroupIntoAlliance(candidates, allianceRule);
    if (teams == null) return;

    var match = BuildMatch(parameters, teams);
    await responseStream.WriteAsync(new MatchResponse { Match = match });
}
```

This version:  
- Validates tickets  
- Packs them into complete matches  
- Emits results following the proto contract  

---

## Why Use AI?

- **Speed**: Generate a working server implementation from proto files in minutes.  
- **Correctness**: AI respects the streaming order and contract.  
- **Flexibility**: Quickly adapt to new rules (latency, MMR, cross-region).  
- **Iteration**: Each change is just a new prompt away.  

---

## Conclusion

By combining **AccelByte's extensible matchmaking system** with **AI-assisted coding**, you can:  

- Generate working `MakeMatches` functions automatically  
- Encode Alliance rules and validation logic with minimal effort  
- Iterate rapidly on balancing strategies  

👉 Get started with the [AccelByte reference repo](https://github.com/AccelByte/matchmaking-function-grpc-plugin-server-csharp), then use AI to tailor the matchmaking logic for your game.  

---

