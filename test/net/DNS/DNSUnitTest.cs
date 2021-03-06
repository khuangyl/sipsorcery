﻿//-----------------------------------------------------------------------------
// Filename: DNSUnitTest.cs
//
// Description: Unit tests for the DNS lookup classes used in the SIPSorcery library.
//
// Author(s):
// Aaron Clauson
// 
// History:
// 14 Oct 2019	Aaron Clauson	Created (aaron@sipsorcery.com), SIP Sorcery PTY LTD, Dublin, Ireland (www.sipsorcery.com).
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System.Linq;
using Heijden.DNS;
using Microsoft.Extensions.Logging;
using SIPSorcery.SIP;
using SIPSorcery.Sys;
using Xunit;

namespace SIPSorcery.Net.UnitTests
{
    [Trait("Category", "dns")]
    public class DNSUnitTest
    {
        private Microsoft.Extensions.Logging.ILogger logger = null;

        public DNSUnitTest(Xunit.Abstractions.ITestOutputHelper output)
        {
            logger = SIPSorcery.UnitTests.TestLogHelper.InitTestLogger(output);
        }

        /// <summary>
        /// Test DNS resolution
        /// also test IPSocket.Parse
        /// </summary>
        [Fact(Skip = "DNS Queries for QType.ANY are not supported widely in the wild.")]
        public void LookupAnyRecordTest()
        {
            logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

            DNSResponse result = DNSManager.Lookup("dns.google", QType.ANY, 100, null, false, false);

            Assert.NotNull(result);

            Assert.NotEmpty(result.RecordsA);
            var ipv4Addresses = from a in result.RecordsA select a.Address;
            Assert.NotEmpty(ipv4Addresses);
            Assert.Contains(IPSocket.ParseSocketString("8.8.8.8").Address, ipv4Addresses);
            Assert.Contains(IPSocket.ParseSocketString("8.8.4.4").Address, ipv4Addresses);

            Assert.NotEmpty(result.RecordsAAAA);
            var ipv6Addresses = from a in result.RecordsAAAA select a.Address;
            Assert.NotEmpty(ipv6Addresses);
            Assert.Contains(IPSocket.ParseSocketString("2001:4860:4860::8888").Address, ipv6Addresses);
            Assert.Contains(IPSocket.ParseSocketString("2001:4860:4860::8844").Address, ipv6Addresses);
        }

        /// <summary>
        /// Test async DNS resolution
        /// 1. queue dns lookup for async resolution
        /// 2. check lookup/resolution cache for result
        /// (also test IPSocket.Parse)
        /// </summary>
        [Fact(Skip = "DNS Queries for QType.ANY are not supported widely in the wild.")]
        public void LookupAnyRecordAsyncCacheTest()
        {
            logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

            //1.queue dns lookup for async resolution
            DNSResponse result = DNSManager.Lookup("dns.google", QType.ANY, 1, null, false, true);
            Assert.Null(result);

            System.Threading.Thread.Sleep(500);

            //2.check lookup / resolution cache for result
            result = DNSManager.Lookup("dns.google", QType.ANY, 150, null, true, false);

            Assert.NotNull(result);

            Assert.NotEmpty(result.RecordsA);
            var ipv4Addresses = from a in result.RecordsA select a.Address;
            Assert.NotEmpty(ipv4Addresses);
            Assert.Contains(IPSocket.ParseSocketString("8.8.8.8").Address, ipv4Addresses);
            Assert.Contains(IPSocket.ParseSocketString("8.8.4.4").Address, ipv4Addresses);

            Assert.NotEmpty(result.RecordsAAAA);
            var ipv6Addresses = from a in result.RecordsAAAA select a.Address;
            Assert.NotEmpty(ipv6Addresses);
            Assert.Contains(IPSocket.ParseSocketString("2001:4860:4860::8888").Address, ipv6Addresses);
            Assert.Contains(IPSocket.ParseSocketString("2001:4860:4860::8844").Address, ipv6Addresses);
        }

        /// <summary>
        /// Test that a known A record is resolved.
        /// </summary>
        [Fact]
        public void LookupARecordMethod()
        {
            logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

            DNSResponse result = DNSManager.Lookup("www.sipsorcery.com", QType.A, 10, null, false, false);

            Assert.NotEmpty(result.RecordsA);
            logger.LogDebug($"Lookup result {result.RecordsA[0].Address}.");

            Assert.Equal("67.222.131.148", result.RecordsA[0].Address.ToString());

            result = DNSManager.Lookup("67.222.131.148", QType.A, 10, null, false, false);
            logger.LogDebug($"Lookup result {result.RecordsA[0].Address}.");
            Assert.Equal("67.222.131.148", result.RecordsA[0].Address.ToString());
        }

        /// <summary>
        /// Test that a known AAAA record is resolved.
        /// </summary>
        [Fact]
        public void LookupAAAARecordMethod()
        {
            logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

            DNSResponse result = DNSManager.Lookup("www.google.com", QType.AAAA, 10, null, false, false);

            foreach (var aaaaResult in result.RecordsAAAA)
            {
                logger.LogDebug($"AAAA Lookup result {aaaaResult.ToString()}.");
            }

            // Three's no guarantee that a particular DNS server will return AAAA records. The AppVeyor
            // macos vm is hooked up to a DNS that does not return AAAA records.
            // worker - 628 - 002:sipsorcery - jyl3x appveyor$ dig AAAA www.gooogle.com
            //
            // ; <<>> DiG 9.10.6 <<>> AAAA www.gooogle.com
            // ; ; global options: +cmd
            // ; ; Got answer:
            // ; ; ->> HEADER << -opcode: QUERY, status: NOERROR, id: 8102
            // ; ; flags: qr rd ra; QUERY: 1, ANSWER: 0, AUTHORITY: 0, ADDITIONAL: 0
            //
            // ; ; QUESTION SECTION:
            // ; www.gooogle.com.IN AAAA
            //
            // ; ; Query time: 24 msec
            // ; ; SERVER: 10.211.55.1#53(10.211.55.1)
            // ; ; WHEN: Wed Jun 10 05:56:30 CDT 2020
            // ; ; MSG SIZE  rcvd: 33

            //Assert.NotEmpty(result.RecordsAAAA);
            //Assert.True(result.RecordsAAAA[0].Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
        }

        [Fact]
        public void LookupSrvRecordMethod()
        {
            logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

            DNSResponse result = DNSManager.Lookup(SIPDNSConstants.SRV_SIP_UDP_QUERY_PREFIX + "sipsorcery.com", QType.SRV, 10, null, false, false);

            foreach (var srvResult in result.RecordSRV)
            {
                logger.LogDebug($"SRV Lookup result {srvResult.ToString()}.");
            }

            Assert.Single(result.RecordSRV);
            Assert.Equal("sip.sipsorcery.com.", result.RecordSRV.First().TARGET);
        }

        /// <summary>
        /// Test that a non qualified hostname can be looked up.
        /// </summary>
        [Fact]
        public void LookupCurrentHostNameMethod()
        {
            logger.LogDebug("--> " + System.Reflection.MethodBase.GetCurrentMethod().Name);
            logger.BeginScope(System.Reflection.MethodBase.GetCurrentMethod().Name);

            string localHostname = System.Net.Dns.GetHostName();

            logger.LogDebug($"Current host name {localHostname}");

            if (localHostname.EndsWith(STUNDns.MDNS_TLD))
            {
                // TODO: Look into why DNS calls on macos cannot resolve domains ending in ".local"
                // RFC6762 domains.
                logger.LogWarning("Skipping unit test LookupCurrentHostNameMethod due to RFC6762 domain.");
            }
            else
            {
                var addressList = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;

                addressList.ToList().ForEach(x => logger.LogDebug(x.ToString()));

                Assert.True(addressList.Count() > 0, "No address results were returned for a local hostname lookup.");
            }
        }
    }
}