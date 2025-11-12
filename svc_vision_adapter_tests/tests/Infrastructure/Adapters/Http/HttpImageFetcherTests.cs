using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using svc_ai_vision_adapter.Application.Contracts;
using svc_ai_vision_adapter.Infrastructure.Adapters.Http;
using svc_ai_vision_adapter.Infrastructure.Adapters.Http.Models;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace svc_vision_adapter_tests;

[TestClass]
public class HttpImageFetcherTests
{
    [TestMethod]
    public async Task FetchAsync_Returns_Bytes_When_Image_Is_Valid()
    {
        // ARRANGE
        var imageBytes = new byte[] { 1, 2, 3, 4 };
        var client = CreateHttpClientReturning(imageBytes);
        var factory = CreateFactory(client);

        var fetcher = new HttpImageFetcher(factory);

        var img = new ImageRefDto("https://example.com/test.jpg");

        // ACT
        var result = await fetcher.FetchAsync(img, CancellationToken.None);

        // ASSERT
        CollectionAssert.AreEqual(imageBytes, result.Bytes);
        Assert.AreEqual(img, result.Ref);
    }

    [TestMethod]
    public async Task FetchAsync_WhenImageIsTooLarge_ThrowsInvalidOperationException()
    {
        // ARRANGE
        var bigLength = 15 * 1024 * 1024; // 15 MB (over MaxBytes = 10MB)

        // Fake HTTP response with large content length
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new MemoryStream()) // content doesn't matter
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        response.Content.Headers.ContentLength = bigLength;

        // Mock HttpMessageHandler to return fake response. 
        //when using httpClient httpMessageHandler is to be mocked as this is the part
        //where the actual http-call takes place
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        var client = new HttpClient(handler.Object);
        var factory = Mock.Of<IHttpClientFactory>(f => f.CreateClient(It.IsAny<string>()) == client);

        var fetcher = new HttpImageFetcher(factory);

        var imageRef = new ImageRefDto("https://example.com/test.png");

        //ACT + ASSERT
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()
            => fetcher.FetchAsync(imageRef));
    }


    [TestMethod]
    public async Task FetchAsync_WhenContentTypeIsNotImage_ShouldThrow()
    {
        // ARRANGE

        // Fake HTTP response where the server returns content that is NOT an image
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            // The actual byte content is irrelevant here – fetcher should fail before reading it
            Content = new StreamContent(new MemoryStream(new byte[] { 1, 2, 3 }))
        };

        // Set the content-type to something invalid for image fetching
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/html");
        response.Content.Headers.ContentLength = 3; // small size is fine

        // Mock HttpMessageHandler because HttpClient itself cannot be directly mocked
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",                       // protected method we are overriding
                ItExpr.IsAny<HttpRequestMessage>(), // match any HTTP request
                ItExpr.IsAny<CancellationToken>())  // match any cancellation token
            .ReturnsAsync(response);               // return our fake HTTP response

        // Construct HttpClient instance using the mocked handler
        var client = new HttpClient(handler.Object);

        // Mock IHttpClientFactory to return our custom HttpClient
        var factory = Mock.Of<IHttpClientFactory>(f => f.CreateClient(
            It.IsAny<string>()) == client);

        // Create the fetcher under test
        var fetcher = new HttpImageFetcher(factory);

        // The input object representing the image reference
        var imageRef = new ImageRefDto("https://example.com/not-an-image.html");

        // ACT + ASSERT 
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => fetcher.FetchAsync(imageRef));
    }

    [TestMethod]
    public async Task FetchUrlAsync_Should_Post_Request_And_Return_Url()
    {
        // Arrange
        var expectedUrl = "https://cdn.trackunit.com/images/img-123.jpg";
        var expectedExpiry = DateTime.UtcNow.AddHours(1);

        // Fake handler to intercept HttpClient calls
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.AbsolutePath == "/internal/v0/media/get-url"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new GetUrlResponse (expectedUrl, expectedExpiry))
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://fake-api.trackunit.com")
        };

        var sut = new HttpImageUrlFetcher(httpClient);

        // Act
        var actualUrl = await sut.FetchUrlAsync("images/2025/11/02/img.jpg", CancellationToken.None);

        // Assert
        Assert.AreEqual(expectedUrl, actualUrl);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.AbsolutePath == "/internal/v0/media/get-url"),
            ItExpr.IsAny<CancellationToken>()
        );
    }
    private static HttpClient CreateHttpClientReturning(byte[] imageBytes, string contentType = "image/png")
    {
        var handler = new Mock<HttpMessageHandler>();

        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(imageBytes)
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType) }
                }
            });

        return new HttpClient(handler.Object);
    }
    private static IHttpClientFactory CreateFactory(HttpClient client)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
               .Returns(client);
        return factory.Object;
    }

}
