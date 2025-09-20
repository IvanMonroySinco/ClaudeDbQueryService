using Microsoft.Extensions.Logging;
using Moq;

namespace ClaudeDbQueryServiceTest.Common;

public abstract class TestBase
{
    protected readonly Mock<ILogger> MockLogger;

    protected TestBase()
    {
        MockLogger = new Mock<ILogger>();
    }

    protected Mock<ILogger<T>> CreateMockLogger<T>()
    {
        return new Mock<ILogger<T>>();
    }
}