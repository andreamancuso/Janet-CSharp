using Xunit;

// Janet is process-global and single-threaded. Tests must run sequentially.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
