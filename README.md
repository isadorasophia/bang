<p align="center">
<img src="media/logo-3x-2-export.png" alt="Murder logo">
</p>

<h1 align="center">A real ECS framework!</h1>

<p align="center">
<a href="https://github.com/isadorasophia/bang/actions/workflows/tag_release.yaml"><img src="https://github.com/isadorasophia/bang/actions/workflows/tag_release.yaml/badge.svg" alt="Publish release"></a>
<a href="LICENSE"><img src="https://img.shields.io/github/license/isadorasophia/bang.svg" alt="License"></a>
</p>

<p align="center">
Welcome to Bang! - an Entity Component System framework in C#.
</p>

### How does it work?
This is not, in any way, an engine, but rather a framework that you can use in whatever engine you're comfortable with. This is currently integrated with [Murder engine](https://github.com/isadorasophia/murder).

Check out our [wiki](https://github.com/isadorasophia/bang/wiki) for more details.

### How to use it?
We are on nuget! So you can either use this repository as a submodule or simply add:

```csproj
<BangVersion>0.0.6.230-dev</BangVersion>

<!-- Main Package. -->
<PackageReference Include="Murder.Bang" Version="$(BangVersion)" />

<!-- Generator package. Mandatory, include it so Bang can generate lookup classes and extensions for your Components. -->
<PackageReference Include="Murder.Bang.Generator" Version="$(BangVersion)">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>

<!-- Optional Package with analyzers and code fixes. -->
<PackageReference Include="Murder.Bang.Analyzers" Version="$(BangVersion)">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

Whatever makes you happier. The nuget package should have symbols and source embedded available with Source Link.

### Contributing
Feel free to contact me or [saint11](http://saint11.org/) if you have any suggestions. I am very interested in people trying it out and any feedback you may have!

Pull requests, bug reports and other contributions made with LLM or any generative AI technology will NOT be accepted. I am not in the slight interested in reviewing any non-human code.

### Real-life examples
[Neo City Express](https://github.com/isadorasophia/neocityexpress) was a Ludum Dare game (made in 72h ⏰) built on top of **Murder** and **Bang**. I highly recommend checking it out, especially if you're good with learning from examples.
