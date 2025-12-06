using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Prompt;
using svc_ai_vision_adapter.Infrastructure.Options;
using svc_ai_vision_adapter.Application.Contracts;
using Moq.Protected;

namespace svc_vision_adapter_tests.tests.Infrastructure.Adapters.GeminiAdapterTest
{
    [TestClass]
    public class GeminiMachineAnalyzerTests
    {
        private Mock<IPromptLoader>? _promptMock;
        private GeminiOptions? _opt;

        [TestInitialize]
        public void Init()
        {
            //using strict to ensure only calls in setup method (BuildPrompt) returns valid.
            //exceptions will be thrown if it is not defined in setup
            _promptMock = new Mock<IPromptLoader>(MockBehavior.Strict);

            _opt = new GeminiOptions
            {
                ApiKey = "TESTKEY",
                Model = "gemini-test-model",
                SchemaPath = "schema.json"
            };

            // Simuler schema-fil til testen
            File.WriteAllText("schema.json", "{\"type\":\"object\"}");
        }

        private HttpClient CreateHttpClient(HttpResponseMessage response)
        {
            //as httpClient is a wrapper, we need to mock the httpMessageHandler
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(response);//returns fake response each time 

            //httpClient now uses the mocked handler
            return new HttpClient(handlerMock.Object);
        }

        [TestMethod]
        public async Task AnalyzeAsync_SendsCorrectHttpRequest()
        {
            // ARRANGE
            _promptMock!.Setup(x => x.BuildPrompt(It.IsAny<MachineAggregateDto>()))
                       .Returns("PROMPT_TEXT");

            var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "candidates": [{
                    "content": {
                      "parts": [ { "text": "{\"brand\":\"Hitachi\"}" } ]
                    }
                  }]
                }
                """, Encoding.UTF8, "application/json")
            };

            var http = CreateHttpClient(httpResponse);

            var analyzer = new GeminiMachineAnalyzer(
                http,
                Options.Create(_opt!),
                _promptMock.Object
            );

            var dto = new MachineAggregateDto { Brand = "Hitachi", MachineType = "Excavator", Model = "ZX35U" };

            // ACT
            await analyzer.AnalyzeAsync(dto, CancellationToken.None);

            // ASSERT
            _promptMock.Verify(x => x.BuildPrompt(It.IsAny<MachineAggregateDto>()),
                Times.Once);
            _promptMock.Verify(x => x.BuildPrompt(It.Is<MachineAggregateDto>(m =>
                            m.Brand == "Hitachi" &&
                            m.MachineType == "Excavator" &&
                            m.Model == "ZX35U")),
                        Times.Once);
        }

        [TestMethod]
        public async Task AnalyzeAsync_ParsesGeminiResponseCorrectly()
        {
            // ARRANGE
            _promptMock!.Setup(x => x.BuildPrompt(It.IsAny<MachineAggregateDto>()))
                       .Returns("PROMPT");

            var responseJson = """
            {
              "candidates": [{
                "content": {
                  "parts": [ { "text": "{\"brand\":\"Hitachi\",\"machineType\":\"Mini\",\"model\":\"ZX35\"}" } ]
                }
              }]
            }
            """;

            var http = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

            var analyzer = new GeminiMachineAnalyzer(
                http,
                Options.Create(_opt!),
                _promptMock.Object
            );

            var dto = new MachineAggregateDto { Brand = "Hitachi", MachineType = "Mini", Model = "ZX35" };

            // ACT
            var result = await analyzer.AnalyzeAsync(dto, CancellationToken.None);

            // ASSERT
            Assert.AreEqual("Hitachi", result.Brand);
            Assert.AreEqual("Mini", result.MachineType);
            Assert.AreEqual("ZX35", result.Model);
        }

        [TestMethod]
        public async Task AnalyzeAsync_ThrowsIfResponseIsEmpty()
        {
            // ARRANGE
            _promptMock!.Setup(x => x.BuildPrompt(It.IsAny<MachineAggregateDto>()))
                       .Returns("PROMPT");

            var http = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("", Encoding.UTF8, "application/json")
            });

            var analyzer = new GeminiMachineAnalyzer(
                http,
                Options.Create(_opt!),
                _promptMock.Object
            );

            var dto = new MachineAggregateDto();

            // ACT + ASSERT
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await analyzer.AnalyzeAsync(dto, CancellationToken.None));
        }

        [TestMethod]
        public async Task AnalyzeAsync_ThrowsIfTextPartMissing()
        {
            // ARRANGE
            _promptMock!.Setup(x => x.BuildPrompt(It.IsAny<MachineAggregateDto>()))
                       .Returns("PROMPT");

            var invalidJson = """
            {
              "candidates": [{
                "content": {
                  "parts": [ { "notText": "missing" } ]
                }
              }]
            }
            """;

            var http = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(invalidJson, Encoding.UTF8, "application/json")
            });

            var analyzer = new GeminiMachineAnalyzer(
                http,
                Options.Create(_opt!),
                _promptMock.Object
            );

            var dto = new MachineAggregateDto { Brand = "Brand", MachineType = "MachineType", Model = "Model" };

            // ACT + ASSERT
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await analyzer.AnalyzeAsync(dto, CancellationToken.None));
        }
        [TestMethod]
        public async Task AnalyzeAsync_LoadsSchemaFileSuccessfully()
        {
            // ARRANGE
            var tempPath = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempPath, "{ \"type\": \"object\" }");

            var options = new GeminiOptions
            {
                ApiKey = "dummy",
                Model = "test-model",
                SchemaPath = tempPath
            };

            _promptMock!.Setup(x => x.BuildPrompt(It.IsAny<MachineAggregateDto>()))
                       .Returns("PROMPT");

            var http = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "candidates": [{
                    "content": {
                      "parts": [ { "text": "{\"brand\":\"Hitachi\"}" } ]
                    }
                  }]
                }
                """, Encoding.UTF8, "application/json")
            });

            var analyzer = new GeminiMachineAnalyzer(
                http,
                Options.Create(options),
                _promptMock.Object
            );

            var dto = new MachineAggregateDto();

            // ACT — Should not throw
            var result = await analyzer.AnalyzeAsync(dto, CancellationToken.None);

            // ASSERT
            Assert.AreEqual("Hitachi", result.Brand);
        }
        [TestMethod]
        public async Task AnalyzeAsync_ThrowsIfSchemaFileMissing()
        {
            // ARRANGE
            var options = new GeminiOptions
            {
                ApiKey = "dummy",
                Model = "test-model",
                SchemaPath = "THIS_FILE_DOES_NOT_EXIST.json"
            };

            _promptMock!.Setup(x => x.BuildPrompt(It.IsAny<MachineAggregateDto>()))
                       .Returns("PROMPT");

            var http = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK));

            var analyzer = new GeminiMachineAnalyzer(
                http,
                Options.Create(options),
                _promptMock.Object
            );

            var dto = new MachineAggregateDto();

            // ACT + ASSERT
            await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () =>
                await analyzer.AnalyzeAsync(dto, CancellationToken.None));
        }


        [TestMethod]
        public async Task AnalyzeAsync_MapsRefusalCorrectly()
        {
            // ARRANGE
            _promptMock!.Setup(x => x.BuildPrompt(It.IsAny<MachineAggregateDto>()))
                       .Returns("PROMPT");

            var http = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "candidates": [{
                    "content": {
                      "parts": [
                        { "text": "{\"status\":\"refusal\",\"reason\":\"not enough info\"}" }
                      ]
                    }
                  }]
                }
                """, Encoding.UTF8, "application/json")
            });

            var analyzer = new GeminiMachineAnalyzer(
                http,
                Options.Create(_opt!),
                _promptMock.Object
            );

            // ACT
            var result = await analyzer.AnalyzeAsync(
                new MachineAggregateDto(), CancellationToken.None);

            // ASSERT
            Assert.IsNull(result.Brand);
            Assert.IsNull(result.MachineType);
            Assert.IsNull(result.Model);
            Assert.AreEqual(0, result.Confidence);
            Assert.IsFalse(result.IsConfident);
            Assert.AreEqual("not enough info", result.TypeSource);
        }
        [TestMethod]
        public async Task AnalyzeAsync_ThrowsIfCandidatesMissing()
        {
            // ARRANGE
            _promptMock!.Setup(x => x.BuildPrompt(It.IsAny<MachineAggregateDto>()))
                       .Returns("PROMPT");

            //empty json output from Gemini
            var http = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });

            var analyzer = new GeminiMachineAnalyzer(
                http,
                Options.Create(_opt!),
                _promptMock.Object
            );

            // ACT + ASSERT
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(async () =>
                await analyzer.AnalyzeAsync(new MachineAggregateDto(), CancellationToken.None));
        }
        [TestMethod]
        public async Task AnalyzeAsync_ThrowsIfPartsArrayEmpty()
        {
            // ARRANGE
            _promptMock!.Setup(x => x.BuildPrompt(It.IsAny<MachineAggregateDto>()))
                       .Returns("PROMPT");

            var http = CreateHttpClient(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                {
                  "candidates": [{
                    "content": { "parts": [] }
                  }]
                }
                """, Encoding.UTF8, "application/json")
            });

            var analyzer = new GeminiMachineAnalyzer(
                http,
                Options.Create(_opt!),
                _promptMock.Object
            );

            // ACT + ASSERT
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await analyzer.AnalyzeAsync(new MachineAggregateDto(), CancellationToken.None));
        }

    }
}
