Status: accepted, 15 Oct 2026.

Context: installer size was being treated as a reason to ship weights.

Decision: weights download into app data, SHA-256 checked, int8 text files named in appsettings. Float16 is forbidden.

Consequences: the package must not contain `.onnx` weights.
