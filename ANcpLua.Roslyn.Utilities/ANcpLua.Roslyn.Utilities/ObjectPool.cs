// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// Adapted from Meziantou.Analyzer

using System.Collections.Concurrent;

namespace ANcpLua.Roslyn.Utilities;

/// <summary>
///     A pool of objects.
/// </summary>
/// <typeparam name="T">The type of objects to pool.</typeparam>
public abstract class ObjectPool<T> where T : class
{
    /// <summary>
    ///     Gets an object from the pool if one is available, otherwise creates one.
    /// </summary>
    public abstract T Get();

    /// <summary>
    ///     Return an object to the pool.
    /// </summary>
    public abstract void Return(T obj);
}

/// <summary>
///     Methods for creating <see cref="ObjectPool{T}" /> instances.
/// </summary>
public static class ObjectPool
{
    /// <summary>
    ///     Shared StringBuilder pool for common usage.
    /// </summary>
    public static ObjectPool<StringBuilder> SharedStringBuilderPool { get; } = CreateStringBuilderPool();

    /// <summary>
    ///     Creates an object pool with the default policy.
    /// </summary>
    public static ObjectPool<T> Create<T>(IPooledObjectPolicy<T>? policy = null) where T : class, new()
    {
        var provider = new DefaultObjectPoolProvider();
        return provider.Create(policy ?? new DefaultPooledObjectPolicy<T>());
    }

    /// <summary>
    ///     Creates a StringBuilder pool with default settings.
    /// </summary>
    public static ObjectPool<StringBuilder> CreateStringBuilderPool()
    {
        var provider = new DefaultObjectPoolProvider();
        return provider.Create(new StringBuilderPooledObjectPolicy());
    }
}

/// <summary>
///     Represents a policy for managing pooled objects.
/// </summary>
public interface IPooledObjectPolicy<T> where T : notnull
{
    /// <summary>
    ///     Create a <typeparamref name="T" />.
    /// </summary>
    T Create();

    /// <summary>
    ///     Runs some processing when an object was returned to the pool.
    /// </summary>
    /// <returns>True if the object should be returned to the pool.</returns>
    bool Return(T obj);
}

/// <summary>
///     A provider of <see cref="ObjectPool{T}" /> instances.
/// </summary>
public abstract class ObjectPoolProvider
{
    /// <summary>
    ///     Creates an <see cref="ObjectPool{T}" />.
    /// </summary>
    public ObjectPool<T> Create<T>() where T : class, new() => Create(new DefaultPooledObjectPolicy<T>());

    /// <summary>
    ///     Creates an <see cref="ObjectPool{T}" /> with the given policy.
    /// </summary>
    public abstract ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy) where T : class;
}

/// <summary>
///     Default implementation of <see cref="ObjectPool{T}" />.
/// </summary>
public class DefaultObjectPool<T> : ObjectPool<T> where T : class
{
    private readonly Func<T> _createFunc;
    private readonly int _maxCapacity;
    private readonly Func<T, bool> _returnFunc;
    private protected readonly ConcurrentQueue<T> Items = new();
    private int _numItems;
    private protected T? FastItem;

    /// <summary>
    ///     Creates an instance with default maximum retained count.
    /// </summary>
    public DefaultObjectPool(IPooledObjectPolicy<T> policy)
        : this(policy, Environment.ProcessorCount * 2)
    {
    }

    /// <summary>
    ///     Creates an instance with specified maximum retained count.
    /// </summary>
    public DefaultObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
    {
        _createFunc = policy.Create;
        _returnFunc = policy.Return;
        _maxCapacity = maximumRetained - 1;
    }

    /// <inheritdoc />
    public override T Get()
    {
        var item = FastItem;
        if (item == null || Interlocked.CompareExchange(ref FastItem, value: null, item) != item)
        {
            if (Items.TryDequeue(out item))
            {
                Interlocked.Decrement(ref _numItems);
                return item;
            }

            return _createFunc();
        }

        return item;
    }

    /// <inheritdoc />
    public override void Return(T obj) => ReturnCore(obj);

    private protected bool ReturnCore(T obj)
    {
        if (!_returnFunc(obj))
            return false;

        if (FastItem is not null || Interlocked.CompareExchange(ref FastItem, obj, comparand: null) is not null)
        {
            if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
            {
                Items.Enqueue(obj);
                return true;
            }

            Interlocked.Decrement(ref _numItems);
            return false;
        }

        return true;
    }
}

/// <summary>
///     The default <see cref="ObjectPoolProvider" />.
/// </summary>
public sealed class DefaultObjectPoolProvider : ObjectPoolProvider
{
    /// <summary>
    ///     The maximum number of objects to retain in the pool.
    /// </summary>
    public int MaximumRetained { get; set; } = Environment.ProcessorCount * 2;

    /// <inheritdoc />
    public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
    {
        if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
            return new DisposableObjectPool<T>(policy, MaximumRetained);

        return new DefaultObjectPool<T>(policy, MaximumRetained);
    }
}

/// <summary>
///     Default implementation for <see cref="PooledObjectPolicy{T}" />.
/// </summary>
public sealed class DefaultPooledObjectPolicy<T> : PooledObjectPolicy<T> where T : class, new()
{
    /// <inheritdoc />
    public override T Create() => new();

    /// <inheritdoc />
    public override bool Return(T obj)
    {
        if (obj is IResettable resettable)
            return resettable.TryReset();

        return true;
    }
}

/// <summary>
///     A base type for <see cref="IPooledObjectPolicy{T}" />.
/// </summary>
public abstract class PooledObjectPolicy<T> : IPooledObjectPolicy<T> where T : notnull
{
    /// <inheritdoc />
    public abstract T Create();

    /// <inheritdoc />
    public abstract bool Return(T obj);
}

/// <summary>
///     Defines a method to reset an object to its initial state.
/// </summary>
public interface IResettable
{
    /// <summary>
    ///     Reset the object to a neutral state.
    /// </summary>
    bool TryReset();
}

/// <summary>
///     Object pool for disposable types.
/// </summary>
public sealed class DisposableObjectPool<T> : DefaultObjectPool<T>, IDisposable where T : class
{
    private volatile bool _isDisposed;

    public DisposableObjectPool(IPooledObjectPolicy<T> policy) : base(policy)
    {
    }

    public DisposableObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained) : base(policy, maximumRetained)
    {
    }

    public void Dispose()
    {
        _isDisposed = true;
        DisposeItem(FastItem);
        FastItem = null;

        while (Items.TryDequeue(out var item))
            DisposeItem(item);
    }

    public override T Get()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(GetType().Name);

        return base.Get();
    }

    public override void Return(T obj)
    {
        if (_isDisposed || !ReturnCore(obj))
            DisposeItem(obj);
    }

    private static void DisposeItem(T? item)
    {
        if (item is IDisposable disposable)
            disposable.Dispose();
    }
}

/// <summary>
///     A policy for pooling <see cref="StringBuilder" /> instances.
/// </summary>
public sealed class StringBuilderPooledObjectPolicy : PooledObjectPolicy<StringBuilder>
{
    /// <summary>
    ///     Gets or sets the initial capacity. Defaults to 100.
    /// </summary>
    public int InitialCapacity { get; set; } = 100;

    /// <summary>
    ///     Gets or sets the maximum retained capacity. Defaults to 4096.
    /// </summary>
    public int MaximumRetainedCapacity { get; set; } = 4 * 1024;

    /// <inheritdoc />
    public override StringBuilder Create() => new(InitialCapacity);

    /// <inheritdoc />
    public override bool Return(StringBuilder obj)
    {
        if (obj.Capacity > MaximumRetainedCapacity)
            return false;

        obj.Clear();
        return true;
    }
}
