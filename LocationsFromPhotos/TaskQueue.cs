using System.Threading.Channels;

namespace LocationsFromPhotos;

public class TaskQueue
{
    private readonly Channel<Func<Task>> _taskQueue;
    private readonly int _maxConcurrentTasks;
    private int _currentRunningTasks;
    private bool _isLastBatch;
    private readonly object _lock = new();

    // Событие для уведомления, что все задачи завершены
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

        // Запускаем обработку задач в фоновом режиме
        for (int i = 0; i < _maxConcurrentTasks; i++)
        {
            tasks.Add(ProcessQueue());
        }

        await Task.WhenAll(tasks); // Ждем, пока все задачи завершатся
        _completionSource.SetResult(true); // Завершаем, если всё обработано
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

            // Ограничиваем количество одновременно выполняющихся задач
            if (_currentRunningTasks >= _maxConcurrentTasks)
            {
                await Task.Delay(100); // Ожидаем, если достигли максимума
                continue;
            }

            // Увеличиваем счетчик выполняемых задач
            _currentRunningTasks++;

            try
            {
                await task(); // Выполняем задачу
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {typeof(TaskQueue)}: {ex.Message}");
            }
            finally
            {
                // Уменьшаем счетчик и проверяем завершение всех задач
                _currentRunningTasks--;
                CheckForCompletion();
            }
        }
    }

    private void CheckForCompletion()
    {
        lock (_lock)
        {
            // Если все задачи завершены и в очереди больше нет задач
            if (_isLastBatch && _currentRunningTasks == 0 && _taskQueue.Reader.Count == 0)
            {
                _completionSource.SetResult(true); // Оповещаем о завершении всех задач
            }
        }
    }

    // Метод для ожидания завершения всех задач
    public Task WaitUntilAllTasksCompleted() => _completionSource.Task;
}