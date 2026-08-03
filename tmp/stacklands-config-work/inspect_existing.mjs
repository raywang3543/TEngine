import { FileBlob, SpreadsheetFile } from "@oai/artifact-tool";

for (const path of process.argv.slice(2)) {
  const input = await FileBlob.load(path);
  const workbook = await SpreadsheetFile.importXlsx(input);
  const summary = await workbook.inspect({
    kind: "workbook,sheet,table,region",
    maxChars: 12000,
    tableMaxRows: 16,
    tableMaxCols: 14,
    tableMaxCellChars: 120,
  });
  console.log(`FILE=${path}`);
  console.log(summary.ndjson);
}
