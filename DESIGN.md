# KoraxLib Documentation Design System

## Purpose

KoraxLib documentation should feel technical, quiet, and direct. The site is for Slay the Spire 2 mod authors who need to understand what the library supports today and how to verify their integration.

## Visual Direction

- Use the VitePress default theme as the base system.
- Prefer restrained contrast, dense technical prose, and clear code examples over marketing-style layout.
- Keep visual customization minimal until the public API is larger.

## Tokens

- Typography: VitePress default sans and monospace stacks.
- Spacing: VitePress default spacing scale.
- Color: VitePress default palette with KoraxLib accent `#b45309` only for brand emphasis if customization is needed.
- Radius: VitePress defaults; no extra rounded decorative cards.

## Components

- Home hero: VitePress home layout only.
- Feature links: VitePress feature grid only.
- API pages: headings, tables, admonitions, and fenced code blocks.
- Warnings: VitePress `::: warning` blocks for unstable or not-yet-implemented API.
- Notes: VitePress `::: tip` and `::: info` blocks for verification notes.

## Content Rules

- Separate current supported behavior from planned behavior.
- Do not present vanilla ability APIs as implemented until source files exist.
- Preserve C# type names, file paths, environment variables, and log strings exactly.
- English is the root locale; Simplified Chinese lives under `/zh/`.

## Accessibility And Performance

- Prefer static Markdown pages with no custom client components.
- Every page has one H1 and descriptive frontmatter.
- Links must be concrete and build-checked.
- No third-party scripts, remote fonts, or analytics in the first version.
