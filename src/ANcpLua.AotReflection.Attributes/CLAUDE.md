# ANcpLua.AotReflection.Attributes

Small attribute and metadata contract package for AOT reflection.

- Project: [ANcpLua.Analyzers.AotReflection.Attributes.csproj](ANcpLua.Analyzers.AotReflection.Attributes.csproj)
- Keep this layer dependency-light because generators and consumers both reference it.
- Runtime convenience helpers belong here only when they are part of the public contract.
