﻿// Copyright (c) 2012, Event Store LLP
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
// Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
// Neither the name of the Event Store LLP nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System.Linq;
using System.Security.Principal;
using EventStore.Core.Messages;
using NUnit.Framework;

namespace EventStore.Core.Tests.Authentication
{
    [TestFixture]
    public class when_handling_multiple_requests_with_reset_password_cache_in_between :
        with_internal_authentication_provider
    {
        private bool _unauthorized;
        private IPrincipal _authenticatedAs;
        private bool _error;

        protected override void Given()
        {
            base.Given();
            ExistingEvent("$user-user", "$user", null, "{LoginName:'user', Salt:'drowssap',Hash:'password'}");
        }

        [SetUp]
        public void SetUp()
        {
            SetUpProvider();

            _internalAuthenticationProvider.Authenticate(
                new TestAuthenticationRequest("user", "password", () => { }, p => { }, () => { }));
            _internalAuthenticationProvider.Handle(
                new InternalAuthenticationProviderMessages.ResetPasswordCache("user"));
            _consumer.HandledMessages.Clear();

            _internalAuthenticationProvider.Authenticate(
                new TestAuthenticationRequest(
                    "user", "password", () => _unauthorized = true, p => _authenticatedAs = p, () => _error = true));
        }

        [Test]
        public void authenticates_user()
        {
            Assert.IsFalse(_unauthorized);
            Assert.IsFalse(_error);
            Assert.NotNull(_authenticatedAs);
            Assert.IsTrue(_authenticatedAs.IsInRole("user"));
        }

        [Test]
        public void publishes_some_read_requests()
        {
            Assert.Greater(
                _consumer.HandledMessages.OfType<ClientMessage.ReadStreamEventsBackward>().Count()
                + _consumer.HandledMessages.OfType<ClientMessage.ReadStreamEventsForward>().Count(), 0);
        }
    }
}
