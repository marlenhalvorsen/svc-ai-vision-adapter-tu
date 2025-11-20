using Microsoft.VisualStudio.TestTools.UnitTesting;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini;
using svc_ai_vision_adapter.Infrastructure.Adapters.GoogleGemini.Models;
using svc_ai_vision_adapter.Application.Contracts;
using System.Collections.Generic;

namespace svc_vision_adapter_tests.tests.Infrastructure.Adapters.GeminiAdapterTest
{
    [TestClass]
    public class GeminiToAggregateMapperTests
    {
        [TestMethod]
        public void Map_RefusalResponse_MapsToEmptyAggregate()
        {
            // ARRANGE
            var dto = new GeminiResponseDto
            {
                Status = "refusal",
                Reason = "Not enough information"
            };

            // ACT
            var result = GeminiToAggregateMapper.Map(dto);

            // ASSERT
            Assert.IsNull(result.Brand);
            Assert.IsNull(result.MachineType);
            Assert.IsNull(result.Model);
            Assert.AreEqual(0, result.Confidence);
            Assert.IsFalse(result.IsConfident);
            Assert.AreEqual("Not enough information", result.TypeSource);
        }

        [TestMethod]
        public void Map_SuccessResponse_MapsFieldsCorrectly()
        {
            // ARRANGE
            var dto = new GeminiResponseDto
            {
                Status = "success",
                Brand = "Hitachi",
                MachineType = "Excavator",
                Model = "ZX85USB-5",
                Weight = 8500,
                Year = "2018",
                Attachment = new List<string> { "Bucket", "QuickCoupler" },
                Confidence = 0.82,
                Source = "vision + llm"
            };

            // ACT
            var result = GeminiToAggregateMapper.Map(dto);

            // ASSERT
            Assert.AreEqual("Hitachi", result.Brand);
            Assert.AreEqual("Excavator", result.MachineType);
            Assert.AreEqual("ZX85USB-5", result.Model);
            Assert.AreEqual(8500, result.Weight);
            Assert.AreEqual("2018", result.Year);
            CollectionAssert.AreEqual(new List<string> { "Bucket", "QuickCoupler" }, result.Attachment);
            Assert.AreEqual(0.82, result.Confidence);
            Assert.IsTrue(result.IsConfident);
            Assert.AreEqual("vision + llm", result.TypeSource);
        }

        [TestMethod]
        public void Map_SetsIsConfident_False_WhenConfidenceLow()
        {
            // ARRANGE
            var dto = new GeminiResponseDto
            {
                Status = "success",
                Confidence = 0.40
            };

            // ACT
            var result = GeminiToAggregateMapper.Map(dto);

            // ASSERT
            Assert.IsFalse(result.IsConfident);
            Assert.AreEqual(0.40, result.Confidence);
        }

        [TestMethod]
        public void Map_SetsIsConfident_False_WhenConfidenceIsNull()
        {
            // ARRANGE
            var dto = new GeminiResponseDto
            {
                Status = "success",
                Confidence = null
            };

            // ACT
            var result = GeminiToAggregateMapper.Map(dto);

            // ASSERT
            Assert.IsFalse(result.IsConfident);
            Assert.AreEqual(0, result.Confidence);
        }

        [TestMethod]
        public void Map_SetsIsConfident_True_WhenConfidenceAboveThreshold()
        {
            // ARRANGE
            var dto = new GeminiResponseDto
            {
                Status = "success",
                Confidence = 0.90
            };

            // ACT
            var result = GeminiToAggregateMapper.Map(dto);

            // ASSERT
            Assert.IsTrue(result.IsConfident);
        }
    }
}
