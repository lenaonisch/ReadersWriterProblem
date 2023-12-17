using ReadersWriterProblem;
int readersCount = 1;
ReadWriteTPL r = new ReadWriteTPL();
Task t = new Task(() => { });
for (int i = 0; i < readersCount; i++)
{
    
    t = r.ReadAsync((i+1)*1000);
    
}
await Task.Delay(1100).ContinueWith(t=> r.WriteAsync(100));
Console.ReadLine();