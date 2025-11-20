using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Options;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Prompt;
using svc_ai_vision_adapter.Infrastructure.Options;
using svc_ai_vision_adapter.Application.Contracts;

namespace svc_vision_adapter_tests.tests.Infrastructure.Adapters.GeminiAdapterTest
{
    [TestClass]
    public class GeminiPromptLoaderTests
    {
        private string _tempFilePath = null!;

        [TestInitialize]
        public void Init()
        {
            // Opret midlertidig prompt-fil
            _tempFilePath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);
        }

        private GeminiPromptLoader CreateLoader(string template)
        {
            File.WriteAllText(_tempFilePath, template);

            var options = Options.Create(new GeminiOptions
            {
                PromptPath = _tempFilePath
            });

            return new GeminiPromptLoader(options);
        }

        [TestMethod]
        public void BuildPrompt_ReplacesPlaceholdersCorrectly()
        {
            // ARRANGE
            var template = "Brand: {{brand}}, MachineType: {{machineType}}, Model: {{model}}";
            var loader = CreateLoader(template);

            var dto = new MachineAggregateDto
            {
                Brand = "Hitachi",
                MachineType = "Excavator",
                Model = "ZX35U"
            };

            // ACT
            var result = loader.BuildPrompt(dto);

            // ASSERT
            Assert.AreEqual("Brand: Hitachi, MachineType: Excavator, Model: ZX35U", result);
        }


        [TestMethod]
        public void BuildPrompt_HandlesNullValues_AsUnknown()
        {
            // ARRANGE
            var template = "Brand: {{brand}}, MachineType: {{machineType}}, Model: {{model}}";
            var loader = CreateLoader(template);

            var dto = new MachineAggregateDto
            {
                Brand = null,
                MachineType = null,
                Model = null
            };

            // ACT
            var result = loader.BuildPrompt(dto);

            // ASSERT
            Assert.AreEqual("Brand: unknown, MachineType: unknown, Model: unknown", result);
        }


        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void Constructor_ThrowsIfPromptFileDoesNotExist()
        {
            // ARRANGE
            var options = Options.Create(new GeminiOptions
            {
                PromptPath = "NON_EXISTING_FILE.txt"
            });

            // ACT
            _ = new GeminiPromptLoader(options);

            // ASSERT happens via ExpectedException
        }

        [TestMethod]
        public void Constructor_LoadsTemplateCorrectly()
        {
            // ARRANGE
            File.WriteAllText(_tempFilePath, "TEMPLATE_TEST");

            var options = Options.Create(new GeminiOptions
            {
                PromptPath = _tempFilePath
            });

            // ACT
            var loader = new GeminiPromptLoader(options);
            var result = loader.BuildPrompt(new MachineAggregateDto());

            // ASSERT
            Assert.IsTrue(result.Contains("TEMPLATE_TEST"));
        }
    }
}
