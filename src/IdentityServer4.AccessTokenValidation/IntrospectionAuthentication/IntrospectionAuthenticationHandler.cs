﻿using IdentityModel.Client;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Http.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer4.AccessTokenValidation
{
    public class IntrospectionAuthenticationHandler : AuthenticationHandler<IntrospectionAuthenticationOptions>
    {
        private readonly IntrospectionClient _client;

        public IntrospectionAuthenticationHandler(IntrospectionClient client)
        {
            _client = client;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string token = null;

            // todo add hook to get token from elsewhere
            string authorization = Request.Headers["Authorization"];

            // If no authorization header found, nothing to process further
            if (string.IsNullOrEmpty(authorization))
            {
                return AuthenticateResult.Failed("No bearer token.");
            }

            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorization.Substring("Bearer ".Length).Trim();
            }

            // If no token found, no further work possible
            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.Failed("No bearer token.");
            }

            if (token.Contains('.') && Options.SkipTokensWithDots)
            {
                return AuthenticateResult.Failed("Token contains a dot. Skipping.");
            }

            var response = await _client.SendAsync(new IntrospectionRequest
            {
                Token = token
            });

            if (response.IsError)
            {
                return AuthenticateResult.Failed("Error returned from introspection: " + response.Error);
            }

            if (response.IsActive)
            {
                var claims = new List<Claim>(response.Claims
                    .Where(c => c.Item1 != "active")
                    .Select(c => new Claim(c.Item1, c.Item2)));

                if (Options.PreserveAccessToken)
                {
                    claims.Add(new Claim("token", token));
                }

                var id = new ClaimsIdentity(claims, Options.AuthenticationScheme);
                var principal = new ClaimsPrincipal(id);

                var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), Options.AuthenticationScheme);
                return AuthenticateResult.Success(ticket);
            }

            return AuthenticateResult.Failed("invalid token.");
        }
    }
}