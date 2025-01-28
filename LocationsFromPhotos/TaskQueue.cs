using System.Threading.Channels;

namespace LocationsFromPhotos;

public class TaskQueue
{
    private readonly Channel<Func<Task>> _taskQueue;
    private readonly int _maxConcurrentTasks;
    private int _currentRunningTasks;
    private bool _isLastBatch;
    private readonly object _lock = new();

    private readonly TaskCompletionSource<bool> _completionSource = new();

    public TaskQueue(int maxConcurrentTasks)
    {
        _maxConcurrentTasks = maxConcurrentTasks;
        _taskQueue = Channel.CreateUnbounded<Func<Task>>();
        StartProcessing();
    }

    public async Task EnqueueTask(Func<Task> task)
    {
        await _taskQueue.Writer.WriteAsync(task);
    }

    public void MarkAsLastBatch()
    {
        lock (_lock)
        {
            _isLastBatch = true;
        }
    }

    private async void StartProcessing()
    {
        var tasks = new List<Task>();

        for (int i = 0; i < _maxConcurrentTasks; i++)
        {
            tasks.Add(ProcessQueue());
        }

        await Task.WhenAll(tasks);
        _completionSource.SetResult(true);
    }

    private async Task ProcessQueue()
    {
        await foreach (Func<Task> task in _taskQueue.Reader.ReadAllAsync())
        {
            if (_isLastBatch)
            {
                Console.SetCursorPosition(0, 7);
                Console.WriteLine($"...number of photos waiting to receive GEO data: {_taskQueue.Reader.Count}{new string(' ', Console.WindowWidth)}");
            }

            if (_currentRunningTasks >= _maxConcurrentTasks)
            {
                await Task.Delay(100);
                continue;
            }

            _currentRunningTasks++;

            try
            {
                await task();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {typeof(TaskQueue)}: {ex.Message}");
            }
            finally
            {
                _currentRunningTasks--;
                CheckForCompletion();
            }
        }
    }

    private void CheckForCompletion()
    {
        lock (_lock)
        {
            if (_isLastBatch && _currentRunningTasks == 0 && _taskQueue.Reader.Count == 0)
            {
                _completionSource.SetResult(true);
            }
        }
    }

    public Task WaitUntilAllTasksCompleted() => _completionSource.Task;
}