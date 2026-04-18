using Xunit;

// Several test classes capture process-global stdout state via PerceptionEmitterCapture
// (which redirects Console.Out and reflection-resets PerceptionEmitter._realStdout).
// xUnit's default parallel-collections execution races those captures across classes,
// producing intermittent "response never emitted" failures in InteractionHandlersTests
// and CommandDispatcherTests. Serialise the whole assembly to make the suite deterministic.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
