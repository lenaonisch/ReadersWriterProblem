using ReadersWriterProblem;

namespace Tests
{
    public class TPLGuaranteeTests
    {
        /// <summary>
        /// Reader initially occupies the source
        /// Then writer writes for 20ms. Two readers come and a writer with 5ms delay
        /// Test case checks that readers read after writers
        /// </summary>
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
                async () =>
                {
                    Assert.Equal(Status.Success, (await r.ReadAsync(5)).Status);
                },
                async () =>
                {
                    var status = r.WriteAsync(20, "a");
                    Assert.Equal(1, r.WritersInQueue);
                    Assert.Equal(Status.Success, await status);
                },
                readAction,
                async () =>
                {
                    var status = await Task.Delay(5).ContinueWith(t => r.WriteAsync(20, "b"));
                    Assert.Equal(2, r.WritersInQueue);
                    Assert.Equal(Status.Success, await status);
                },
                readAction);
        }
        /// <summary>
        /// While reading for 20 ms, reader with 10ms delay and 2 writers (5 and 10ms delay) come
        /// Test case checks that both writers finished their work before the reader
        /// </summary>
        [Fact]
        public void CanWriteAsSoonAsPossible()
        {
            ReadWriteGuarantee r = new ReadWriteGuarantee();
            Task reader1 = new(() => { });
            Task reader2 = new(() => { });

            Parallel.Invoke(
                () => reader1 = r.ReadAsync(20),
                async () =>
                {
                    var status = await Task.Delay(10).ContinueWith(t => r.ReadAsync(0));
                    var t = await status;
                    Assert.Equal(Status.Success, t.Status);
                    Assert.Equal("ab", t.Content);
                },
                async () =>
                {
                    var status = await Task.Delay(10).ContinueWith(t => r.WriteAsync(5, "b"));
                    Assert.Equal(Status.Success, await status);
                },
                async () =>
                {
                    var status = await Task.Delay(5).ContinueWith(t => r.WriteAsync(6, "a"));
                    Assert.Equal(Status.Success, await status);
                });
        }

        [Fact]
        public void ReadersWritersQueues()
        {
            ReadWriteGuarantee r = new ReadWriteGuarantee();
            Task reader1 = new(() => { });
            Task reader2 = new(() => { });

            Parallel.Invoke(
                async () =>
                {
                    Assert.Equal(0, r.ReadersInQueue);
                    Assert.Equal(0, r.WritersInQueue);
                    await r.ReadAsync(20);
                    Assert.Equal(0, r.WritersInQueue);
                }, // while reading for 20 ms, reader and 2 writers come
                async () =>
                {
                    await Task.Delay(10).ContinueWith(t => r.ReadAsync(0));
                    Assert.Equal(1, r.ReadersInQueue);
                    Assert.Equal(0, r.WritersInQueue);
                },
                async () =>
                {
                    await Task.Delay(10).ContinueWith(t => r.WriteAsync(5, "b"));
                    Assert.Equal(1, r.ReadersInQueue);
                    Assert.Equal(2, r.WritersInQueue);
                },
                async () =>
                {
                    await Task.Delay(5).ContinueWith(t => r.WriteAsync(6, "a"));
                    Assert.Equal(1, r.ReadersInQueue);
                    Assert.Equal(1, r.WritersInQueue);
                });
        }
    }
}