Status: in-code

## In words

A fault is a named product failure, such as a width of 128, a text width other than 512, a prefix applied twice, or a fusion rank other than 60. Callers catch `RecallFaultException` instead of inventing a second error code. The fault value `VectorSchemaStale` stays reserved. A text collection that is missing its keyword field is replaced on open. That replacement does not throw this fault.

## Owns

Faults owns `RecallFault` and `RecallFaultException`. `VectorSchemaStale` is 20. `AgeCutTextForbidden` is 21 when age shrink or the recall gate is invoked on text. Opening a text collection whose loaded schema has no `fts_key` field replaces that collection instead of throwing `VectorSchemaStale`. That value stays reserved.

## Does not own

It does not choose the policy that throws.
