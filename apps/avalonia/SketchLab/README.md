# SketchLab

Dogfoods **Novolis.Avalonia.Controls** `SketchControl` with a Paint-light toolbar (Font Awesome via **Optris.Icons.Avalonia.FontAwesome**).

**Shipped app:** production desk lives in **novolis-apps** as [Sketch Studio](../../../novolis-apps/src/SketchStudio/) (`src/SketchStudio`) — Open/Save `.sketchjson`, installer/zip via the apps catalog.

Tools: Pen, Line, Spline, Box, Circle, Eraser, Select  
Options: Snap to grid, **Meetup** (vertex snap), Gridify, stroke color + width  
Clipboard: Copy PNG (ChatGPT-friendly), Copy SVG

```powershell
dotnet run --project novolis-dogfooding/apps/avalonia/SketchLab -p:NovolisUseProjectReferences=true
```
