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
        protected class OccupiedCounters : Counters
        {
            public bool IsOccupied = false; 
        }
        protected string? _source;
        protected OccupiedCounters _counters;
        
        public ReadWriteGuarantee()
        {
            _counters = new OccupiedCounters();
        }

        public async Task<ReadResult> ReadAsync(int duration, int? millisecondsTimeout = null, bool throwsEx = false)
        {
            Monitor.Enter(_counters);
            try
            {
                if (_counters.WritersCount > 0)
                {
                    Monitor.Exit(_counters);
                    var canRead = () => _counters.WritersCount == 0;
                    if (millisecondsTimeout != null)
                        SpinWait.SpinUntil(canRead, (int)millisecondsTimeout);
                    else
                        SpinWait.SpinUntil(canRead);
                }

                _counters.ReadersCount++;
            }
            finally
            {
                if (Monitor.IsEntered(_counters))
                    Monitor.Exit(_counters);
            }

            _counters.IsOccupied = true;
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
                    _counters.IsOccupied = false;
                }
                return new ReadResult() { Status = Status.Success, Content = _source };
            });
        }

        public async Task<Status> WriteAsync(int duration, string text, int? millisecondsTimeout = null, bool throwsEx = false)
        {
            Monitor.Enter(_counters);
            try
            {
                _counters.WritersCount++;

                if (_counters.WritersCount > 1)
                {
                    var canWrite = () => !_counters.IsOccupied && _counters.WritersCount == 1;
                    Monitor.Exit(_counters);
                    if (millisecondsTimeout != null)
                        SpinWait.SpinUntil(canWrite, (int)millisecondsTimeout);
                    else
                        SpinWait.SpinUntil(canWrite);
                }

            }
            finally
            {
                if (Monitor.IsEntered(_counters))
                    Monitor.Exit(_counters);
            }

            _counters.IsOccupied = true;
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
                    _counters.IsOccupied = false;
                }
                return Status.Success;
            });
        }
    }
}
