using Novolis.Markup.Markdown;
using Novolis.Markup.Markdown.Documents;

var outDir = Path.GetFullPath(Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".novolis",
    "artifacts",
    "hello-markdown-pdf"));
Directory.CreateDirectory(outDir);
var pdfPath = Path.Combine(outDir, "hello-markdown.pdf");

var document = new MarkdownDocument()
    .WithHeader("Duckville Harbor")
    .With(new MarkdownParagraph().WithText(
        "Freight bells marked the hour across the strait while spray bit the quay stones."))
    .WithHeader("Quay-side", 2)
    .With(new MarkdownParagraph().WithText(
        "Captain Rook checked the manifest twice, then folded the chart into his coat as fog settled low over the channel markers."))
    .With(new MarkdownHorizontalRule())
    .WithHeader("Lien notes", 3)
    .WithUnorderedList("Bonded cargo waited in the shed", "The tide turned toward the open reach")
    .WithOrderedList("Check the bond ledger", "Cast off at first light")
    .WithHeader("Manifest")
    .With(new MarkdownParagraph().WithText(
        "The river ran cold through Duckville. Harbor lights winked across the bonded shed while freight bells counted the watch."))
    .WithHeader("Departure")
    .With(new MarkdownParagraph().WithText(
        "Morning light found the bridge empty and the channel clear for a Calypso tramp outbound."));

MarkdownDocumentPdfExporter.ExportToFile(document, pdfPath, new MarkdownPagedExportOptions
{
    Title = "Duckville Harbor",
    Author = "Novolis",
});

var bytes = File.ReadAllBytes(pdfPath);
Console.WriteLine(pdfPath);
Console.WriteLine($"Bytes: {bytes.Length}");
return bytes.Length > 1000 && bytes.Length < 80_000 ? 0 : 1;
