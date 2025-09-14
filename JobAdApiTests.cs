using NUnit.Framework;
using RestSharp;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Serilog;
using Serilog.Extensions.Logging;

namespace AlzaPozice.Tests
{
    public class JobAdApiTests
    {
        private ILogger<JobAdApiTests> _logger;
        private IConfiguration _config;
        private string _logPath;

        [SetUp]
        public void Setup()
        {
            var configBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");
            _config = configBuilder.Build();
            _logPath = _config["Logging:LogPath"];

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(_logPath)
                .CreateLogger();

            var loggerFactory = new SerilogLoggerFactory(Log.Logger);
            _logger = loggerFactory.CreateLogger<JobAdApiTests>();
        }

        [Test]
        public void JobAd_ShouldContainAllRequiredInformation()
        {
            var client = new RestClient("https://webapi.alza.cz");
            var request = new RestRequest("/api/career/v2/positions/java-developer-", Method.Get);

            _logger.LogInformation("Sending request to job ad API...");
            var response = client.Execute(request);

            _logger.LogInformation("Checking if API response is successful...");
            NUnit.Framework.Assert.That(response.IsSuccessful, Is.True, "API response was not successful");
            _logger.LogInformation("API response received.");

            var json = JsonDocument.Parse(response.Content);

            // Popis pracovní pozice
            _logger.LogInformation("Kontrola popisu pracovní pozice...");
            if (json.RootElement.TryGetProperty("description", out var descriptionElement))
            {
                var description = descriptionElement.GetString();
                NUnit.Framework.Assert.That(string.IsNullOrWhiteSpace(description), Is.False, "Popis pracovní pozice není vyplněn");
                _logger.LogInformation("Popis pracovní pozice je vyplněn.");
            }
            else
            {
                _logger.LogError("Popis pracovní pozice není v odpovědi API");
                NUnit.Framework.Assert.Fail("Popis pracovní pozice není v odpovědi API");
            }

            // Vhodné pro studenty
            _logger.LogInformation("Kontrola vhodnosti pro studenty...");
            var isForStudents = json.RootElement.GetProperty("isForStudents").GetBoolean();
            NUnit.Framework.Assert.That(isForStudents, Is.True, "Pracovní pozice není vhodná pro studenty");
            _logger.LogInformation("Pracovní pozice je vhodná pro studenty.");

            // Místo výkonu práce
            _logger.LogInformation("Kontrola místa výkonu práce...");
            var location = json.RootElement.GetProperty("location");
            NUnit.Framework.Assert.That(location.GetProperty("name").GetString(), Is.EqualTo("Hall office park"), "Název místa výkonu práce nesouhlasí");
            NUnit.Framework.Assert.That(location.GetProperty("country").GetString(), Is.EqualTo("Česká republika"), "Stát nesouhlasí");
            NUnit.Framework.Assert.That(location.GetProperty("city").GetString(), Is.EqualTo("Praha"), "Město nesouhlasí");
            NUnit.Framework.Assert.That(location.GetProperty("street").GetString(), Is.EqualTo("U Pergamenky 2"), "Ulice a číslo nesouhlasí");
            NUnit.Framework.Assert.That(location.GetProperty("zipCode").GetString(), Is.EqualTo("17000"), "PSČ nesouhlasí");
            _logger.LogInformation("Místo výkonu práce je správně.");

            // Nadřízený
            _logger.LogInformation("Kontrola nadřízeného...");
            var executive = json.RootElement.GetProperty("executiveUser");
            NUnit.Framework.Assert.That(string.IsNullOrWhiteSpace(executive.GetProperty("name").GetString()), Is.False, "Jméno nadřízeného není vyplněno");
            NUnit.Framework.Assert.That(executive.GetProperty("name").GetString(), Is.EqualTo("Kozák Michal"), "Jméno nadřízeného nesouhlasí");
            NUnit.Framework.Assert.That(string.IsNullOrWhiteSpace(executive.GetProperty("photoUrl").GetString()), Is.False, "Fotografie nadřízeného není vyplněna");
            NUnit.Framework.Assert.That(string.IsNullOrWhiteSpace(executive.GetProperty("description").GetString()), Is.False, "Popis nadřízeného není vyplněn");
            _logger.LogInformation("Nadřízený je správně vyplněn.");
        }

        [Test]
        public void InvalidJobAd_ShouldReturnNotFound()
        {
            var client = new RestClient("https://webapi.alza.cz");
            var request = new RestRequest("/api/career/v2/positions/invalid-position", Method.Get);

            _logger.LogInformation("Sending request to invalid job ad API...");
            var response = client.Execute(request);

            NUnit.Framework.Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.NotFound), "Neplatná pozice nevrací 404");
            _logger.LogInformation("Neplatná pozice správně vrací 404.");
        }
    }
}