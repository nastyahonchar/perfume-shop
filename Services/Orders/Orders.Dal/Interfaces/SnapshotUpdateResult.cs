namespace Orders.Dal.Interfaces;

public enum SnapshotUpdateResult
{
    Updated,
    Stale,
    NotFound
}