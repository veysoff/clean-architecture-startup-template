using Application.Abstractions.Behaviors;
using Application.Abstractions.Caching;
using Application.Abstractions.Messaging;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using SharedKernel;

namespace Application.UnitTests.Behaviors;

public sealed class QueryCachingDecoratorTests
{
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<IQueryHandler<TestQuery, TestResponse>> _handlerMock;
    private readonly Mock<ILogger<QueryCachingDecorator<TestQuery, TestResponse>>> _loggerMock;
    private readonly QueryCachingDecorator<TestQuery, TestResponse> _decorator;

    public QueryCachingDecoratorTests()
    {
        _cacheMock = new Mock<IDistributedCache>();
        _handlerMock = new Mock<IQueryHandler<TestQuery, TestResponse>>();
        _loggerMock = new Mock<ILogger<QueryCachingDecorator<TestQuery, TestResponse>>>();
        _decorator = new QueryCachingDecorator<TestQuery, TestResponse>(
            _handlerMock.Object,
            _cacheMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnCachedValue_WhenCacheHit()
    {
        // Arrange
        var query = new TestQuery(Guid.NewGuid());
        var cachedResponse = new TestResponse("Cached Data");
        var cachedResult = Result.Success(cachedResponse);
        string serializedCache = System.Text.Json.JsonSerializer.Serialize(cachedResult);

        _cacheMock
            .Setup(x => x.GetAsync(query.CacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(System.Text.Encoding.UTF8.GetBytes(serializedCache));

        // Act
        var result = await _decorator.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Data.Should().Be("Cached Data");

        // Verify inner handler was NOT called
        _handlerMock.Verify(
            x => x.Handle(It.IsAny<TestQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCallInnerHandler_WhenCacheMiss()
    {
        // Arrange
        var query = new TestQuery(Guid.NewGuid());
        var handlerResponse = new TestResponse("Fresh Data");
        var handlerResult = Result.Success(handlerResponse);

        _cacheMock
            .Setup(x => x.GetAsync(query.CacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _handlerMock
            .Setup(x => x.Handle(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        // Act
        var result = await _decorator.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Data.Should().Be("Fresh Data");

        // Verify inner handler WAS called
        _handlerMock.Verify(
            x => x.Handle(query, It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify result was cached
        _cacheMock.Verify(
            x => x.SetAsync(
                query.CacheKey,
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotCacheFailureResult()
    {
        // Arrange
        var query = new TestQuery(Guid.NewGuid());
        var error = new Error("TestError", "Test error message", ErrorType.Failure);
        var failureResult = Result.Failure<TestResponse>(error);

        _cacheMock
            .Setup(x => x.GetAsync(query.CacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _handlerMock
            .Setup(x => x.Handle(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResult);

        // Act
        var result = await _decorator.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("TestError");

        // Verify result was NOT cached
        _cacheMock.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUseCacheExpiration_WhenSpecified()
    {
        // Arrange
        var query = new TestQuery(Guid.NewGuid());
        var handlerResponse = new TestResponse("Fresh Data");
        var handlerResult = Result.Success(handlerResponse);

        _cacheMock
            .Setup(x => x.GetAsync(query.CacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[]?)null);

        _handlerMock
            .Setup(x => x.Handle(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(handlerResult);

        // Act
        await _decorator.Handle(query, CancellationToken.None);

        // Assert - Verify cache was set with correct expiration
        _cacheMock.Verify(
            x => x.SetAsync(
                query.CacheKey,
                It.IsAny<byte[]>(),
                It.Is<DistributedCacheEntryOptions>(opts =>
                    opts.AbsoluteExpirationRelativeToNow == query.Expiration),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Test query and response types
    public sealed record TestQuery(Guid Id) : IQuery<TestResponse>, ICachedQuery
    {
        public string CacheKey => $"test:{Id}";
        public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
    }

    public sealed record TestResponse(string Data);
}
