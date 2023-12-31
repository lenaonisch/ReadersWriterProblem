namespace ReadersWriterProblem
{
    public class ReadWriteTPL: IReadWrite
    {
        private string? _source;
        Counters _counters;

        public ReadWriteTPL()
        {
            _counters = new Counters();
        }
        public class Counters { public int WritersCount; public int ReadersCount; }

        public async Task<Status> WriteAsync(int duration, string? text = null, bool throwsEx = false)
        {
            lock (_counters)
            {
                if (_counters.ReadersCount > 0 || _counters.WritersCount > 0)
                    return Status.Occupied;

                _counters.WritersCount++;
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

        public async Task<ReadResult> ReadAsync(int duration, bool throwsEx = false)
        {
            lock (_counters)
            {
                if (_counters.WritersCount > 0)
                    return new ReadResult() { Status = Status.Occupied, Content = null };

                _counters.ReadersCount++;
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
    }

    public struct ReadResult
    {
        public string? Content;
        public Status Status;
    }
    public enum Status
    {
        None = 0,
        Occupied,
        Success
    }
}
