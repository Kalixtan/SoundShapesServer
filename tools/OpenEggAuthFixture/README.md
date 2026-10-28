# OpenEgg authentication fixture

Run against a new, empty local data directory:

```sh
dotnet run --project tools/OpenEggAuthFixture -- /tmp/openegg-server-fixture
BUNKUM_DATA_FOLDER=/tmp/openegg-server-fixture dotnet run --project SoundShapesServer
```

The first command prints a test user ID, a one-hour session ID, and a target
user ID. Use them with OpenEgg's `tests/networking/run_probe.py --user-id ID
--session-id ID --target-id ID`.
Use a fresh directory for each run. This seeds a local test token directly;
it does not test retail ticket login.
