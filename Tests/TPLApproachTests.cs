using ReadersWriterProblem;

namespace Tests
{
    public class TPLApproachTests
    {
        [Fact]
        public async void CanNotReadIfSomeoneWrites()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            var writeTask = r.WriteAsync(2);
            Assert.Equal(Status.Occupied, (await r.ReadAsync(0)).Status);
        }

        [Fact]
        public void CanNotWriteManyIfWrites()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            Parallel.Invoke(
                async () => Assert.Equal(Status.Success, await r.WriteAsync(2)),
                async () => Assert.Equal(Status.Occupied, await r.WriteAsync(2)),
                async () => Assert.Equal(Status.Occupied, await r.WriteAsync(2)));
        }

        [Fact]
        public void CanNotWriteManyIfReads()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            Parallel.Invoke(
                async () => Assert.Equal(Status.Success, (await r.ReadAsync(10)).Status),
                async () => Assert.Equal(Status.Occupied, await r.WriteAsync(2)),
                async () => Assert.Equal(Status.Occupied, await r.WriteAsync(2)));
        }

        [Fact]
        public async void CanNotReadManyIfWrites()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            Parallel.Invoke(
                async () => Assert.Equal(Status.Success, await r.WriteAsync(2)),
                async () => Assert.Equal(Status.Occupied, (await r.ReadAsync(2)).Status),
                async () => Assert.Equal(Status.Occupied, (await r.ReadAsync(2)).Status));
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
            Task reader1 = new(() => { });
            Task reader2 = new(() => { });

            Parallel.Invoke(
                () => reader1 = r.ReadAsync(10),
                () => reader2 = r.ReadAsync(20),
                async () =>
                {
                    var status = await Task.Delay(15).ContinueWith(t => r.WriteAsync(5));
                    Assert.Equal(Status.Occupied, await status);
                });

            var status = await Task.WhenAll(reader1, reader2).ContinueWith(t => r.WriteAsync(5));
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
            var readResult = await r.ReadAsync(2);
            Assert.Equal(Status.Success, await r.WriteAsync(0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        public async void CanReadWhenWritingFinished(int readersCount)
        {
            ReadWriteTPL r = new ReadWriteTPL();
            ReadResult? content;
            string expected = "test";
            await r.WriteAsync(2, expected);

            for (int i = 0; i < readersCount; i++)
            {
                content = await r.ReadAsync(1);
                Assert.Equal(expected, content?.Content);
                Assert.Equal(Status.Success, content?.Status);
                content = null;
            }
        }

        [Fact]
        public async void CanReadAfterWritingExceptionHandled()
        {
            ReadWriteTPL r = new ReadWriteTPL();
            await Assert.ThrowsAsync<ReadWriteException>(() => r.WriteAsync(2, "text", true));
            var readResult = await r.ReadAsync(0);
            Assert.Equal(Status.Success, readResult.Status);
            Assert.Null(readResult.Content);
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