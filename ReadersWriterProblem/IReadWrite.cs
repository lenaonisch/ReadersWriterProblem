namespace ReadersWriterProblem
{
    public interface IReadWrite
    {
        public Task<Status> WriteAsync(int duration, string? text = null, bool throwsEx = false);
        public Task<ReadResult> ReadAsync(int duration, bool throwsEx = false);
    }
}
