using ChatApp.server.DTO;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Time.Testing;

namespace ChatApp.Server.Tests.Controllers
{
    public class AuthControllerRateLimitTests : IClassFixture<NormalRateLimitWebApplicationFactory>
    {
        private readonly NormalRateLimitWebApplicationFactory _factory;

        public AuthControllerRateLimitTests(NormalRateLimitWebApplicationFactory factory)
        {
            _factory = factory;
        }
        [Fact]
        public async Task Login_UnderRateLimit_ReturnsOk()
        {
            // arrange
            var factory = new NormalRateLimitWebApplicationFactory();
            var client = factory.CreateClient();
            var request = new LoginRequestDTO { Username = "testUser", Password = "password123" };
            HttpResponseMessage response;
            // act
            for (int i = 0; i < 3; i++)
            {
                response = await client.PostAsJsonAsync("/auth/login", request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            
            
        }
        [Fact]
        public async Task Login_OverRateLimit_ReturnsTooManyRequests()
        {
            // Arranage
            var factory = new NormalRateLimitWebApplicationFactory();
            var client = factory.CreateClient();
            var request = new LoginRequestDTO { Username = "testUser", Password = "password123" };
            HttpResponseMessage response;
            // Act 
            for (int i = 0; i < 4; i++)
            {
            response = await client.PostAsJsonAsync("/auth/login", request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                
            }
            response = await client.PostAsJsonAsync("/auth/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);


        }
        [Fact]
        public async Task Login_RateLimitReset_ReturnsOk()
        {
            // Arrange
            var fakeTime = new FakeTimeProvider();
            var factory = new NormalRateLimitWebApplicationFactory();
            var client = factory.CreateClient();
            var request = new LoginRequestDTO { Username = "testUser", Password = "password123" };
            HttpResponseMessage response;

            // Act
            for (int i = 0; i < 4; i++)
            {
                response = await client.PostAsJsonAsync("/auth/login", request);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            response = await client.PostAsJsonAsync("/auth/login", request);
            Assert.Equal (HttpStatusCode.TooManyRequests, response.StatusCode);

            await Task.Delay(TimeSpan.FromSeconds(1.1));
            response = await client.PostAsJsonAsync("/auth/login", request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
