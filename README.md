# CENT.Face.Quality.OFIQ

A small C# library for face image quality assessment using the [OFIQ](https://github.com/BSI-OFIQ/OFIQ-Project) 1.2.0 C API. It returns the unified quality score, individual quality measures, face bounding boxes, and optional facial landmarks.

**Provider:** CENT SI d.o.o.

**Author:** Dejan Mauer

**C# code license:** [MIT](LICENSE)

The repository includes a console sample, two public NASA test portraits, and a ready-to-use Windows x64 native runtime with models. Large runtime files are stored in **Git LFS**. This is an independent C# wrapper; OFIQ is developed by BSI and its contributors.

## Requirements

- Windows x64 for native assessment.
- [Visual Studio 2026, version 18.0 or newer](https://learn.microsoft.com/en-us/dotnet/core/install/windows) with the **.NET desktop development** workload. In Visual Studio Installer, select this workload and ensure the **.NET 10 SDK** is installed. This is the C# workload used by the console sample and tests.
- Alternatively, the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and a terminal.
- [Git for Windows](https://git-scm.com/downloads/win) and [Git LFS](https://git-lfs.com/).
- The latest [Microsoft Visual C++ v14 Redistributable, x64](https://aka.ms/vc14/vc_redist.x64.exe), required by the bundled native DLLs.

The C++ development workload is needed only to rebuild OFIQ itself. Running the included native runtime needs the C# workload and the redistributable above. Model loading requires several hundred MB of memory and takes a few seconds; reuse an engine for multiple images.

The library targets **.NET Standard 2.0**, has no additional managed package dependencies beyond that framework, and can be referenced by compatible .NET and .NET Framework applications. Native assessment still requires a Windows x64 host. The console sample and tests target **.NET 10**.

## Clone and run

Clone the GitHub repository using its HTTPS or SSH address, then run these commands from its root:

```powershell
git lfs install
git lfs pull
dotnet restore CENT.Face.Quality.OFIQ.sln
dotnet build CENT.Face.Quality.OFIQ.sln -c Release
dotnet run --project samples/CENT.Face.Quality.OFIQ.Sample -c Release -- --runtime runtime/ofiq test-images/eileen-collins.jpg
```

The runtime and models occupy approximately **470 MB**. A GitHub source ZIP may contain LFS pointer files instead of models; cloning and running `git lfs pull` is recommended. The file `runtime/ofiq/models/unified_quality_score/magface_iresnet50_norm.onnx` should be 174,394,035 bytes, not a small text pointer.

Assess multiple portraits and include landmarks:

```powershell
dotnet run --project samples/CENT.Face.Quality.OFIQ.Sample -c Release -- --runtime runtime/ofiq --landmarks test-images/eileen-collins.jpg test-images/michael-collins.jpg
```

You can also set an absolute runtime location for the current terminal:

```powershell
$env:OFIQ_RUNTIME_DIR = (Resolve-Path runtime/ofiq).Path
dotnet run --project samples/CENT.Face.Quality.OFIQ.Sample -c Release -- test-images/eileen-collins.jpg
```

The sample prints the native version, score, face box, per-measure raw/scalar values and status. Exit codes are `0` for success, `1` for setup or assessment failure, and `2` for invalid arguments. Use `--help` for usage. A completed assessment can contain individual measures that failed; inspect their status before using their scores.

## Run in Visual Studio

1. Install the prerequisites above, clone the repository, and run `git lfs pull` in **View > Terminal** from the repository root.
2. Open `CENT.Face.Quality.OFIQ.sln`. Allow NuGet restore to finish.
3. In Solution Explorer, right-click **CENT.Face.Quality.OFIQ.Sample** and choose **Set as Startup Project**.
4. Open the sample project's **Properties > Debug > Open debug launch profiles UI**. Set **Working directory** to the absolute folder containing `CENT.Face.Quality.OFIQ.sln`.
5. Set **Command line arguments** to:

   ```text
   --runtime runtime/ofiq --landmarks test-images/eileen-collins.jpg
   ```

6. Build the solution and press **Ctrl+F5** to run, or **F5** to debug. The sample explicitly targets **x64**, including when the solution configuration is called Any CPU.

For your own C# application, add a project reference to `src/CENT.Face.Quality.OFIQ/CENT.Face.Quality.OFIQ.csproj`, set the host application's **Platform target** to **x64**, and supply the runtime folder to `OfiqEngine.Create`. Runtime files are kept separately; a project reference or NuGet package does not copy the models into your application's output directory.

## C# usage

```csharp
using System;
using System.IO;
using CENT.Face.Quality.OFIQ;

using var engine = OfiqEngine.Create(@"C:\ofiq-runtime");
var result = engine.Assess(File.ReadAllBytes("portrait.jpg"), includeLandmarks: true);

Console.WriteLine($"Unified quality: {result.UnifiedQualityScore:0.##} / 100");
Console.WriteLine($"Face: {result.Face}");
Console.WriteLine($"Landmarks: {result.Landmarks.Count}");

var sharpness = result[OfiqMeasure.Sharpness];
if (sharpness?.Status == OfiqMeasureStatus.Success)
    Console.WriteLine($"Sharpness: {sharpness.Scalar:0.##}");
```

- `Assess(byte[])` decodes encoded JPEG, PNG, BMP, and other formats supported by the bundled OpenCV build.
- `Assess(OfiqImage)` accepts decoded **8-bit grayscale** or **24-bit BGR**, top-down rows with no padding. Buffer length must equal `width * height * (depth / 8)`.
- `ScalarQuality(byte[])` returns only the unified quality score.
- Successful scalar values range from 0 to 100; higher means better quality for that measure. Raw values use measure-specific units. `-1` represents an uncomputed unified score. Inspect each `OfiqMeasureResult.Status` before using a component score.
- `Assess(..., includeLandmarks: true)` additionally returns all detected face boxes and 98 landmarks for the assessed face. The normal call leaves these lists empty.
- Native failures throw `OfiqException`; `Code` contains the OFIQ return code. Invalid image data reports `ImageReadingError`; an image without a detectable face reports `QualityAssessmentError`.
- Keep an engine alive and dispose it when finished. Calls that use its native handle are serialized. Assessment is CPU-bound, so schedule it away from a UI thread. A process must use one native runtime directory.

See [Program.cs](samples/CENT.Face.Quality.OFIQ.Sample/Program.cs) for a complete runnable example and error handling.

## Native runtime and models

The tested bundle is **OFIQ 1.2.0** with **ONNX Runtime 1.18.1**, built for Windows x64. It includes:

```text
runtime/ofiq/
  ofiq_c.dll                C API used by this wrapper
  ofiq_lib.dll              OFIQ implementation
  onnxruntime.dll           ONNX Runtime CPU library
  ofiq_config.jaxn          Upstream configuration
  models/                  Model files and their license notices
  licenses/                Native dependency and original model licenses
  BUILD-INFO.md             Versions, source revision and build provenance
  SHA256SUMS.txt            SHA-256 hashes of the DLLs, config and model data
```

Pass this directory to `OfiqEngine.Create`, or copy the **entire directory** to a deployment location such as `C:\ofiq-runtime`. Model paths in `ofiq_config.jaxn` are relative to the configuration directory. Copy the notices along with the binaries and models.

If you obtain the runtime separately or want to rebuild it:

1. Get the [OFIQ 1.2.0 source](https://github.com/BSI-OFIQ/OFIQ-Project/tree/v1.2.0), revision `d9beef8220c3829661d11e2aa3a96aef9a94a5f3`, and follow its [Windows build instructions](https://github.com/BSI-OFIQ/OFIQ-Project/blob/v1.2.0/BUILD.md#windows). Building native code additionally requires the **Desktop development with C++** workload, Python 3.10+, CMake 3.26+, and Conan 2.18.1. The upstream script supports Visual Studio 2022 via `build.cmd --compiler 17`; its compiler selection is separate from the Visual Studio 2026 requirement for this C# sample.
2. Collect `ofiq_c.dll` and `ofiq_lib.dll` from `install_x86_64/Release/bin`, and `onnxruntime.dll` from the upstream ONNX Runtime Windows x64 package. Use the matching `data/ofiq_config.jaxn`. A C++-only runtime without **ofiq_c.dll** cannot be used by this wrapper.
3. Get the official [OFIQ model archive](https://standards.iso.org/iso-iec/29794/-5/ed-2/en/OFIQ-MODELS.zip), also referenced by the [upstream model documentation](https://github.com/BSI-OFIQ/OFIQ-Project/blob/v1.2.0/data/models/README.md). Extract it so `models/face_landmark_estimation/ADNet.onnx` and the other model subdirectories sit directly below your runtime folder. Avoid an extra nested `models/models` directory.
4. Preserve all model licenses and native dependency notices. Set `OFIQ_RUNTIME_DIR` to the resulting runtime directory and run the sample.

This wrapper binds the **1.2.0 ABI**; upgrading native DLLs requires checking the interop declarations and tests against the new C headers.

### Troubleshooting

| Symptom | Action |
| --- | --- |
| Missing DLL or Win32 error 126 | Install the latest x64 Visual C++ redistributable and keep all three DLLs together. |
| Missing models, tiny `.onnx` files, or initialization fails | Run `git lfs pull`; verify the complete `models/` tree and config belong to this runtime. |
| `ofiq_c.dll` missing | Use the OFIQ 1.2.0 C API build. A C++ library alone is insufficient. |
| 32-bit process or bad image format | Set the host application's platform target to x64. |
| Native paths cannot be marshalled | Use a runtime path representable in the Windows system code page; the OFIQ 1.2.0 C API takes narrow strings. |
| No face found | Try a clear portrait. A failed assessment raises `OfiqException`; it is not a quality score of zero. |

## Tests and packaging

```powershell
# ABI layout, managed input validation, and console argument tests.
# Native assessments are explicitly skipped unless OFIQ_RUNTIME_DIR is set.
dotnet test CENT.Face.Quality.OFIQ.sln -c Release

# Include native assessment tests using both public portraits.
$env:OFIQ_RUNTIME_DIR = (Resolve-Path runtime/ofiq).Path
dotnet test CENT.Face.Quality.OFIQ.sln -c Release

# Build a managed library package; native runtime/models remain separate.
dotnet pack src/CENT.Face.Quality.OFIQ -c Release -o artifacts
```

GitHub Actions builds and tests the managed projects without downloading LFS runtime files. Run the native tests locally before changing interop declarations or the bundled runtime. These smoke tests do not claim ISO conformance certification.

## Test images and licenses

The [test-images](test-images/README.md) folder contains two unmodified NASA portraits with source links and checksums. They are public-domain U.S. government imagery for informational testing, with NASA credited. They are ordinary sample images, not OFIQ conformance test fixtures.

The C# library and sample are MIT-licensed by **CENT SI d.o.o.**, author **Dejan Mauer**. OFIQ, ONNX Runtime, native dependencies and models retain their respective licenses; they are not relicensed under the CENT license. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md), [OFIQ-LICENSE.md](OFIQ-LICENSE.md), and the notices shipped in `runtime/ofiq`. No endorsement by NASA, the pictured people, BSI, ISO, IEC, or eu-LISA is implied.
