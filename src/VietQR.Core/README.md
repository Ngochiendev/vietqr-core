# VietQR.Core

[![NuGet](https://img.shields.io/nuget/v/VietQR.Core?style=flat-square)](https://www.nuget.org/packages/VietQR.Core)
[![NuGet Downloads](https://img.shields.io/nuget/dt/VietQR.Core?style=flat-square)](https://www.nuget.org/packages/VietQR.Core)
[![.NET](https://img.shields.io/badge/.NET-net6.0%20%7C%20net8.0%20%7C%20net9.0%20%7C%20net10.0-512BD4?style=flat-square)](https://github.com/Ngochiendev/vietqr-core#supported-runtimes)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](https://github.com/Ngochiendev/vietqr-core/blob/main/LICENSE)

**VietQR.Core** generates and renders payment QR codes for Vietnam according to the **NAPAS247 / EMVCo** standard.

Given a bank BIN and an account number, the library produces a checksummed EMVCo payload (QRIBFTTA to account / QRIBFTTC to card), plus a rendered PNG card in three templates — using SkiaSharp, entirely offline. The full bank catalog (76+ banks) and all bank logos ship **embedded in the package**, so it works out of the box with no configuration or network access.

## Getting started

### Prerequisites

- Any .NET runtime listed in the **Supported runtimes** section below.
- No other setup required — assets, logo files and the bank catalog are embedded in the package.

### Install

```sh
dotnet add package VietQR.Core
```

> Pin to a specific release when needed:
> `dotnet add package VietQR.Core --version <x.y.z>`

`dotnet add` resolves the latest stable version and writes the exact
`PackageReference` (with version) into your `.csproj`. Prefer this over a
copy-pasted `PackageReference` element: a version-less reference is allowed
only under Central Package Management (`Directory.Packages.props`) — otherwise
NuGet treats it as a floating `*` and emits an `NU1604` warning.

## Usage

### 1. Generate the EMV payload string

No dependency injection required:

```csharp
using VietQR.Core;

// Dynamic QR (QRIBFTTA — transfer to account)
string payload = VietQrPayload.Create(
    BankBin.Create("970422"),        // MB Bank BIN, exactly 6 digits
    accountNo: "056789799999",
    amount: 50000,
    description: "hoc phi");

// 00020101021238520010A000000727...
Console.WriteLine(payload);
```

Omit `amount` to emit a static QR (`Point of Initiation Method = 11`).
Use `serviceType: VietQrServiceType.Card` for QRIBFTTC (transfer to card).

The library automatically strips Vietnamese diacritics and rejects control
characters, so `description` input is safe to pass as-is.

### 2. Look up a bank

The catalog is embedded — zero config, works offline:

```csharp
using VietQR.Core;

var catalog = EmbeddedBankCatalog.Instance;
BankInfo? bank = await catalog.FindAsync("970422");

// bank.Bin == "970422", bank.Code == "MB", bank.ShortName == "MBBank"
```

`FindAsync` accepts a BIN, a short code (`"MB"`) or a short name (`"MBBank"`),
and is case-insensitive.
See `IBankCatalog` for the full surface, including `GetAllAsync()`.

### 3. Render the card image

Register the library in DI, then request `IQrImageRenderer`. In an ASP.NET Core
app the container is provided by the host; the snippet below builds it manually
so it compiles as-is in any project:

```csharp
using VietQR.Core;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddVietQrCore();          // anywhere during app startup

using var provider = services.BuildServiceProvider();
var generator = provider.GetRequiredService<IQrGenerator>();
var renderer  = provider.GetRequiredService<IQrImageRenderer>();

string payload = generator.Generate(new VietQrRequest(
    Bin: BankBin.Create("970422"),
    AccountNo: "056789799999",
    Amount: 50000,
    Description: "hoc phi"));

var options = new QrRenderOptions
{
    Template = "print",              // "compact" (default) | "qr_only" | "print"
    BankBin = "970422",
    AccountNo = "056789799999",
    AccountName = "NGUYEN VAN A",
    Amount = 50000,
    Description = "hoc phi",
    Size = 500                       // only used by the "qr_only" template
};

byte[] png = await renderer.RenderPngAsync(payload, options);   // ct defaults to CancellationToken.None
string dataUrl = $"data:image/png;base64,{Convert.ToBase64String(png)}";
```

`RenderPngAsync` returns PNG `byte[]` — embed directly in HTML
(`data:image/png;base64,...`), write to disk, or return from your own REST API.

### Available templates

Canvas sizes, straight from the rendering code (`QrImageService.cs`):

| Template  | Canvas size                                                        | Recommended for                                         |
| --------- | ------------------------------------------------------------------ | ------------------------------------------------------- |
| `compact` | 540 × 540                                                          | Default card: VietQR header + QR + Napas247 + bank logo |
| `qr_only` | 500 × 500 (configurable via `QrRenderOptions.Size`)                | Minimal square QR with the VietQR logo in the center    |
| `print`   | 600 × dynamic (590 px + 28 px per info line + 16 px bottom margin) | Receipt-style card with amount, transfer note, account  |

| `qr_only`                                                                                         | `compact`                                                                                         | `print`                                                                                       |
| ------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| ![qr_only](https://raw.githubusercontent.com/Ngochiendev/vietqr-core/main/docs/image/qr_only.png) | ![compact](https://raw.githubusercontent.com/Ngochiendev/vietqr-core/main/docs/image/compact.png) | ![print](https://raw.githubusercontent.com/Ngochiendev/vietqr-core/main/docs/image/print.png) |

### Utilities

The `VietQr` class also exposes useful static helpers (used by our own test suite):

- `VietQr.GenCRCCode(string content)` — compute the CRC16-CCITT (Tag 63) for the
  payload **without** the 4-character CRC suffix.
- `VietQr.VerifyCRC(string content)` — validate a complete payload end-to-end;
  the string must already end with the `6304XXXX` CRC tag.
- `VietQr.ExtractBinFromEmvCo(string content)` — pull the BIN out of any
  compliant payload.

## Supported runtimes

| TFM     | Support  |
| ------- | -------- |
| net6.0  | ✅       |
| net8.0  | ✅ (LTS) |
| net9.0  | ✅       |
| net10.0 | ✅ (LTS) |

Native SkiaSharp binaries are packaged for Windows, Linux and macOS, so
`dotnet publish` works for all major targets without extra steps.

## Feedback

Bugs, feature requests and questions go to the
[GitHub issue tracker](https://github.com/Ngochiendev/vietqr-core/issues).
Release notes for every version are published on the
[GitHub Releases](https://github.com/Ngochiendev/vietqr-core/releases) page,
and on nuget.org under each package version.

## Contributing

See [CONTRIBUTING.md](https://github.com/Ngochiendev/vietqr-core/blob/main/CONTRIBUTING.md).
Please include the NAPAS247 reference payload you expect, along with your test,
for anything affecting generated output.

## License

Copyright © 2026 Ngochiendev.
This project is [MIT-licensed](https://github.com/Ngochiendev/vietqr-core/blob/main/LICENSE).

Payment QR codes reference bank accounts and transfer instructions; always
verify the BIN, account number and amount against your own infrastructure
before showing or rendering a QR to a customer. See
[DISCLAIMER.md](https://github.com/Ngochiendev/vietqr-core/blob/main/DISCLAIMER.md).
