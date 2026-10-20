Status: in-code for the desktop benchmark row, decided-absent for the rest.

## Measured

Task 2.2.5 is done. The desktop line is recorded in `docs/plan.md`: `Host: desktop`, `InsertOne` mean 30.66 us, `HybridTop20` mean 2,202.43 us, `LoadTextEncoder` mean 727.3 ms. The desktop encoder line is 800 ms. The mobile 60 ms and 600 ms cells were never measurable in Phase 2 and stay on task A.2.9. A connected phone cannot run this benchmark project, and an emulator label is not a phone number. The benchmark still exits without a number when `RECALL_TEXT_MODEL_DIR` is unset. Task 7.1.2 crash restart and egress check are not started. Task 7.1.3 golden recall in English, Arabic, Chinese, Hindi, and French is not started. UI languages stay Arabic and English either way. Tasks 7.2.1 and 7.2.2 are not started. Crash reports stay on the device until U3 is decided.
