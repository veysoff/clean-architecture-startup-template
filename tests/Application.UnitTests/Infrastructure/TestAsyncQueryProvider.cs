using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Application.UnitTests.Infrastructure;

/// <summary>
/// Helper class for mocking async EF Core queries in unit tests
/// </summary>
internal sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner)
    {
        _inner = inner;
    }

    public IQueryable CreateQuery(Expression expression)
    {
        return new TestAsyncEnumerable<TEntity>(expression);
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new TestAsyncEnumerable<TElement>(expression);
    }

    public object? Execute(Expression expression)
    {
        return _inner.Execute(expression);
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return _inner.Execute<TResult>(expression);
    }

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
    {
        Type resultType = typeof(TResult).GetGenericArguments()[0];
        object? executionResult = typeof(IQueryProvider)
            .GetMethod(
                name: nameof(IQueryProvider.Execute),
                genericParameterCount: 1,
                types: [typeof(Expression)])
            ?.MakeGenericMethod(resultType)
            .Invoke(this, [expression]);

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))
            ?.MakeGenericMethod(resultType)
            .Invoke(null, [executionResult])!;
    }
}

internal sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable)
        : base(enumerable)
    { }

    public TestAsyncEnumerable(Expression expression)
        : base(expression)
    { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public ValueTask<bool> MoveNextAsync()
    {
        return new ValueTask<bool>(inner.MoveNext());
    }

    public T Current => inner.Current;

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }
}
