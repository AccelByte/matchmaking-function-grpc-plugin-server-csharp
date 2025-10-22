// Copyright (c) 2022-2025 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

using Grpc.Core;
using Google.Protobuf.WellKnownTypes;

using AccelByte.MatchmakingV2.MatchFunction;
using AccelByte.PluginArch.Demo.Server.Model;

namespace AccelByte.PluginArch.Demo.Server.Services
{
    public class MatchFunctionService : MatchFunction.MatchFunctionBase
    {
        private readonly ILogger<MatchFunctionService> _Logger;

        public MatchFunctionService(ILogger<MatchFunctionService> logger)
        {
            _Logger = logger;
        }

        public override Task<StatCodesResponse> GetStatCodes(GetStatCodesRequest request, ServerCallContext context)
        {
            _Logger.LogInformation("Received GetStatCodes request.");
            try
            {
                string rulesJson = request.Rules?.Json ?? "{}";
                GameRules? rules = JsonSerializer.Deserialize<GameRules>(rulesJson);
                if (rules != null)
                {
                    try
                    {
                        rules.Validate();
                    }
                    catch (ValidationError ex)
                    {
                        throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
                    }
                }

                StatCodesResponse response = new StatCodesResponse();
                return Task.FromResult(response);
            }
            catch (ValidationError ex)
            {
                _Logger.LogError($"Validation error: {ex.Message}");
                throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
            }
            catch (Exception ex)
            {
                _Logger.LogError($"Cannot deserialize json rules: {ex.Message}");
                throw;
            }
        }

        public override Task<ValidateTicketResponse> ValidateTicket(ValidateTicketRequest request, ServerCallContext context)
        {
            _Logger.LogInformation("Received ValidateTicket request.");
            ValidateTicketResponse response = new ValidateTicketResponse()
            {
                ValidTicket = true
            };

            return Task.FromResult(response);
        }

        public override Task<EnrichTicketResponse> EnrichTicket(EnrichTicketRequest request, ServerCallContext context)
        {
            _Logger.LogInformation("Received EnrichTicket request.");

            Ticket ticket = request.Ticket;
            if (ticket.TicketAttributes.Fields.Count <= 0)
                ticket.TicketAttributes.Fields.Add("enrichedNumber", Value.ForNumber(20));

            EnrichTicketResponse response = new EnrichTicketResponse() { Ticket = ticket };
            return Task.FromResult(response);
        }

        public override async Task MakeMatches(IAsyncStreamReader<MakeMatchesRequest> requestStream, IServerStreamWriter<MatchResponse> responseStream, ServerCallContext context)
        {
            bool firstMessage = true;
            int matchesMade = 0;
            GameRules? rules = null;
            Queue<Ticket> unmatchedTickets = new Queue<Ticket>();

            while (await requestStream.MoveNext())
            {
                MakeMatchesRequest request = requestStream.Current;
                _Logger.LogInformation("Received MakeMatches request.");

                if (firstMessage)
                {
                    firstMessage = false;
                    if (request.Parameters == null)
                    {
                        string error = "First message must have the expected 'parameters' set.";
                        _Logger.LogError(error);
                        throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                    }

                    try
                    {
                        string rulesJson = request.Parameters.Rules?.Json ?? "{}";
                        rules = JsonSerializer.Deserialize<GameRules>(rulesJson);
                        rules?.Validate();
                    }
                    catch (ValidationError ex)
                    {
                        _Logger.LogError($"Validation error: {ex.Message}");
                        throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
                    }
                }
                else
                {
                    if (rules == null)
                    {
                        string error = "Rules not initialized.";
                        _Logger.LogError(error);
                        throw new RpcException(new Status(StatusCode.FailedPrecondition, error));
                    }

                    if (request.Ticket == null)
                    {
                        string error = "Message must have the expected 'ticket' set.";
                        _Logger.LogError(error);
                        throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                    }

                    unmatchedTickets.Enqueue(request.Ticket);
                    
                    var matches = BuildMatches(rules, unmatchedTickets);
                    foreach (var match in matches)
                    {
                        var response = new MatchResponse { Match = match };
                        _Logger.LogInformation("Match made and sent to client!");
                        await responseStream.WriteAsync(response);
                        matchesMade++;
                    }

                    if (matches.Count == 0)
                    {
                        _Logger.LogInformation($"Not enough tickets to create a match: {unmatchedTickets.Count}");
                    }
                }
            }

            _Logger.LogInformation($"Received MakeMatches (end): {matchesMade} match(es) made");
        }

        private List<Match> BuildMatches(GameRules rules, Queue<Ticket> unmatchedTickets)
        {
            var matches = new List<Match>();

            var (minPlayers, maxPlayers) = CalculatePlayerLimits(rules);

            while (unmatchedTickets.Count >= minPlayers)
            {
                int numPlayers = unmatchedTickets.Count >= maxPlayers ? maxPlayers : minPlayers;

                _Logger.LogInformation("Received enough tickets to create a match!");

                bool backfill = rules.AutoBackfill && numPlayers < maxPlayers;

                var matchedTickets = new List<Ticket>(numPlayers);
                for (int i = 0; i < numPlayers; i++)
                {
                    matchedTickets.Add(unmatchedTickets.Dequeue());
                }

                var playerIds = matchedTickets
                    .SelectMany(t => t.Players)
                    .Select(p => p.PlayerId)
                    .ToList();

                string teamId = Guid.NewGuid().ToString("N");
                var team = new Match.Types.Team
                {
                    TeamId = teamId
                };
                team.UserIds.AddRange(playerIds);

                var match = new Match
                {
                    Backfill = backfill
                };
                match.Tickets.AddRange(matchedTickets);
                match.Teams.Add(team);
                match.MatchAttributes.Fields["small-team-1"] = Value.ForString(teamId);
                
                // RegionPreference value is just an example.
                // The value(s) should be from the best region on the matchmaker.Ticket.Latencies
                match.RegionPreferences.Add("us-east-2");
                match.RegionPreferences.Add("us-west-2");

                matches.Add(match);
            }

            return matches;
        }

        private (int minPlayers, int maxPlayers) CalculatePlayerLimits(GameRules rules)
        {
            int minPlayers = 0;
            int maxPlayers = 0;

            if (rules.Alliance != null)
            {
                minPlayers = rules.Alliance.MinNumber * rules.Alliance.PlayerMinNumber;
                maxPlayers = rules.Alliance.MaxNumber * rules.Alliance.PlayerMaxNumber;
            }

            if (minPlayers == 0 && maxPlayers == 0)
            {
                minPlayers = 2;
                maxPlayers = 2;
            }

            if (rules.ShipCountMin != 0)
            {
                minPlayers *= rules.ShipCountMin;
            }

            if (rules.ShipCountMax != 0)
            {
                maxPlayers *= rules.ShipCountMax;
            }

            return (minPlayers, maxPlayers);
        }

        public override async Task BackfillMatches(IAsyncStreamReader<BackfillMakeMatchesRequest> requestStream, IServerStreamWriter<BackfillResponse> responseStream, ServerCallContext context)
        {
            bool firstMessage = true;
            int proposalsMade = 0;
            GameRules? rules = null;
            Queue<Ticket> unmatchedTickets = new Queue<Ticket>();
            Queue<BackfillTicket> unmatchedBackfillTickets = new Queue<BackfillTicket>();

            while (await requestStream.MoveNext())
            {
                BackfillMakeMatchesRequest request = requestStream.Current;
                _Logger.LogInformation("Received BackfillMatches request.");

                if (firstMessage)
                {
                    firstMessage = false;
                    if (request.Parameters == null)
                    {
                        string error = "First message must have the expected 'parameters' set.";
                        _Logger.LogError(error);
                        throw new RpcException(new Status(StatusCode.InvalidArgument, error));
                    }

                    try
                    {
                        string rulesJson = request.Parameters.Rules?.Json ?? "{}";
                        rules = JsonSerializer.Deserialize<GameRules>(rulesJson);
                        rules?.Validate();
                    }
                    catch (ValidationError ex)
                    {
                        _Logger.LogError($"Validation error: {ex.Message}");
                        throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
                    }
                }
                else
                {
                    if (rules == null)
                    {
                        string error = "Rules not initialized.";
                        _Logger.LogError(error);
                        throw new RpcException(new Status(StatusCode.FailedPrecondition, error));
                    }

                    if (request.Ticket != null)
                    {
                        unmatchedTickets.Enqueue(request.Ticket);
                    }
                    else if (request.BackfillTicket != null)
                    {
                        unmatchedBackfillTickets.Enqueue(request.BackfillTicket);
                    }

                    var proposals = BuildBackfillProposals(unmatchedTickets, unmatchedBackfillTickets);
                    foreach (var proposal in proposals)
                    {
                        var response = new BackfillResponse { BackfillProposal = proposal };
                        _Logger.LogInformation("Backfill proposal made and sent to client!");
                        await responseStream.WriteAsync(response);
                        proposalsMade++;
                    }

                    if (proposals.Count == 0)
                    {
                        _Logger.LogInformation($"Not enough tickets to create a backfill proposal: {unmatchedTickets.Count}, {unmatchedBackfillTickets.Count}");
                    }
                }
            }

            _Logger.LogInformation($"Received BackfillMatches (end): {proposalsMade} proposal(s) made");
        }

        private List<BackfillProposal> BuildBackfillProposals(
            Queue<Ticket> unmatchedTickets,
            Queue<BackfillTicket> unmatchedBackfillTickets)
        {
            var proposals = new List<BackfillProposal>();

            while (unmatchedTickets.Count > 0 && unmatchedBackfillTickets.Count > 0)
            {
                _Logger.LogInformation("Received enough tickets to backfill!");

                var backfillTicket = unmatchedBackfillTickets.Dequeue();
                var ticket = unmatchedTickets.Dequeue();

                var proposal = CreateBackfillProposal(backfillTicket, ticket);
                proposals.Add(proposal);
            }

            return proposals;
        }

        private BackfillProposal CreateBackfillProposal(BackfillTicket backfillTicket, Ticket newTicket)
        {
            string teamId = Guid.NewGuid().ToString("N");
            var newTeam = new BackfillProposal.Types.Team
            {
                TeamId = teamId
            };

            var proposal = new BackfillProposal
            {
                BackfillTicketId = backfillTicket.TicketId,
                CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
                ProposalId = "",
                MatchPool = backfillTicket.MatchPool,
                MatchSessionId = backfillTicket.MatchSessionId
            };

            proposal.AddedTickets.Add(newTicket);

            if (backfillTicket.PartialMatch?.Teams != null)
            {
                foreach (var existingTeam in backfillTicket.PartialMatch.Teams)
                {
                    var proposalTeam = new BackfillProposal.Types.Team
                    {
                        TeamId = existingTeam.TeamId
                    };
                    proposalTeam.UserIds.AddRange(existingTeam.UserIds);
                    proposalTeam.Parties.AddRange(existingTeam.Parties);
                    proposal.ProposedTeams.Add(proposalTeam);
                }
            }

            proposal.ProposedTeams.Add(newTeam);

            return proposal;
        }
    }
}
