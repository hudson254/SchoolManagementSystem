using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using SMS.Domain.Interfaces;

namespace SMS.UnitTests.Common
{
    /// <summary>
    /// Test helper for <see cref="IUnitOfWork"/> doubles.
    /// <para>
    /// The real <c>UnitOfWork.ExecuteInTransactionAsync</c> runs the supplied
    /// operation inside a database transaction (wrapping it in the Npgsql execution
    /// strategy), commits on success and rolls back + rethrows on failure. A bare
    /// <c>Mock&lt;IUnitOfWork&gt;</c> returns a completed task with a default value
    /// instead, which silently swallows the whole operation.
    /// <para>
    /// <c>RunsTransactionInline&lt;T&gt;</c> makes the double behave like the real
    /// thing for the unit under test: the operation runs, its result is returned and
    /// an exception propagates exactly as it does in production (the rollback itself
    /// is the database's job and is covered by the integration/API suites against
    /// PostgreSQL).
    /// </para>
    /// </summary>
    public static class UnitOfWorkMockExtensions
    {
        public static Mock<IUnitOfWork> RunsTransactionInline<T>(this Mock<IUnitOfWork> unitOfWork)
        {
            unitOfWork
                .Setup(u => u.ExecuteInTransactionAsync(
                    It.IsAny<Func<Task<T>>>(), It.IsAny<CancellationToken>()))
                .Returns((Func<Task<T>> operation, CancellationToken _) => operation());

            return unitOfWork;
        }
    }
}
