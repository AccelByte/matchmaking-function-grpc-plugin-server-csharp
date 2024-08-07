﻿// Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
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

        private int _ShipCountMin = 2;

        private int _ShipCountMax = 2;

        private List<Ticket> _UnmatchedTickets = new List<Ticket>();

        private Match MakeMatchFromUnmatchedTickets()
        {
            List<Ticket.Types.PlayerData> players = new List<Ticket.Types.PlayerData>();
            for (int i = 0; i < _UnmatchedTickets.Count; i++)
                players.AddRange(_UnmatchedTickets[i].Players);

            List<string> playerIds = players.Select(p => p.PlayerId).ToList();

            Match match = new Match();

            // RegionPreference value is just an example. The value(s) should be from the best region on the matchmaker.Ticket.Latencies
            match.RegionPreferences.Add("us-east-2");
            match.RegionPreferences.Add("us-west-2");
            
            match.Tickets.AddRange(_UnmatchedTickets);

            Match.Types.Team team = new Match.Types.Team();
            team.UserIds.AddRange(playerIds);
            match.Teams.Add(team);

            return match;
        }

        private async Task CreateAndPushMatchResultAndClearUnmatchedTickets(IServerStreamWriter<MatchResponse> responseStream)
        {
            await responseStream.WriteAsync(new MatchResponse()
            {
                Match = MakeMatchFromUnmatchedTickets()
            });
            _UnmatchedTickets.Clear();
        }

        public MatchFunctionService(ILogger<MatchFunctionService> logger)
        {
            _Logger = logger;
        }

        public override Task<StatCodesResponse> GetStatCodes(GetStatCodesRequest request, ServerCallContext context)
        {
            _Logger.LogInformation("Received GetStatCodes request.");
            try
            {
                StatCodesResponse response = new StatCodesResponse();

                return Task.FromResult(response);
            }
            catch (Exception x)
            {
                _Logger.LogError("Cannot deserialize json rules. " + x.Message);
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
            while (await requestStream.MoveNext())
            {
                MakeMatchesRequest request = requestStream.Current;
                _Logger.LogInformation("Received make matches request.");
                if (request.Parameters != null)
                {
                    _Logger.LogInformation("Received parameters");
                    if (request.Parameters.Rules != null)
                    {
                        RuleObject? ruleObj = JsonSerializer.Deserialize<RuleObject>(request.Parameters.Rules.Json);
                        if (ruleObj == null)
                        {
                            _Logger.LogError("Invalid Rules JSON");
                            throw new Exception("Invalid Rules JSON");
                        }

                        if ((ruleObj.ShipCountMin != 0) && (ruleObj.ShipCountMax != 0)
                            && (ruleObj.ShipCountMin <= ruleObj.ShipCountMax))
                        {
                            _ShipCountMin = ruleObj.ShipCountMin;
                            _ShipCountMax = ruleObj.ShipCountMax;
                            _Logger.LogInformation(String.Format(
                                "Update shipCountMin = {0} and shipCountMax = {1}",
                                _ShipCountMin, _ShipCountMax
                            ));
                        }
                    }
                }

                if (request.Ticket != null)
                {
                    _Logger.LogInformation("Received ticket");
                    _UnmatchedTickets.Add(request.Ticket);
                    if (_UnmatchedTickets.Count == _ShipCountMax)
                    {
                        await CreateAndPushMatchResultAndClearUnmatchedTickets(responseStream);
                    }

                    _Logger.LogInformation("Unmatched tickets size : " + _UnmatchedTickets.Count.ToString());
                }
            }

            //complete
            _Logger.LogInformation("On completed. Unmatched tickets size: " + _UnmatchedTickets.Count.ToString());
            if (_UnmatchedTickets.Count >= _ShipCountMin)
            {
                await CreateAndPushMatchResultAndClearUnmatchedTickets(responseStream);
            }
        }

        public override async Task BackfillMatches(IAsyncStreamReader<BackfillMakeMatchesRequest> requestStream, IServerStreamWriter<BackfillResponse> responseStream, ServerCallContext context)
        {
            while (await requestStream.MoveNext())
            {
                await responseStream.WriteAsync(new BackfillResponse()
                {
                    BackfillProposal = new BackfillProposal()
                });

            }
        }
    }
}