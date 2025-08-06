using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;
using TelegramAutoPoster.Clients.Configuration;
using TelegramAutoPoster.Clients.Interfaces;

namespace TelegramAutoPoster.Clients
{
	public class OpenRouterClient : IAiChatClient
	{
		private readonly OpenRouterConfig _config;
		private int _requestsNumber = 0;

		public OpenRouterClient(IOptions<OpenRouterConfig> options)
		{
			_config = options.Value;
		}

		public async Task<string> GetAnswerAsync(string prompt)
		{
			//if (_requestsNumber == _config.RequestsLimitPerDay)
			//{
			//	throw new RateLimitExceededException("OpenRouter limit reached");
			//}


			var proxy = new WebProxy(_config.ProxyUrl)
			{
				UseDefaultCredentials = true
			};

			var handler = new HttpClientHandler
			{
				Proxy = proxy,
				UseProxy = true,
				UseDefaultCredentials = true
			};

			using var httpClient = new HttpClient(handler);

			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config.ApiKey}");

			var requestData = new
			{
				model = _config.Model,
				messages = new[] { new { role = "user", content = prompt } }
			};

			var requestJson = JsonSerializer.Serialize(requestData);
			var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

			var response = await httpClient.PostAsync(_config.ApiUrl, content);
			var responseJson = await response.Content.ReadAsStringAsync();

			using JsonDocument doc = JsonDocument.Parse(responseJson);

			var aiChatResponse = doc
				.RootElement
				.GetProperty("choices")[0]
				.GetProperty("message")
				.GetProperty("content")
				.GetString();

			_requestsNumber++;


			return aiChatResponse;
		}
	}
}
