# DiveProtocol Testing

## Test layers

EditMode tests exercise pure runtime data, run lifecycle rules, reward calculation, JSON persistence, corruption recovery, and persistence boundaries without loading gameplay scenes. PlayMode tests load the generated system scenes and verify the persistent AppRoot and the Bootstrap → Main Menu → Demo Level → Results flow.

Run both suites from **Window → General → Test Runner**, selecting EditMode or PlayMode and choosing **Run All**. Command-line runs use Unity `6000.0.69f1` with `-runTests -testPlatform EditMode` or `-testPlatform PlayMode` and a `-testResults` path under the ignored `Logs` directory.

## Save isolation

Every save test creates a unique directory under the operating-system temporary directory and deletes it during teardown. PlayMode tests inject a test SaveManager before Bootstrap loads. Tests never read or write the real `Application.persistentDataPath` save.

RunState is process-local data and must never enter the JSON document. Persistence-boundary tests serialize MetaSaveData, reject forbidden run field names, create an active run, flush shutdown meta changes, and then reconstruct SaveManager and RunManager. Meta progression reloads while CurrentRun remains null, which verifies that an interrupted run cannot be continued.

## Important behavior tests

- Aborted runs clear CurrentRun, create no LastResult, add no processed RunId, grant no currency, and return directly to Main Menu in PlayMode.
- Eligible RunResults are applied once. A second application is rejected both before and after reconstructing SaveManager from disk.
- Player death settles score-derived currency without increasing SuccessfulRuns.
- Corrupt primary saves recover the backup; two corrupt files are preserved and replaced with defaults.

## Scene dependencies

PlayMode tests require `SCN_Bootstrap`, `SCN_MainMenu`, `SCN_DemoLevel`, and `SCN_Results` to be generated and present in Build Settings. If any scene is unavailable, scene tests use Ignore with a message instructing the developer to run Create System Scenes and Configure Build Scenes. Ignore is preferable to a misleading code failure when generated test fixtures are absent.

## Extending coverage

When adding permanent upgrades or unlocks, add tests for their default value, successful mutation, automatic save, round-trip load, migration/version behavior, and resistance to duplicate rewards. When adding any RunState field, update persistence-boundary tests to prove that the field and its data cannot appear in MetaSaveData JSON or be restored after a simulated process restart.
