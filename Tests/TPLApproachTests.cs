using ReadersWriterProblem;

namespace Tests
{
    public class TPLApproachTests
    {
        [Fact]
        public async void CanNotReadIfSomeoneWrites()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            var readTask = r.WriteAsync(2);
            Assert.Null(await r.ReadAsync(0));
        }

        [Fact]
        public async void CanNotWriteIfSomeoneReads()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            r.ReadAsync(20);

            Assert.Equal(Status.Occupied, await r.WriteAsync(0));
        }

        [Fact]
        // Timeline:
        // reader1 --
        // reader2 ----
        // writer1    -
        // writer2     -
        public async void CanNotWriteIfAtLeastOneStillReads()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            Task reader1 = r.ReadAsync(10);
            Task reader2 = r.ReadAsync(20);

            var status = await Task.Delay(15).ContinueWith(t => r.WriteAsync(5));
            Assert.Equal(Status.Occupied, await status);

            status = await Task.WhenAll(reader1, reader2).ContinueWith(t => r.WriteAsync(5));
            Assert.Equal(Status.Success, await status);
        }

        [Fact]
        public async void CanNotWriteIfSomeoneWrites()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            var writeTask = r.WriteAsync(2);
            Assert.Equal(Status.Occupied, await r.WriteAsync(0));
        }

        [Fact]
        public async void CanWriteWhenReadingFinished()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            string? content = await r.ReadAsync(2);
            Assert.Equal(Status.Success, await r.WriteAsync(0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async void CanReadWhenWritingFinished(int readersCount)
        {
            ReadWriteTPL r = new ReadWriteTPL();
            string? content;
            string expected = "test";
            await r.WriteAsync(2, expected);

            for (int i = 0; i < readersCount; i++)
            {
                content = await r.ReadAsync(1);
                Assert.Equal(expected, content);
                content = null;
            }
        }

        [Fact]
        public async void CanReadAfterWritingExceptionHandled()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            await Assert.ThrowsAsync<ReadWriteException>(() => r.WriteAsync(2, "text", true));
            Assert.Null(await r.ReadAsync(0));
        }

        [Fact]
        public async void CanWriteAfterReadingExceptionHandled()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            await Assert.ThrowsAsync<ReadWriteException>(() => r.ReadAsync(2, true));
            Assert.Equal(Status.Success, await r.WriteAsync(0));
        }
    }
}