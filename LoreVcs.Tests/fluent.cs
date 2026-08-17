using Xunit;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using LoreVcs.Types;
using LoreVcs.Types.Args;
using LoreVcs.Types.Events;
using LoreVcs.Types.Enums;

namespace LoreVcs.Tests;

public class LoreFluentAPITests
{
    private string repositoryUrl = string.Empty;
    public LoreFluentAPITests()
    {
        repositoryUrl = Guid.NewGuid().ToString();
    }

    [Fact]
    public void Wait_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var result = Lore.RepositoryCreate(globalArgs, repositoryArgs).Wait();

        Assert.True(result == 0);
    }

    [Fact]
    public async Task WaitAsync_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var task = Lore.RepositoryCreate(globalArgs, repositoryArgs).WaitAsync();
        Assert.NotNull(task);

        var result = await task;
        Assert.True(result == 0);
    }

    [Fact]
    public async Task Same_User_Context_Multiple_WaitAsync_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        List<Task<int>> tasks = new();
        const int numTasks = 5;
        for (int i = 0; i < numTasks; ++i)
        {
            var guid = Guid.NewGuid().ToString();
            var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir + guid };
            var repo = guid;
            var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repo };
            var task = Lore.RepositoryCreate(globalArgs, repositoryArgs)
                            .Callback((LoreEventFFI loreEvent, ulong userContext) =>
                            {
                                Assert.Equal(123ul, userContext);
                            })
                            .UserContext(123)
                            .WaitAsync();
            tasks.Add(task);
        }

        for (int i = 0; i < numTasks; ++i)
        {
            var task = tasks[i];
            Assert.NotNull(task);
            var result = await task;
            Assert.Equal(0, result);
        }
    }

    [Fact]
    public async Task AsyncIter_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var events = Lore.RepositoryCreate(globalArgs, repositoryArgs).AsyncIter();
        var completeEvent = await events.OfType<LoreCompleteEventData>().FirstAsync();
        Assert.True(completeEvent.Status == 0);
    }

    [Fact]
    public async Task AsyncIter_With_Filter_Does_Not_Block()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var events = Lore.RepositoryCreate(globalArgs, repositoryArgs)
            .FilterByType([LoreEventTag.COMPLETE])
            .AsyncIter();

        await foreach (var ev in events)
        {
            if (ev is LoreCompleteEventData completeEvent)
            {
                Assert.Equal(0, completeEvent.Status);
            }
            else
            {
                Assert.Fail("Got events other than LoreCompleteEventData.");
            }
        }
    }

    [Fact]
    public void Collect_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var events = Lore.RepositoryCreate(globalArgs, repositoryArgs).Collect();

        Assert.True(events.Count > 0);

        var ev = events.OfType<LoreCompleteEventData>().First();
        Assert.True(ev.Status == 0);
    }

    [Fact]
    public async Task CollectAsync_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };


        var events = await Lore.RepositoryCreate(globalArgs, repositoryArgs).CollectAsync();

        Assert.True(events.Count > 0);

        var ev = events.OfType<LoreCompleteEventData>().First();
        Assert.True(ev.Status == 0);
    }

    [Fact]
    public void Filter_By_Type_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var events = Lore.RepositoryCreate(globalArgs, repositoryArgs)
                        .FilterByType([LoreEventTag.LOG, LoreEventTag.COMPLETE, LoreEventTag.END])
                        .Collect();

        foreach (LoreEvent ev in events)
        {
            Assert.True(ev.Tag == LoreEventTag.LOG || ev.Tag == LoreEventTag.COMPLETE || ev.Tag == LoreEventTag.END);
        }
    }

    [Fact]
    public void User_Context_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var events = Lore.RepositoryCreate(globalArgs, repositoryArgs)
                        .UserContext(1234)
                        .Callback((LoreEventFFI loreEvent, ulong userContext) =>
                        {
                            Assert.Equal(1234ul, userContext);
                        })
                        .Wait();
    }

    [Fact]
    public void GlobalCallback_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryCreateArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };
        var repositoryStatusArgs = new LoreRepositoryStatusArgs { };

        const ulong createContext = 7000001;
        const ulong statusContext = 7000002;
        const ulong afterDisposeContext = 7000003;

        var logMessages = new ConcurrentDictionary<ulong, ConcurrentQueue<string>>();

        using (Lore.GlobalCallback(LoreEventTag.LOG,
          (loreEvent, userContext) =>
          {
              logMessages
                  .GetOrAdd(userContext, _ => new ConcurrentQueue<string>())
                  .Enqueue(loreEvent.GetData<LoreLogEventDataFFI>().Message);
          }))
        {
            Lore.RepositoryCreate(globalArgs, repositoryCreateArgs)
                .UserContext(createContext)
                .Wait();
            Assert.True(MessageCount(logMessages, createContext) > 0);

            Lore.RepositoryStatus(globalArgs, repositoryStatusArgs)
                .UserContext(statusContext)
                .Wait();
            Assert.True(MessageCount(logMessages, statusContext) > 0);
        }

        // After disposing the global callback it should no longer be executed:
        Lore.RepositoryStatus(globalArgs, repositoryStatusArgs)
            .UserContext(afterDisposeContext)
            .Wait();
        Assert.Equal(0, MessageCount(logMessages, afterDisposeContext));
    }

    private static int MessageCount(
        ConcurrentDictionary<ulong, ConcurrentQueue<string>> messages,
        ulong userContext
    ) => messages.TryGetValue(userContext, out var queue) ? queue.Count : 0;

    [Fact]
    public async Task GlobalCallback_Async_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryCreateArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };
        var repositoryStatusArgs = new LoreRepositoryStatusArgs { };

        const ulong createContext = 7100001;
        const ulong statusContext = 7100002;
        const ulong afterDisposeContext = 7100003;

        var logMessages = new ConcurrentDictionary<ulong, ConcurrentQueue<string>>();

        var unregisterGlobalCallback = Lore.GlobalCallback(LoreEventTag.LOG,
          (loreEvent, userContext) =>
          {
              logMessages
                  .GetOrAdd(userContext, _ => new ConcurrentQueue<string>())
                  .Enqueue(loreEvent.GetData<LoreLogEventDataFFI>().Message);
          }
        );

        await Lore.RepositoryCreate(globalArgs, repositoryCreateArgs)
            .UserContext(createContext)
            .WaitAsync();
        Assert.True(MessageCount(logMessages, createContext) > 0);

        await Lore.RepositoryStatus(globalArgs, repositoryStatusArgs)
            .UserContext(statusContext)
            .WaitAsync();
        Assert.True(MessageCount(logMessages, statusContext) > 0);

        unregisterGlobalCallback.Dispose();

        // After disposing the global callback it should no longer be executed:
        await Lore.RepositoryStatus(globalArgs, repositoryStatusArgs)
            .UserContext(afterDisposeContext)
            .WaitAsync();
        Assert.Equal(0, MessageCount(logMessages, afterDisposeContext));
    }

    // --- Non-zero return code tests ---

    [Fact]
    public void Wait_NonZero_ReturnCode()
    {
        var invalidArgs = new LoreGlobalArgs
        {
            Offline = true,
            RepositoryPath = "/tmp/nonexistent-repo-path-" + Guid.NewGuid()
        };
        var error = Assert.Throws<LoreError>(
            () => Lore.RepositoryStatus(invalidArgs, new LoreRepositoryStatusArgs()).Wait()
        );
        Assert.NotEqual(0, error.ReturnCode);
    }

    [Fact]
    public async Task WaitAsync_NonZero_ReturnCode()
    {
        var invalidArgs = new LoreGlobalArgs
        {
            Offline = true,
            RepositoryPath = "/tmp/nonexistent-repo-path-" + Guid.NewGuid()
        };
        var error = await Assert.ThrowsAsync<LoreError>(
            () => Lore.RepositoryStatus(invalidArgs, new LoreRepositoryStatusArgs()).WaitAsync()
        );
        Assert.NotEqual(0, error.ReturnCode);
    }

    [Fact]
    public void Collect_NonZero_ReturnCode()
    {
        var invalidArgs = new LoreGlobalArgs
        {
            Offline = true,
            RepositoryPath = "/tmp/nonexistent-repo-path-" + Guid.NewGuid()
        };
        var error = Assert.Throws<LoreError>(
            () => Lore.RepositoryStatus(invalidArgs, new LoreRepositoryStatusArgs()).Collect()
        );
        Assert.NotEqual(0, error.ReturnCode);
    }

    [Fact]
    public async Task CollectAsync_NonZero_ReturnCode()
    {
        var invalidArgs = new LoreGlobalArgs
        {
            Offline = true,
            RepositoryPath = "/tmp/nonexistent-repo-path-" + Guid.NewGuid()
        };
        var error = await Assert.ThrowsAsync<LoreError>(
            () => Lore.RepositoryStatus(invalidArgs, new LoreRepositoryStatusArgs()).CollectAsync()
        );
        Assert.NotEqual(0, error.ReturnCode);
    }

    [Fact]
    public async Task AsyncIter_NonZero_ReturnCode()
    {
        var invalidArgs = new LoreGlobalArgs
        {
            Offline = true,
            RepositoryPath = "/tmp/nonexistent-repo-path-" + Guid.NewGuid()
        };
        var events = new List<LoreEvent>();
        // The iterator yields the events first, then throws once the operation
        // completes with a non-zero return code.
        var error = await Assert.ThrowsAsync<LoreError>(async () =>
        {
            await foreach (var ev in Lore.RepositoryStatus(invalidArgs, new LoreRepositoryStatusArgs()).AsyncIter())
            {
                events.Add(ev);
            }
        });
        Assert.NotEqual(0, error.ReturnCode);
        var completeEvents = events.OfType<LoreCompleteEventData>().ToList();
        Assert.Single(completeEvents);
        Assert.NotEqual(0, completeEvents[0].Status);
    }

    // --- Double-execution prevention tests ---

    [Fact]
    public void Double_Wait_Raises()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var executor = Lore.RepositoryCreate(globalArgs, repositoryArgs);
        executor.Wait();

        Assert.Throws<InvalidOperationException>(() => executor.Wait());
    }

    [Fact]
    public void Double_Collect_Raises()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var executor = Lore.RepositoryCreate(globalArgs, repositoryArgs);
        executor.Collect();

        Assert.Throws<InvalidOperationException>(() => executor.Collect());
    }

    [Fact]
    public void Wait_Then_Collect_Raises()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var executor = Lore.RepositoryCreate(globalArgs, repositoryArgs);
        executor.Wait();

        Assert.Throws<InvalidOperationException>(() => executor.Collect());
    }

    [Fact]
    public async Task Double_AsyncIter_Raises()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = Guid.NewGuid().ToString() };

        var executor = Lore.RepositoryCreate(globalArgs, repositoryArgs);
        await foreach (var _ in executor.AsyncIter()) { }

        // AsyncIter is an async iterator — the exception is thrown when enumeration begins
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in executor.AsyncIter()) { }
        });
    }

    [Fact]
    public async Task Wait_Then_AsyncIter_Raises()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = Guid.NewGuid().ToString() };

        var executor = Lore.RepositoryCreate(globalArgs, repositoryArgs);
        executor.Wait();

        // AsyncIter is an async iterator — the exception is thrown when enumeration begins
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in executor.AsyncIter()) { }
        });
    }

    [Fact]
    public async Task AsyncIter_Then_Wait_Raises()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = Guid.NewGuid().ToString() };

        var executor = Lore.RepositoryCreate(globalArgs, repositoryArgs);
        await foreach (var _ in executor.AsyncIter()) { }

        Assert.Throws<InvalidOperationException>(() => executor.Wait());
    }

    // --- Behavioral tests ---

    [Fact]
    public void Cold_Handle_No_Execution_Until_Wait()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var callbackCalled = new List<bool>();

        var executor = Lore.RepositoryCreate(globalArgs, repositoryArgs)
            .Callback((LoreEvent, userContext) => { callbackCalled.Add(true); });

        Assert.Empty(callbackCalled);

        executor.Wait();
        Assert.NotEmpty(callbackCalled);
    }

    [Fact]
    public void Method_Chaining_Works()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var callbackContexts = new List<ulong>();

        var result = Lore.RepositoryCreate(globalArgs, repositoryArgs)
            .Callback((LoreEvent, userContext) => { callbackContexts.Add(userContext); })
            .FilterByType([LoreEventTag.COMPLETE, LoreEventTag.END])
            .UserContext(42)
            .Wait();

        Assert.Equal(0, result);
        Assert.All(callbackContexts, ctx => Assert.Equal(42ul, ctx));
    }

    [Fact]
    public void Collect_With_Filter()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var events = Lore.RepositoryCreate(globalArgs, repositoryArgs)
            .FilterByType([LoreEventTag.COMPLETE, LoreEventTag.END])
            .Collect();

        Assert.Equal(2, events.Count);
        Assert.Contains(events, e => e is LoreCompleteEventData);
        Assert.Contains(events, e => e is LoreEndEventData);
    }

    [Fact]
    public void Collect_Event_Data_Accessible_Outside_Callback()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var events = Lore.RepositoryCreate(globalArgs, repositoryArgs).Collect();

        var completeEvents = events.OfType<LoreCompleteEventData>().ToList();
        Assert.Single(completeEvents);
        Assert.Equal(0, completeEvents[0].Status);

        var endEvents = events.OfType<LoreEndEventData>().ToList();
        Assert.Single(endEvents);
    }

    [Fact]
    public async Task AsyncIter_Event_Data_Accessible_Outside()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var events = new List<LoreEvent>();
        await foreach (var ev in Lore.RepositoryCreate(globalArgs, repositoryArgs).AsyncIter())
        {
            events.Add(ev);
        }

        var completeEvents = events.OfType<LoreCompleteEventData>().ToList();
        Assert.Single(completeEvents);
        Assert.Equal(0, completeEvents[0].Status);

        var endEvents = events.OfType<LoreEndEventData>().ToList();
        Assert.Single(endEvents);
    }

    [Fact]
    public async Task AsyncIter_Break_Early()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        LoreEvent? firstEvent = null;
        await foreach (var ev in Lore.RepositoryCreate(globalArgs, repositoryArgs).AsyncIter())
        {
            firstEvent = ev;
            break;
        }

        Assert.NotNull(firstEvent);
    }

    [Fact]
    public void Multiple_GlobalCallbacks_Same_Type()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        const ulong createContext = 7300001;

        var logEventsA = 0;
        var logEventsB = 0;

        using var unsubA = Lore.GlobalCallback(LoreEventTag.LOG,
            (loreEvent, userContext) =>
            {
                if (userContext == createContext)
                {
                    Interlocked.Increment(ref logEventsA);
                }
            });
        using var unsubB = Lore.GlobalCallback(LoreEventTag.LOG,
            (loreEvent, userContext) =>
            {
                if (userContext == createContext)
                {
                    Interlocked.Increment(ref logEventsB);
                }
            });

        Lore.RepositoryCreate(globalArgs, repositoryArgs)
            .UserContext(createContext)
            .Wait();

        var countA = 0;
        var countB = 0;
        var converged = SpinWait.SpinUntil(
            () =>
            {
                countA = Volatile.Read(ref logEventsA);
                countB = Volatile.Read(ref logEventsB);
                return countA != 0 && countA == countB;
            },
            TimeSpan.FromSeconds(2));

        Assert.True(converged, $"counts did not converge: A={countA}, B={countB}");
        Assert.NotEqual(0, countA);
        Assert.Equal(countA, countB);
    }

    [Fact]
    public void GlobalCallback_Ignores_PerCall_Filter()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        const ulong createContext = 7200001;

        var logEvents = 0;

        using var unsub = Lore.GlobalCallback(LoreEventTag.LOG,
            (loreEvent, userContext) =>
            {
                if (userContext == createContext)
                {
                    Interlocked.Increment(ref logEvents);
                }
            });

        Lore.RepositoryCreate(globalArgs, repositoryArgs)
            .FilterByType([LoreEventTag.COMPLETE])
            .UserContext(createContext)
            .Wait();

        Assert.NotEqual(0, Volatile.Read(ref logEvents));
    }

    [Fact]
    public void Wait_Without_Callback_Succeeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var result = Lore.RepositoryCreate(globalArgs, repositoryArgs).Wait();
        Assert.Equal(0, result);
    }

    [Fact]
    public void Complete_And_End_Events_Emitted_For_All_Methods()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // wait + callback (fresh path: re-creating a repository at an existing
        // path now throws a LoreError)
        var waitArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = Path.Combine(tempDir, "wait") };
        var waitEvents = new List<LoreEvent>();
        var repoUrl1 = Guid.NewGuid().ToString();
        Lore.RepositoryCreate(waitArgs, new LoreRepositoryCreateArgs { RepositoryUrl = repoUrl1 })
            .Callback((loreEvent, userContext) => { waitEvents.Add(loreEvent.Clone()); })
            .Wait();

        Assert.Contains(waitEvents, e => e is LoreCompleteEventData);
        Assert.Contains(waitEvents, e => e is LoreEndEventData);

        // collect
        var collectArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = Path.Combine(tempDir, "collect") };
        var repoUrl2 = Guid.NewGuid().ToString();
        var collectEvents = Lore.RepositoryCreate(collectArgs, new LoreRepositoryCreateArgs { RepositoryUrl = repoUrl2 })
            .Collect();

        Assert.Contains(collectEvents, e => e is LoreCompleteEventData);
        Assert.Contains(collectEvents, e => e is LoreEndEventData);

        // waitAsync + callback
        var waitAsyncArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = Path.Combine(tempDir, "waitAsync") };
        var waitAsyncEvents = new ConcurrentQueue<LoreEvent>();
        var repoUrl3 = Guid.NewGuid().ToString();
        Lore.RepositoryCreate(waitAsyncArgs, new LoreRepositoryCreateArgs { RepositoryUrl = repoUrl3 })
            .Callback((loreEvent, userContext) => { waitAsyncEvents.Enqueue(loreEvent.Clone()); })
            .WaitAsync()
            .GetAwaiter()
            .GetResult();

        Assert.Contains(waitAsyncEvents, e => e is LoreCompleteEventData);
        Assert.Contains(waitAsyncEvents, e => e is LoreEndEventData);

        // collectAsync
        var collectAsyncArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = Path.Combine(tempDir, "collectAsync") };
        var repoUrl4 = Guid.NewGuid().ToString();
        var collectAsyncEvents = Lore.RepositoryCreate(collectAsyncArgs, new LoreRepositoryCreateArgs { RepositoryUrl = repoUrl4 })
            .CollectAsync()
            .GetAwaiter()
            .GetResult();

        Assert.Contains(collectAsyncEvents, e => e is LoreCompleteEventData);
        Assert.Contains(collectAsyncEvents, e => e is LoreEndEventData);
    }

    // --- END-before-return tests ---

    // END is the last event lorelib dispatches, and the only reliable
    // terminator: a post-command task can still land a LOG event behind
    // COMPLETE. The async terminals must not resume before it.

    [Fact]
    public async Task WaitAsync_Resumes_Only_After_End()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var tags = new ConcurrentQueue<LoreEventTag>();
        await Lore.RepositoryCreate(globalArgs, repositoryArgs)
            .Callback((loreEvent, userContext) => { tags.Enqueue(loreEvent.Tag); })
            .WaitAsync();

        Assert.Equal(LoreEventTag.END, tags.Last());
    }

    [Fact]
    public async Task CollectAsync_Includes_End_Event()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var events = await Lore.RepositoryCreate(globalArgs, repositoryArgs).CollectAsync();

        Assert.IsType<LoreEndEventData>(events.Last());
    }

    // The task must not be completed in a way that lets the awaiting
    // continuation run on the lorelib worker thread that dispatched the event:
    // that thread runs nothing but the callback, and a synchronous Lore call
    // from it would block a thread the native runtime is driving tasks on.
    [Fact]
    public async Task Async_Continuation_Does_Not_Run_On_Callback_Thread()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        // Task.Run escapes xunit's synchronization context, which would
        // otherwise post the continuation elsewhere and hide the problem.
        await Task.Run(async () =>
        {
            var callbackThreadIds = new ConcurrentQueue<int>();
            await Lore.RepositoryCreate(globalArgs, repositoryArgs)
                .Callback((loreEvent, userContext) =>
                {
                    callbackThreadIds.Enqueue(Environment.CurrentManagedThreadId);
                })
                .WaitAsync();

            Assert.DoesNotContain(Environment.CurrentManagedThreadId, callbackThreadIds);
        });
    }

    // A synchronous Lore call is the operation that would fail outright if the
    // continuation had resumed on a lorelib worker thread.
    [Fact]
    public async Task Sync_Call_After_Await_Succeeds()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        await Task.Run(async () =>
        {
            var createArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
            await Lore.RepositoryCreate(createArgs, new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl })
                .WaitAsync();

            var statusArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
            var result = Lore.RepositoryStatus(statusArgs, new LoreRepositoryStatusArgs()).Wait();

            Assert.Equal(0, result);
        });
    }

    // NotificationSubscribe resolves its task on COMPLETE rather than END,
    // because a live subscription keeps the callback registered and only
    // dispatches END on unsubscribe. Offline it fails before any subscription
    // exists, so this covers that failure path rather than the early return;
    // exercising a live subscription needs a notification server.
    [Fact]
    public async Task NotificationSubscribe_Async_Offline_Fails_Without_Hanging()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        Lore.RepositoryCreate(globalArgs, new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl }).Wait();

        var subscribe = Lore.NotificationSubscribe(globalArgs, new LoreNotificationSubscribeArgs()).WaitAsync();
        var completed = await Task.WhenAny(subscribe, Task.Delay(TimeSpan.FromSeconds(30)));

        Assert.Same(subscribe, completed);
        var error = await Assert.ThrowsAsync<LoreError>(() => subscribe);
        Assert.NotEqual(0, error.ReturnCode);
    }

    // --- Callback exception tests ---

    // An exception thrown by a user callback must not unwind into the native
    // callback frame; it surfaces on the calling thread instead.

    [Fact]
    public void Wait_Rethrows_Callback_Exception()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var error = Assert.Throws<InvalidOperationException>(
            () => Lore.RepositoryCreate(globalArgs, repositoryArgs)
                .Callback((loreEvent, userContext) => throw new InvalidOperationException("callback boom"))
                .Wait()
        );
        Assert.Equal("callback boom", error.Message);
    }

    [Fact]
    public async Task WaitAsync_Rethrows_Callback_Exception()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Lore.RepositoryCreate(globalArgs, repositoryArgs)
                .Callback((loreEvent, userContext) => throw new InvalidOperationException("callback boom"))
                .WaitAsync()
        );
        Assert.Equal("callback boom", error.Message);
    }

    [Fact]
    public async Task AsyncIter_Rethrows_Callback_Exception_And_Terminates()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        var globalArgs = new LoreGlobalArgs { Offline = true, RepositoryPath = tempDir };
        var repositoryArgs = new LoreRepositoryCreateArgs { RepositoryUrl = repositoryUrl };

        // A throwing callback must still let the enumerator finish: END
        // completes the channel regardless.
        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var ev in Lore.RepositoryCreate(globalArgs, repositoryArgs)
                .Callback((loreEvent, userContext) => throw new InvalidOperationException("callback boom"))
                .AsyncIter())
            {
            }
        });
        Assert.Equal("callback boom", error.Message);
    }

    [Fact]
    public async Task Multiple_Parallel_Calls()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        const int numCalls = 50;
        var tasks = new List<Task<int>>();

        for (int i = 0; i < numCalls; i++)
        {
            var repoId = Guid.NewGuid().ToString();
            var globalArgs = new LoreGlobalArgs
            {
                Offline = true,
                RepositoryPath = Path.Combine(tempDir, $"repo-{i}")
            };
            var repositoryArgs = new LoreRepositoryCreateArgs
            {
                RepositoryUrl = repoId
            };
            tasks.Add(Lore.RepositoryCreate(globalArgs, repositoryArgs).WaitAsync());
        }

        var results = await Task.WhenAll(tasks);
        Assert.Equal(numCalls, results.Length);
        Assert.All(results, r => Assert.Equal(0, r));
    }

    private static readonly byte[] StoragePartition = Enumerable.Repeat((byte)0x11, 16).ToArray();
    private static readonly byte[] StorageContext = Enumerable.Repeat((byte)0x22, 16).ToArray();

    private static ulong OpenInMemoryStoreFluent(LoreGlobalArgs globalArgs)
    {
        ulong handleId = 0;
        var openArgs = new LoreStorageOpenArgs { RepositoryPath = string.Empty, InMemory = true };

        var result = Lore.StorageOpen(globalArgs, openArgs)
            .Callback((LoreEventFFI loreEvent, ulong _) =>
            {
                if (loreEvent.Tag == LoreEventTag.STORAGE_OPENED)
                {
                    handleId = loreEvent.GetData<LoreStorageOpenedEventDataFFI>().HandleId;
                }
            })
            .Wait();

        Assert.Equal(0, result);
        Assert.NotEqual((ulong)0, handleId);
        return handleId;
    }

    [Fact]
    public void Storage_Open_Close_Fluent_Works()
    {
        var globalArgs = new LoreGlobalArgs { Offline = true };

        var handleId = OpenInMemoryStoreFluent(globalArgs);

        var closeArgs = new LoreStorageCloseArgs { Handle = new LoreStore(handleId) };
        var closeResult = Lore.StorageClose(globalArgs, closeArgs).Wait();

        Assert.Equal(0, closeResult);
    }

    [Fact]
    public void Storage_Put_Get_Fluent_Works()
    {
        var globalArgs = new LoreGlobalArgs { Offline = true };
        var handleId = OpenInMemoryStoreFluent(globalArgs);
        try
        {
            var payload = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();

            using var putArgs = new LoreStoragePutArgs
            {
                Handle = new LoreStore(handleId),
                Items = new[]
                {
                    new LoreStoragePutItem
                    {
                        Id = 1UL,
                        Partition = new LorePartition(StoragePartition),
                        Context = new LoreContext(StorageContext),
                        Data = payload,
                    }
                }
            };

            var putEvents = Lore.StoragePut(globalArgs, putArgs)
                .FilterByType([LoreEventTag.STORAGE_PUT_ITEM_COMPLETE])
                .Collect()
                .OfType<LoreStoragePutItemCompleteEventData>()
                .ToList();

            Assert.Single(putEvents);
            Assert.Equal(LoreErrorCode.NONE, putEvents[0].ErrorCode);
            var putAddress = putEvents[0].Address;

            using var getArgs = new LoreStorageGetArgs
            {
                Handle = new LoreStore(handleId),
                Items = new[]
                {
                    new LoreStorageGetItem
                    {
                        Id = 1UL,
                        Partition = new LorePartition(StoragePartition),
                        Address = putAddress,
                    }
                }
            };

            var getEvents = Lore.StorageGet(globalArgs, getArgs)
                .FilterByType([LoreEventTag.STORAGE_GET_DATA, LoreEventTag.STORAGE_GET_ITEM_COMPLETE])
                .Collect();

            var receivedBytes = getEvents
                .OfType<LoreStorageGetDataEventData>()
                .SelectMany(e => e.Bytes)
                .ToArray();
            var completes = getEvents.OfType<LoreStorageGetItemCompleteEventData>().ToList();

            Assert.Single(completes);
            Assert.Equal(LoreErrorCode.NONE, completes[0].ErrorCode);
            Assert.Equal(payload, receivedBytes);
        }
        finally
        {
            var closeArgs = new LoreStorageCloseArgs { Handle = new LoreStore(handleId) };
            Lore.StorageClose(globalArgs, closeArgs).Wait();
        }
    }

    [Fact]
    public async Task Storage_Open_Close_Fluent_WaitAsync_Works()
    {
        var globalArgs = new LoreGlobalArgs { Offline = true };

        ulong handleId = 0;
        var openArgs = new LoreStorageOpenArgs { RepositoryPath = string.Empty, InMemory = true };

        var openResult = await Lore.StorageOpen(globalArgs, openArgs)
            .Callback((LoreEventFFI loreEvent, ulong _) =>
            {
                if (loreEvent.Tag == LoreEventTag.STORAGE_OPENED)
                {
                    handleId = loreEvent.GetData<LoreStorageOpenedEventDataFFI>().HandleId;
                }
            })
            .WaitAsync();

        Assert.Equal(0, openResult);
        Assert.NotEqual((ulong)0, handleId);

        var closeArgs = new LoreStorageCloseArgs { Handle = new LoreStore(handleId) };
        var closeResult = await Lore.StorageClose(globalArgs, closeArgs).WaitAsync();
        Assert.Equal(0, closeResult);
    }

    [Fact]
    public async Task Storage_Put_Get_Fluent_AsyncIter_Works()
    {
        var globalArgs = new LoreGlobalArgs { Offline = true };
        var handleId = OpenInMemoryStoreFluent(globalArgs);
        try
        {
            var payload = Enumerable.Range(0, 64).Select(i => (byte)i).ToArray();

            using var putArgs = new LoreStoragePutArgs
            {
                Handle = new LoreStore(handleId),
                Items = new[]
                {
                    new LoreStoragePutItem
                    {
                        Id = 1UL,
                        Partition = new LorePartition(StoragePartition),
                        Context = new LoreContext(StorageContext),
                        Data = payload,
                    }
                }
            };

            LoreAddress? putAddress = null;
            await foreach (var ev in Lore.StoragePut(globalArgs, putArgs).AsyncIter())
            {
                if (ev is LoreStoragePutItemCompleteEventData putComplete)
                {
                    Assert.Equal(LoreErrorCode.NONE, putComplete.ErrorCode);
                    putAddress = putComplete.Address;
                }
            }
            Assert.NotNull(putAddress);

            using var getArgs = new LoreStorageGetArgs
            {
                Handle = new LoreStore(handleId),
                Items = new[]
                {
                    new LoreStorageGetItem
                    {
                        Id = 1UL,
                        Partition = new LorePartition(StoragePartition),
                        Address = putAddress.Value,
                    }
                }
            };

            var receivedBytes = new List<byte>();
            var completes = 0;
            await foreach (var ev in Lore.StorageGet(globalArgs, getArgs).AsyncIter())
            {
                if (ev is LoreStorageGetDataEventData data)
                {
                    receivedBytes.AddRange(data.Bytes);
                }
                else if (ev is LoreStorageGetItemCompleteEventData getComplete)
                {
                    Assert.Equal(LoreErrorCode.NONE, getComplete.ErrorCode);
                    completes++;
                }
            }

            Assert.Equal(1, completes);
            Assert.Equal(payload, receivedBytes.ToArray());
        }
        finally
        {
            var closeArgs = new LoreStorageCloseArgs { Handle = new LoreStore(handleId) };
            await Lore.StorageClose(globalArgs, closeArgs).WaitAsync();
        }
    }
}
