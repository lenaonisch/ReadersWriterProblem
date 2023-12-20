namespace ReadersWriterProblem
{
    public class ReadWriteTPL
    {
        private string? _source;
        Counters _counters;

        public ReadWriteTPL()
        {
            _counters = new Counters();
        }
        class Counters { public bool IsWriting; public int ReadersCount; }

        public async Task<Status> WriteAsync(int duration, string? text = null, bool throwsEx = false)
        {
            lock (_counters)
            {
                if (_counters.ReadersCount > 0 || _counters.IsWriting)
                    return Status.Occupied;

                _counters.IsWriting = true;
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
                    _counters.IsWriting = false;
                }
                return Status.Success;
            });
        }

        public async Task<ReadResult> ReadAsync(int duration, bool throwsEx = false)
        {
            lock (_counters)
            {
                if (_counters.IsWriting)
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
