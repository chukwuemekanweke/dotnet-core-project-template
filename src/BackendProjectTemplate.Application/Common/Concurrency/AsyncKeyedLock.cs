using System.Collections.Concurrent;

namespace BackendProjectTemplate.Application.Common.Concurrency;

internal sealed class AsyncKeyedLock
{
    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    internal int EntryCount => _entries.Count;

    public async ValueTask<Lease> AcquireAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        Entry entry;
        while (true)
        {
            entry = _entries.GetOrAdd(key, static _ => new Entry());
            lock (entry.SyncRoot)
            {
                if (!entry.IsRetired)
                {
                    entry.ReferenceCount++;
                    break;
                }
            }

            _entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
        }

        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
            return new Lease(this, key, entry);
        }
        catch
        {
            ReleaseReference(key, entry);
            throw;
        }
    }

    private void Release(string key, Entry entry)
    {
        entry.Semaphore.Release();
        ReleaseReference(key, entry);
    }

    private void ReleaseReference(string key, Entry entry)
    {
        var remove = false;
        lock (entry.SyncRoot)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount == 0)
            {
                entry.IsRetired = true;
                remove = true;
            }
        }

        if (remove)
            _entries.TryRemove(new KeyValuePair<string, Entry>(key, entry));
    }

    internal sealed class Lease : IAsyncDisposable
    {
        private AsyncKeyedLock? _owner;
        private readonly string _key;
        private readonly Entry _entry;

        internal Lease(AsyncKeyedLock owner, string key, Entry entry)
        {
            _owner = owner;
            _key = key;
            _entry = entry;
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _owner, null)?.Release(_key, _entry);
            return ValueTask.CompletedTask;
        }
    }

    internal sealed class Entry
    {
        public object SyncRoot { get; } = new();
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int ReferenceCount { get; set; }
        public bool IsRetired { get; set; }
    }
}
