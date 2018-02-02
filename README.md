# UrlBase64

`UrlBase64` is a package for all versions of the .NET Framework and .NET Core, which supports the creation of url-safe base64 encoding. `UrlBase64` provides symmetric routines to both encode and later decode data in a fashion that makes it safe for use on the general web and as input to ASP.NET web applications (under IIS or similar).

## Installation

`UrlBase64` is available via [nuget](https://www.nuget.org/packages/UrlBase64/) and the Visual Studio package manager. To install, simply run the following in the package manager console:

    Install-Package UrlBase64

Or search for `UrlBase64` in the Visual Studio package manager.

## Usage

All functions are contained in the static `UrlBase64` class, found in the `NeoSmart.Utils` namespace.

```csharp
using NeoSmart.Utils;

void UsageSample()
{
	var foo = Encoding.UTF8.GetBytes("foo");
	var encoded = UrlBase64.Encode(foo);
	var decoded = UrlBase64.Decode(encoded);

	var bar = Encoding.UTF8.GetString(decoded);

	Assert.AreEqual("foo", bar);
}
```

### Padding Options (Encoding)

`UrlBase64` supports two different padding modes (internally: `PaddingPolicy`) for generating base64 content:

```csharp
public enum PaddingPolicy
{
	Discard,
	Preserve,
}
```

The `PaddingPolicy` option controls the behavior of `UrlBase64` when encoding content that does not fall on a 4-character (output) boundary. Per [RFC 4648](https://tools.ietf.org/html/rfc4648), base64 specifies that an "optional depending on the circumstances" trailing `=` sign is used to pad the output to be a multiple of 4 characters long. `UrlBase64` supports both padded and unpadded output via an optional `PaddingPolicy` parameter to `UrlBase64.Encode` controlling this behavior. The default behavior at this time is to omit the trailing padding given that it a) can (and usually is) be inferred automatically when dropped from the encoded output, and b) utilizes a symbol that requires encoding when used in URLs.

```csharp
var bytes = Encoding.UTF8.GetBytes("Mary had a little lamb");
Console.WriteLine(bytes, UrlBase64.Encode(bytes, PaddingPolicy.Discard));
Console.WriteLine(bytes, UrlBase64.Encode(bytes, PaddingPolicy.Preserve));
```

>TWFyeSBoYWQgYSBsaXR0bGUgbGFtYg
>TWFyeSBoYWQgYSBsaXR0bGUgbGFtYg==

## License and Copyright

`UrlBase64` is developed by Mahmoud Al-Qudsi of NeoSmart Technologies. `UrlBase64` is released under the terms of the MIT Public License. Copyright NeoSmart Technologies 2017-2018.
