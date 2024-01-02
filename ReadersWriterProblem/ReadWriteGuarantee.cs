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
    public class ReadWriteGuarantee : IReadWrite
    {
        protected string? _source;
        protected Counters _counters;

        public ReadWriteGuarantee()
        {
            _counters = new Counters();
        }

        public async Task<ReadResult> ReadAsync(int duration, bool throwsEx = false)
        {
            Monitor.Enter(_counters);
            try
            {
                if (_counters.WritersCount > 0)
                {
                    Monitor.Exit(_counters);
                    SpinWait.SpinUntil(() => _counters.WritersCount == 0);
                }

                _counters.ReadersCount++;
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
                }
                return new ReadResult() { Status = Status.Success, Content = _source };
            });
        }

        public async Task<Status> WriteAsync(int duration, string? text = null, bool throwsEx = false)
        {
            Monitor.Enter(_counters);
            try
            {
                if (_counters.ReadersCount > 0 || _counters.WritersCount > 0)
                {
                    Monitor.Exit(_counters);
                    SpinWait.SpinUntil(() => _counters.ReadersCount == 0 || _counters.WritersCount == 0);
                }

                _counters.WritersCount++;
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
