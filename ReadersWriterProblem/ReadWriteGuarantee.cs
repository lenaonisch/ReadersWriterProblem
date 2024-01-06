using static ReadersWriterProblem.ReadWriteTPL;

namespace ReadersWriterProblem
{
    /// <summary>
    ///1) Всё как прежде - читатели и писатели
    ///2) Писатели имеют приоритет над читателями
    ///3) тут был спойлер :)
    ///4) Вести подсчёт количества попыток записи/чтения
    ///5) Гарантировать запись/чтение
    ///6) опционально: добавить механизм отмены(ручной или авто) при невозможности записи/чтения за определённый промежуток времени
    /// </summary>
    public class ReadWriteGuarantee
    {
        protected string? _source;
        protected ReadingNowCounters _counters;
        public int ReadersInQueue { get { return _counters.ReadersCount; } }
        public int WritersInQueue { get { return _counters.WritersCount; } }

        public ReadWriteGuarantee()
        {
            _counters = new ReadingNowCounters();
        }

        public class ReadingNowCounters : Counters { public bool ReadingNow = false; }

        public async Task<ReadResult> ReadAsync(int duration, int? millisecondsTimeout = null, bool throwsEx = false)
        {
            bool canRead = true;
            Monitor.Enter(_counters);
            try
            {
                if (_counters.WritersCount > 0)
                {
                    Monitor.Exit(_counters);
                    var readCondition = () => _counters.WritersCount == 0;
                    if (millisecondsTimeout != null)
                        canRead = SpinWait.SpinUntil(readCondition, (int)millisecondsTimeout);
                    else
                        SpinWait.SpinUntil(readCondition);
                }

                if (canRead)
                {
                    object rLock = new object();
                    lock (rLock)
                    {
                        _counters.ReadersCount++;
                        _counters.ReadingNow = true;
                    }
                }
                else return new ReadResult() { Status = Status.Occupied };

            }
            finally
            {
                if (Monitor.IsEntered(_counters))
                    Monitor.Exit(_counters);
            }

            return await Task.Delay(duration).ContinueWith(t =>
            {
                try
                {
                    if (throwsEx)
                    {
                        throw new ReadWriteException();
                    }
                }
                catch { throw; }
                finally
                {
                    _counters.ReadersCount--;
                    _counters.ReadingNow = false;
                }
                return new ReadResult() { Status = Status.Success, Content = _source };
            });
        }

        public async Task<Status> WriteAsync(int duration, string text, int? millisecondsTimeout = null, bool throwsEx = false)
        {
            bool canWrite = true;
            Monitor.Enter(_counters);
            try
            {
                _counters.WritersCount++;

                var writeCondition = () => _counters.WritersCount == 1 && _counters.ReadingNow == false;
                Monitor.Exit(_counters);
                if (millisecondsTimeout != null)
                    canWrite = SpinWait.SpinUntil(writeCondition, (int)millisecondsTimeout);
                else
                    SpinWait.SpinUntil(writeCondition);

            }
            finally
            {
                if (Monitor.IsEntered(_counters))
                    Monitor.Exit(_counters);
            }

            if (!canWrite)
            {
                lock (_counters)
                {
                    _counters.WritersCount--;
                }
                return Status.Occupied;
            }

            return await Task.Delay(duration).ContinueWith(t =>
            {
                try
                {
                    if (throwsEx)
                    {
                        throw new ReadWriteException();
                    }
                    _source += text;
                }
                catch { throw; }
                finally
                {
                    _counters.WritersCount--;
                }
                return Status.Success;
            });
        }
    }
}
