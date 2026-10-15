Status: accepted, 15 Oct 2026.

Context: the schema had to stay ready without a network.

Decision: content ids, an append-only local op-log, `ISyncAdapter` empty, `SyncFeature.Enabled` false.

Consequences: pairing, transport, and encryption are not implemented.
