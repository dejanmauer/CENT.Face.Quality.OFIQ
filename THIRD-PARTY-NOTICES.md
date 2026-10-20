# Third-party notices

The root [MIT license](LICENSE) applies to the C# library, sample and tests supplied by CENT SI d.o.o. Third-party files retain the licenses described below.

## OFIQ 1.2.0

Source: [BSI-OFIQ/OFIQ-Project](https://github.com/BSI-OFIQ/OFIQ-Project/tree/v1.2.0), revision `d9beef8220c3829661d11e2aa3a96aef9a94a5f3`.

Copyright (c) 2024 Federal Office for Information Security, Germany. OFIQ uses the MIT license; its dependency table and full license are retained in [OFIQ-LICENSE.md](OFIQ-LICENSE.md). The C# native declarations and numeric identifiers follow OFIQ's public C headers. The native DLLs and config are third-party OFIQ artifacts.

## Native dependencies

The bundle includes ONNX Runtime 1.18.1 CPU, copyright Microsoft Corporation, MIT license. Its full license and bundled third-party notices are in [runtime/ofiq/licenses/onnxruntime](runtime/ofiq/licenses/onnxruntime).

The OFIQ build uses OpenCV 4.5.5 (Apache 2.0), gzip-hpp, magic_enum, taocpp-json, taocpp-pegtl, and native image/serialization dependencies. License files collected from the actual Conan packages used by the build are retained under [runtime/ofiq/licenses](runtime/ofiq/licenses): `opencv`, `gzip-hpp`, `magic_enum`, `taocpp-json`, `taocpp-pegtl`, `libjpeg-turbo`, `libpng`, `openjpeg`, `protobuf`, `zlib`, and `gtest` (the build's test dependency). Embedded components may carry additional notices inside those license files.

The Microsoft Visual C++ redistributable is installed separately from Microsoft; its installer is not bundled.

## Model data

The supplied models come from the official OFIQ model archive referenced by [upstream](https://github.com/BSI-OFIQ/OFIQ-Project/blob/v1.2.0/data/models/README.md). Their accompanying `LICENSE.md` files are retained next to the model data under [runtime/ofiq/models](runtime/ofiq/models). Those notices identify MIT and Apache 2.0 terms, describe upstream model conversions, and credit the OFIQ team and original projects.

Full original project license texts are additionally retained in [runtime/ofiq/licenses/models](runtime/ofiq/licenses/models); `SOURCE.txt` records the retrieved source and license blob identifier for each copy:

| Model family | Original project | License identified by OFIQ |
| --- | --- | --- |
| Expression neutrality, Grimmer | [Efficient-Expression-Neutrality-Estimation](https://github.com/dasec/Efficient-Expression-Neutrality-Estimation) | MIT |
| Expression neutrality, HSEMotion | [face-emotion-recognition](https://github.com/av-savchenko/face-emotion-recognition) | Apache 2.0 |
| Face detector | [face-detection-with-OpenCV-and-DNN](https://github.com/sr6033/face-detection-with-OpenCV-and-DNN) | MIT |
| ADNet landmarks | [ADNet](https://github.com/huangyangyu/ADNet) | MIT |
| Occlusion segmentation | [FaceExtraction](https://github.com/face3d0725/FaceExtraction) | MIT |
| Face parsing | [face-parsing.PyTorch](https://github.com/zllrunning/face-parsing.PyTorch) | MIT |
| Head pose | [3DDFA_V2](https://github.com/cleardusk/3DDFA_V2) | MIT |
| Unified quality score | [MagFace](https://github.com/IrvingMeng/MagFace) | Apache 2.0 |
| Sharpness and compression artifacts | OFIQ development team | MIT |

Redistribution must preserve the applicable licenses and notices. CENT's MIT license does not replace third-party model or binary terms.

## Test portraits

NASA is the source of the public test portraits. See [test-images/README.md](test-images/README.md) for image identifiers, download URLs, public-domain basis and checksums. Image inclusion is for factual software testing, with no implication of endorsement.
