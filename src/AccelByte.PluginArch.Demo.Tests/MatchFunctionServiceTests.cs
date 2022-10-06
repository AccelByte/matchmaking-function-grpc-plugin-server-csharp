// Copyright (c) 2022 AccelByte Inc. All Rights Reserved.
// This is licensed software from AccelByte Inc, for limitations
// and restrictions contact your company contract manager.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using AccelByte.MatchFunctionGrpc;
using AccelByte.PluginArch.Demo.Server.Services;

namespace AccelByte.PluginArch.Demo.Tests
{
    [TestFixture]
    public class MatchFunctionServiceTests
    {
        [Test]
        public async Task GetStatCodesTest()
        {
            var service = new MatchFunctionService();
            var response = await service.GetStatCodes(new GetStatCodesRequest(), new UnitTestCallContext());

            Assert.IsNotNull(response.Codes);
            Assert.AreEqual(3, response.Codes.Count);
            Assert.AreEqual("2", response.Codes[1]);
        }

        [Test]
        public async Task ValidateTicketTest()
        {
            var service = new MatchFunctionService();
            var response = await service.ValidateTicket(new ValidateTicketRequest(), new UnitTestCallContext());

            Assert.IsTrue(response.Valid);
        }

        [Test]
        public async Task MakeMatchesTest()
        {
            string testCaseId = "1234567890";

            var service = new MatchFunctionService();

            var context = new UnitTestCallContext();
            var requestStream = new TestAsyncStreamReader<MakeMatchesRequest>(context);
            var responseStream = new TestServerStreamWriter<MatchResponse>(context);

            using var call = service.MakeMatches(requestStream, responseStream, context);

            MakeMatchesRequest request = new MakeMatchesRequest();
            request.Ticket = new Ticket();
            request.Ticket.Players.Add(new Ticket.Types.PlayerData()
            {
                PlayerId = testCaseId
            });

            requestStream.AddMessage(request);

            var response = await responseStream.ReadNextAsync();
            Assert.IsNotNull(response);
            if (response != null)
            {
                Assert.AreEqual(1, response.Match.Teams.Count);
                Assert.AreEqual(1, response.Match.Teams[0].UserIds.Count);
                Assert.AreEqual(testCaseId, response.Match.Teams[0].UserIds[0]);
            }

            requestStream.Complete();
            await call;
            responseStream.Complete();

            Assert.Null(await responseStream.ReadNextAsync());
        }
    }
}
