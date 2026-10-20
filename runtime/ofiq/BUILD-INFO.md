# Bundled Windows x64 runtime

- OFIQ: **1.2.0**, upstream tag `v1.2.0`, source revision `d9beef8220c3829661d11e2aa3a96aef9a94a5f3`.
- Source: <https://github.com/BSI-OFIQ/OFIQ-Project/tree/v1.2.0>.
- Build: Release x64, Visual Studio 2026 / MSVC 195, Conan 2.18.1, `USE_CONAN=ON`.
- Native DLL build recorded on 17 October 2026; copied into this standalone distribution on 20 October 2026. The C API DLL's hash was recorded on 19 October 2026.
- ONNX Runtime: Microsoft **1.18.1 CPU**, official Windows x64 archive. Original version, commit ID, license and third-party notices are in `licenses/onnxruntime/`.
- Config: unmodified upstream `data/ofiq_config.jaxn`.
- Models: official OFIQ model archive, with accompanying licenses retained. Model provenance and original projects are described in each model directory's `LICENSE.md`.
- SHA-256 hashes of the actual bundled DLLs, configuration and model data: [SHA256SUMS.txt](SHA256SUMS.txt).

Direct Conan requirements: `gtest/1.14.0`, `opencv/4.5.5`, `taocpp-json/1.0.0-beta.13`, `magic_enum/0.8.1`, `gzip-hpp/0.1.0`. Native dependency notices were collected from the build's Conan package directories. The bundled C API, core library and ONNX Runtime DLL hashes match the recorded OFIQ build.

These binaries and models are provided under their respective upstream licenses, described in the root [THIRD-PARTY-NOTICES.md](../../THIRD-PARTY-NOTICES.md), rather than the CENT C# software license. Preserve this directory's licenses and the notices beside model files when redistributing it.
