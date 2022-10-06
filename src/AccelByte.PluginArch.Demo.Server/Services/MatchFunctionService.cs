// Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using Grpc.Core;
using AccelByte.MatchFunctionGrpc;

namespace AccelByte.PluginArch.Demo.Server.Services
{
    public class MatchFunctionService : MatchFunction.MatchFunctionBase
    {
        public override Task<StatCodesResponse> GetStatCodes(GetStatCodesRequest request, ServerCallContext context)
        {
            StatCodesResponse response = new StatCodesResponse();
            response.Codes.Add(new string[] { "1", "2", "3" });

            return Task.FromResult(response);
        }

        public override Task<ValidateTicketResponse> ValidateTicket(ValidateTicketRequest request, ServerCallContext context)
        {
            ValidateTicketResponse response = new ValidateTicketResponse()
            {
                Valid = true
            };

            return Task.FromResult(response);
        }

        public override async Task MakeMatches(IAsyncStreamReader<MakeMatchesRequest> requestStream, IServerStreamWriter<MatchResponse> responseStream, ServerCallContext context)
        {
            /*while (await requestStream.MoveNext())
            {
                MakeMatchesRequest request = requestStream.Current;
                if (request.Ticket.Players.Count > 0)
                {
                    foreach (var player in request.Ticket.Players)
                    {
                        MatchResponse response = new MatchResponse();

                        Match.Types.Team team = new Match.Types.Team();
                        team.UserIds.Add(player.PlayerId);
            
                        response.Match = new Match();
                        response.Match.Teams.Add(team);

                        await responseStream.WriteAsync(response);
                    }
                }
            }*/

            await foreach (MakeMatchesRequest request in requestStream.ReadAllAsync())
            {
                if (request.Ticket.Players.Count > 0)
                {
                    var player = request.Ticket.Players[0];
                    MatchResponse response = new MatchResponse();

                    Match.Types.Team team = new Match.Types.Team();
                    team.UserIds.Add(player.PlayerId);

                    response.Match = new Match();
                    response.Match.Teams.Add(team);

                    await responseStream.WriteAsync(response);
                }
            }
        }
    }
}