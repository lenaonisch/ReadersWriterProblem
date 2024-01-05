using ReadersWriterProblem;

ReadWriteGuarantee r = new ReadWriteGuarantee();
Task reader1 = new(() => { });
Task reader2 = new(() => { });

Parallel.Invoke(
    () => reader1 = r.ReadAsync(10),
    () => reader2 = r.ReadAsync(20),
    async () =>
    {
        var status = await Task.Delay(17).ContinueWith(t => r.WriteAsync(6, "b"));

    },
    async () =>
    {
        var status = await Task.Delay(13).ContinueWith(t => r.WriteAsync(5, "a"));

    });

var status = await Task.WhenAll(reader1, reader2).ContinueWith(t => r.ReadAsync(5));
Console.ReadLine();