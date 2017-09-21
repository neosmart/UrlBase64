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

## License and Copyright

`UrlBase64` is developed by Mahmoud Al-Qudsi of NeoSmart Technologies. `UrlBase64` is released under the terms of the MIT Public License.
