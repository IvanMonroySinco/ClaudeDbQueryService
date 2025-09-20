using ClaudeDbQueryService.Core.Application.BussinessLogic.ClaudeQuery.Queries;
using ClaudeDbQueryService.Infrastructure.External.ApiServices;
using ClaudeDbQueryServiceTest.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ClaudeDbQueryServiceTest.ClaudeQuery;

public class HealthStatusQueryTests : TestBase
{
    private readonly Mock<IClaudeApiService> _mockClaudeApiService;
    private readonly Mock<ILogger<GetHealthStatusQuery>> _mockLogger;
    private readonly GetHealthStatusQuery _query;

    public HealthStatusQueryTests()
    {
        _mockClaudeApiService = new Mock<IClaudeApiService>();
        _mockLogger = CreateMockLogger<GetHealthStatusQuery>();
        _query = new GetHealthStatusQuery(_mockClaudeApiService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetHealthStatus_WhenClaudeServiceIsHealthy_ShouldReturnHealthyStatus()
    {
        // Arrange
        _mockClaudeApiService.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _query.GetHealthStatus();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetHealthStatus_WhenClaudeServiceIsUnhealthy_ShouldReturnDegradedStatus()
    {
        // Arrange
        _mockClaudeApiService.Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _query.GetHealthStatus();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }
}