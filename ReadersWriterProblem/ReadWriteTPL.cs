namespace ReadersWriterProblem
{
    public class ReadWriteTPL
    {
        private string? _source;
        private bool _isWriting;
        private int _readersCount;

        public async Task<Status> WriteAsync(int duration, string? text = null, bool throwsEx = false)
        {
            if (_readersCount > 0 || _isWriting)
                return Status.Occupied;

            _isWriting = true;
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
                    _isWriting = false;
                }
                return Status.Success;
            });
        }

        public async Task<string?> ReadAsync(int duration, bool throwsEx = false)
        {
            if (_isWriting)
                return null;

            _readersCount++;
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
                    _readersCount--;
                }
                return _source;
            });
        }
    }

    public enum Status
    {
        None = 0,
        Occupied,
        Success
    }
}
