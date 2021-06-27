using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchEbooks.Infrastructure
{
    public class AsyncReaderWriterLock : IDisposable
    {
        private readonly SemaphoreSlim _readerLock, _writerLock;
        private int _readerCount;

        public AsyncReaderWriterLock()
        {
            _readerLock = new SemaphoreSlim(1, 1);
            _writerLock = new SemaphoreSlim(1, 1);
            _readerCount = 0;
        }

        public async Task AcquireWriterLock(CancellationToken token = default)
        {
            await _writerLock.WaitAsync(token);
            await AcquireReaderLockSafe(token);
        }

        public void ReleaseWriterLock()
        {
            _readerLock.Release();
            _writerLock.Release();
        }

        public async Task AcquireReaderLock(CancellationToken token = default)
        {
            await _writerLock.WaitAsync(token);

            if (Interlocked.Increment(ref _readerCount) == 1)
                await AcquireReaderLockSafe(token);

            _writerLock.Release();
        }

        public void ReleaseReaderLock()
        {
            if (Interlocked.Decrement(ref _readerCount) == 0)
                _readerLock.Release();
        }

        public void Dispose()
        {
            _writerLock.Dispose();
            _readerLock.Dispose();
        }

        private async Task AcquireReaderLockSafe(CancellationToken token = default)
        {
            try
            {
                await _readerLock.WaitAsync(token);
            }
            catch
            {
                _readerLock.Release();
                throw;
            }
        }
    }
}
