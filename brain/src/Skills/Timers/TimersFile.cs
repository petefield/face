using Microsoft.Extensions.Options;

namespace Brain.Skills;

/// <summary>
/// Thread-safe access to the timers file (timers.md by default), one timer per row. Register as a
/// singleton: the instance's lock serialises all callers.
/// </summary>
public class TimersFile
{
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly string _path;

    public TimersFile(IOptions<TimersOptions> options)
    {
        var path = options.Value.FilePath;
        _path = string.IsNullOrWhiteSpace(path) ? new TimersOptions().FilePath : path;
    }

    /// <summary>Appends a row, creating the file if needed.</summary>
    public Task AppendLineAsync(string line, CancellationToken cancellationToken = default) =>
        WithLockAsync(async () =>
        {
            await File.AppendAllTextAsync(_path, line + Environment.NewLine, cancellationToken);
            return true;
        }, cancellationToken);

    /// <summary>Replaces the whole contents of the file.</summary>
    public Task WriteAllAsync(string content, CancellationToken cancellationToken = default) =>
        WithLockAsync(async () =>
        {
            await File.WriteAllTextAsync(_path, content, cancellationToken);
            return true;
        }, cancellationToken);

    /// <summary>Reads the file as one string per row, returning an empty list if it doesn't exist.</summary>
    public Task<List<string>> ReadAllAsync(CancellationToken cancellationToken = default) =>
        WithLockAsync(async () => File.Exists(_path)
            ? new List<string>(await File.ReadAllLinesAsync(_path, cancellationToken))
            : [], cancellationToken);

    /// <summary>Deletes the nth row (1-based). Returns false if there is no such row.</summary>
    public Task<bool> DeleteRowAsync(int n, CancellationToken cancellationToken = default) =>
        WithLockAsync(async () =>
        {
            if (!File.Exists(_path))
            {
                return false;
            }

            var lines = new List<string>(await File.ReadAllLinesAsync(_path, cancellationToken));
            if (n < 1 || n > lines.Count)
            {
                return false;
            }

            lines.RemoveAt(n - 1);
            await File.WriteAllLinesAsync(_path, lines, cancellationToken);
            return true;
        }, cancellationToken);

    /// <summary>
    /// Removes every row matching the predicate in a single locked read-filter-write, and returns the removed
    /// rows. Safe against concurrent appends, unlike deleting by row number from an earlier snapshot.
    /// </summary>
    public Task<List<string>> RemoveWhereAsync(Func<string, bool> predicate, CancellationToken cancellationToken = default) =>
        WithLockAsync(async () =>
        {
            if (!File.Exists(_path))
            {
                return new List<string>();
            }

            var lines = await File.ReadAllLinesAsync(_path, cancellationToken);
            var removed = lines.Where(predicate).ToList();
            if (removed.Count > 0)
            {
                await File.WriteAllLinesAsync(_path, lines.Where(line => !predicate(line)), cancellationToken);
            }

            return removed;
        }, cancellationToken);

    private async Task<T> WithLockAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)
    {
        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            _fileLock.Release();
        }
    }
}
