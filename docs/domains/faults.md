Status: in-code

## Owns

Faults owns `RecallFault` and `RecallFaultException`. `VectorSchemaStale` is 20. Opening a text collection that has no `fts_key` vector replaces that collection instead of throwing this fault. The value stays reserved.

## Does not own

It does not choose the policy that throws.
