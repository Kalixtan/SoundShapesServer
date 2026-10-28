# Replacement server parity worklist

The server is known to work with retail for exercised flows. That is useful
compatibility evidence, not proof that its implementation matches the PS3
engine or the full retail service contract. Some behavior was reconstructed
from captures and may encode guesses. Treat retail traces and controlled
retail observations as the authority; record server-only assumptions.

## Current evidence

- [x] Local server starts and accepts OpenEgg requests.
- [x] `GET /otg/~index:level.page?type=level&search=tagged3` returns HTTP 200
      JSON to OpenEgg.
- [x] The same route with `search=all` and no session returns HTTP 403.
- [x] Album, following, followers, and activity page URLs reach protected
      handlers and return HTTP 403 without a session.
- [x] `POST /otg/identity/login/token/psn.post` with an invalid ticket reaches
      the login handler and returns HTTP 400.
- [ ] No authenticated OpenEgg request has been verified yet.
- [ ] The featured-level probe uses a nonexistent placeholder identity and
      returns 404; confirm handler routing with a real test user before drawing
      conclusions from that status.

## Retail contract matrix

- [ ] For every implemented client API, record retail route construction,
      HTTP method, query encoding, headers, body bytes, and accepted statuses.
- [ ] Compare server route, auth policy, status, response shape, and side
      effects against that matrix; label observed facts separately from guesses.
- [ ] Verify `PAGE`, `ALL`, `GET`, `PUT`, `DELETE`, and `POST` operation suffix
      handling and the HTTP method actually used for each.
- [ ] Verify path escaping, empty and duplicate query fields, pagination,
      Unicode, malformed IDs, and boundary lengths.
- [ ] Confirm response JSON field names, types, null/empty behavior, ordering,
      and large integer representation against retail callbacks and scripts.

## Authentication and identity

- [ ] Trace login with retail ticket fixtures and compare success, invalid
      ticket, unauthorized account, account creation, and banned account cases.
- [ ] Verify session ID, user ID, token lifetime, refresh/relogin, and header
      handling against retail request traces.
- [ ] Verify EULA fetch, version/hash, acceptance, and the client state changes
      that follow each response.
- [ ] Verify unauthenticated versus authenticated policy per route; do not
      infer it from the endpoint name or a single successful capture.

## Endpoint behavior

- [ ] Check all registered game endpoints against the 69 Watermelon wrappers:
      levels, discovery, metadata, relations, records, leaderboards, activity,
      albums, identity, EULA, grief reports, and cloud saves.
- [ ] Check expected behavior for the 45 PSN/Store/Near `Network_*` APIs and
      13 `OnlineCoordinator_*` APIs; document service features the backend
      cannot represent.
- [ ] Exercise malformed and boundary requests, duplicate actions, missing
      resources, permission failures, and concurrent requests.
- [ ] Compare database side effects and event ordering with retail-visible
      results, not only HTTP status codes.

## Test method

- [ ] Build server fixtures for stable users, sessions, levels, albums, and
      relationships so tests reach successful authenticated handler paths.
- [ ] Add endpoint tests for every route and error branch; retain existing
      regression tests and run them with the parity suite.
- [ ] Save retail captures with game version, platform, setup, and input notes;
      replay equivalent requests against the server where possible.
- [ ] Keep ambiguous results unresolved until a retail trace, retail capture,
      or controlled fixture distinguishes them.
