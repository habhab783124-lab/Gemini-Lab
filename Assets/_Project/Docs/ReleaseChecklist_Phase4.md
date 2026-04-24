# Release Checklist - Phase 4

## Functional Gates
- [ ] Chat panel sends player message and receives `ChatChunk`/`ChatDone`.
- [ ] Status panel reflects pet FSM transitions and work result.
- [ ] Inventory panel can display current furniture build palette.
- [ ] Apartment/Overlay toggle works without service reset.
- [ ] Travel flow supports depart -> complete/fail -> UI update.

## Quality Gates
- [ ] EditMode tests pass.
- [ ] PlayMode smoke tests pass.
- [ ] Gateway logs keep `traceId` and avoid raw sensitive payload exposure.

## Performance Gates
- [ ] Overlay mode frame rate remains stable for 10-minute idle run.
- [ ] No unbounded timeline/event memory growth (travel timeline capped).

## Packaging Gates
- [ ] Windows build generated from CI.
- [ ] Artifact name/version matches release notes.
- [ ] Smoke checklist attached to release PR.
