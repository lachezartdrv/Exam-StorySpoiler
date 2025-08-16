using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using StorySpoiler.Models;

namespace StorySpoiler
{
    [TestFixture]
    public class StorySpoilerTests
    {
        private RestClient client;
        private static string lastCreatedStoryId;

        private const string baseUrl = "https://d3s5nxhwblsjbi.cloudfront.net/";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJlMTgzZDZjZS1kNWM0LTRjNDgtYWRlMi1jNjRkMjdiYWI5NjQiLCJpYXQiOiIwOC8xNi8yMDI1IDA2OjI2OjM1IiwiVXNlcklkIjoiMzUyZjYxM2MtMmYyOC00MDU3LThlMDgtMDhkZGRiMWExM2YzIiwiRW1haWwiOiJsYWNoMTVAZXhhbXBsZS5jb20iLCJVc2VyTmFtZSI6ImxhY2gxNSIsImV4cCI6MTc1NTM0NzE5NSwiaXNzIjoiU3RvcnlTcG9pbF9BcHBfU29mdFVuaSIsImF1ZCI6IlN0b3J5U3BvaWxfV2ViQVBJX1NvZnRVbmkifQ.BJUl4fY36MLr5Hs_5gr8wTOfFhwaXys6FGmb32os_qU";

        private const string LoginUsername = "lach15";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginUsername, LoginPassword);
            }

            var options = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var tempClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { username, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token form the response");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }


        [Test, Order(1)]
        public void CreateNewStory_WithRequiredFields_ShouldReturnCreated()
        {
            var storyRequest = new StoryDTO
            {
                Title = "New",
                Description = "New description",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(storyRequest);
            var response = client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            lastCreatedStoryId = createResponse?.StoryId;

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Expected status code 201 Created.");
            Assert.That(createResponse?.Msg, Is.EqualTo("Successfully created!"));
        }

        [Test, Order(2)]
        public void EditExistingSpoiler_ShouldReturnSuccess()
        {
            var editSpoiler = new StoryDTO
            {
                Title = "edited title",
                Description = " edited description",
                Url = ""
            };

            var request = new RestRequest($"/api/Story/Edit/{lastCreatedStoryId}", Method.Put);
            request.AddJsonBody(editSpoiler);
            var response = client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Does.Contain("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllSpoilers_ShouldReturnListOfStorySpoilers()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseItems, Is.Not.Null);
            Assert.That(responseItems, Is.Not.Empty);

        }

        [Test, Order(4)]
        public void DeleteStorySpoiler_ShouldReturnSuccess()
        {
            TestContext.WriteLine($"Deleting StoryId: {lastCreatedStoryId}");

            var request = new RestRequest($"/api/Story/Delete/{lastCreatedStoryId}", Method.Delete);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully"));
        }

        [Test,Order(5)]
        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var editSpoiler = new StoryDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(editSpoiler);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test,Order(6)]
        public void EditNonExistingStorySpoiler_ShouldReturnNotFound()
        {
            string nonExistingStoryId = "123";
            var editRequest = new StoryDTO
            {
                Title = "edited non-existing",
                Description = "edited non existing",
                Url = ""
            };
            var request = new RestRequest($"/api/Story/Edit/{lastCreatedStoryId}", Method.Put);
            request.AddJsonBody(editRequest);
            request.AddQueryParameter("storyId", nonExistingStoryId);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content, Does.Contain("No spoilers..."));
        }

        [Test,Order(7)]
        public void DeleteNonExistingStorySpoiler_ShouldReturnBadRequest()
        {
            string nonExistingStoryId = "123";
            var request = new RestRequest($"/api/Story/Delete/{lastCreatedStoryId}", Method.Delete);
            request.AddQueryParameter("storyId", nonExistingStoryId);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("Unable to delete this story spoiler!"));
        }


        [OneTimeTearDown]
        public void Teardown()
        {
            this.client?.Dispose();
        }
    }

}