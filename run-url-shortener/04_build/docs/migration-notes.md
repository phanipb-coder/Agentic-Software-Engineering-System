# Migration and Rollback Notes

The demo has no destructive migrations. `JsonFilePersistenceStore` can be replaced additively by implementing `IPersistenceStore` and changing dependency registration.

For production SQL adoption:
1. Create `ShortLinks` and `ClickEvents` tables with a unique index on `Code`.
2. Backfill JSON data through a one-time import job.
3. Deploy the new provider behind the same interface.
4. Keep the JSON file as a read-only rollback snapshot until verification completes.
5. Rollback by switching provider configuration; do not drop tables during rollout.
