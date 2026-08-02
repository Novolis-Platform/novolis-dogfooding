# SketchLab

Dogfoods **Novolis.Avalonia.Controls** `SketchControl` with a Paint-light toolbar (Font Awesome via **Optris.Icons.Avalonia.FontAwesome**).

**Shipped app:** production bridge lives in **novolis-apps** as [Sketch Studio](../../../novolis-apps/src/SketchStudio/) — Open/Save `.sketchjson`, installer/zip via the apps catalog.

## Tools

Pen, Line, Spline, Box, Circle, Eraser, Select

## Options

Snap to grid, **Meetup** (vertex snap), Gridify, stroke color + width

## Clipboard

Copy PNG (ChatGPT-friendly), Copy SVG

## Run

```powershell
dotnet run --project novolis-dogfooding/apps/avalonia/SketchLab -p:NovolisUseProjectReferences=true
```

## Related

| Package / app | Role |
|---------------|------|
| `Novolis.Avalonia.Controls` | `SketchControl` canvas |
| [Sketch Studio](../../../novolis-apps/src/SketchStudio/) | Production sketch studio in novolis-apps |
