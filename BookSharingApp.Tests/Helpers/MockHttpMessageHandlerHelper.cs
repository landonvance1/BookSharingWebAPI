using System.Net;
using Moq;
using Moq.Protected;

namespace BookSharingApp.Tests.Helpers
{
    public static class MockHttpMessageHandlerHelper
    {
        /// <summary>
        /// Creates a mock HttpMessageHandler that returns a successful response with the given JSON content
        /// </summary>
        public static HttpMessageHandler CreateMockHandler(string jsonResponse, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var mockHandler = new Mock<HttpMessageHandler>();

            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(jsonResponse)
                });

            return mockHandler.Object;
        }

        /// <summary>
        /// Creates a mock HttpMessageHandler that throws an exception when SendAsync is called
        /// </summary>
        public static HttpMessageHandler CreateMockHandlerWithException(Exception exception)
        {
            var mockHandler = new Mock<HttpMessageHandler>();

            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(exception);

            return mockHandler.Object;
        }
    }
}
