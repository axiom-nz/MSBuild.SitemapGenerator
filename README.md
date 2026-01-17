# MSBuild.SitemapGenerator

This was split off from a Blazor Standalone WASM project that relied on [BlazorWasmPreRendering.Build](https://github.com/jsakamoto/BlazorWasmPreRendering.Build) to generate prerendered HTML files.
Generating sitemaps from the prerendered HTML files was an easy way to piggyback off the existing build process.

## Required MSBuild Properties

- `BaseUrl`: The base URL for the website.
- `PublishDir`: The directory where the generated sitemap will be published.
- `RelativeWwwRootPath`: The relative path from `$(PublishDir)` to the wwwroot or equivalent directory.