using ReadersWriterProblem;

namespace Tests
{
    public class TPLGuaranteeTests
    {
        [Fact]
        public void WritersHavePriorityOverReaders()
        {
            ReadWriteGuarantee r = new ReadWriteGuarantee();
            var readAction = async void () =>
            {
                var result = await r.ReadAsync(5);
                Assert.Equal(Status.Success, result.Status);
                Assert.Equal("ab", result.Content);
            };

            Parallel.Invoke(
                async () => Assert.Equal(Status.Success, (await r.ReadAsync(5)).Status), // someone initially occupies
                async () => Assert.Equal(Status.Success, await r.WriteAsync(5, "a")),
                readAction,
                async () => Assert.Equal(Status.Success, await r.WriteAsync(5, "b")),
                readAction);
        }

        [Fact]
        public async void CanWriteWhenAllFinishRead()
        {
            ReadWriteGuarantee r = new ReadWriteGuarantee();
            Task reader1 = new(() => { });
            Task reader2 = new(() => { });

            Parallel.Invoke(
                () => reader1 = r.ReadAsync(10),
                () => reader2 = r.ReadAsync(20),
                async () =>
                {
                    var status = await Task.Delay(17).ContinueWith(t => r.WriteAsync(5, "b"));
                    Assert.Equal(Status.Success, await status);
                },
                async () =>
                {
                    var status = await Task.Delay(15).ContinueWith(t => r.WriteAsync(5, "a"));
                    Assert.Equal(Status.Success, await status);
                });

            var status = await Task.WhenAll(reader1, reader2).ContinueWith(t => r.ReadAsync(5));
            Assert.Equal(Status.Success, (await status).Status);
            Assert.Equal("ab", (await status).Content);
        }

    }
}