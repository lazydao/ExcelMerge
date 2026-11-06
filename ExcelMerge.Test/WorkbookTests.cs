using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace ExcelMerge.Test
{
    [TestClass]
    public class WorkbookTests
    {
        private string directory;

        [TestInitialize]
        public void Initialize()
        {
            directory = Path.Combine(Path.GetTempPath(), "ExcelMerge.Test", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(directory, true);
        }

        [TestMethod]
        public void GetSheetNames_XlsWithMissingRowRecord()
        {
            var path = CreateXlsWithMissingRowRecord("missing-row.xls", "tail");

            CollectionAssert.AreEqual(new[] { "Data" }, ExcelWorkbook.GetSheetNames(path).ToArray());
        }

        [TestMethod]
        public void Create_XlsWithMissingRowRecord_PreservesCellsAndInput()
        {
            var path = CreateXlsWithMissingRowRecord("missing-row.xls", "tail");
            var original = File.ReadAllBytes(path);

            var workbook = ExcelWorkbook.Create(path, new ExcelSheetReadConfig());

            AssertCells(workbook, "tail");
            CollectionAssert.AreEqual(original, File.ReadAllBytes(path));
        }

        [TestMethod]
        public void Diff_XlsWithMissingRowRecord_DetectsChangedCell()
        {
            var sourcePath = CreateXlsWithMissingRowRecord("source.xls", "before");
            var destinationPath = CreateXlsWithMissingRowRecord("destination.xls", "after");
            var source = ExcelWorkbook.Create(sourcePath, new ExcelSheetReadConfig());
            var destination = ExcelWorkbook.Create(destinationPath, new ExcelSheetReadConfig());

            var diff = ExcelSheet.Diff(source.Sheets["Data"], destination.Sheets["Data"],
                new ExcelSheetDiffConfig { SrcHeaderIndex = -1, DstHeaderIndex = -1 });

            Assert.AreEqual(1, diff.CreateSummary().ModifiedCellCount);
        }

        [TestMethod]
        public void Create_StandardXls_PreservesCells()
        {
            var path = Path.Combine(directory, "standard.xls");
            WriteWorkbook(new HSSFWorkbook(), path, "tail");

            AssertCells(ExcelWorkbook.Create(path, new ExcelSheetReadConfig()), "tail");
        }

        [TestMethod]
        public void Create_Xlsx_PreservesCells()
        {
            var path = Path.Combine(directory, "standard.xlsx");
            WriteWorkbook(new XSSFWorkbook(), path, "tail");

            AssertCells(ExcelWorkbook.Create(path, new ExcelSheetReadConfig()), "tail");
        }

        [TestMethod]
        public void GetSheetNames_Xlsx_ReleasesFile()
        {
            var path = Path.Combine(directory, "names.xlsx");
            WriteWorkbook(new XSSFWorkbook(), path, "tail");

            CollectionAssert.AreEqual(new[] { "Data" }, ExcelWorkbook.GetSheetNames(path).ToArray());
            using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        }

        private static void AssertCells(ExcelWorkbook workbook, string tail)
        {
            var rows = workbook.Sheets["Data"].Rows.Values.ToArray();
            Assert.AreEqual(2, rows.Length);
            Assert.AreEqual("id", rows[0].Cells[0].Value);
            Assert.AreEqual("22", rows[1].Cells[0].Value);
            Assert.AreEqual(tail, rows[1].Cells[1].Value);
            Assert.AreEqual(21, rows[1].Cells[1].OriginalRowIndex);
        }

        private static void WriteWorkbook(IWorkbook workbook, string path, string tail)
        {
            var sheet = workbook.CreateSheet("Data");
            var header = sheet.CreateRow(0);
            header.CreateCell(0).SetCellValue("id");
            header.CreateCell(1).SetCellValue("value");
            var row = sheet.CreateRow(21);
            row.CreateCell(0).SetCellValue(22);
            row.CreateCell(1).SetCellValue(tail);
            using (var stream = File.Create(path))
                workbook.Write(stream);
            workbook.Close();
        }

        private string CreateXlsWithMissingRowRecord(string name, string tail)
        {
            var path = Path.Combine(directory, name);
            WriteWorkbook(new HSSFWorkbook(), path, tail);
            byte[] records;
            using (var input = File.OpenRead(path))
            {
                var fileSystem = new POIFSFileSystem(input);
                using (var stream = fileSystem.CreateDocumentInputStream("Workbook"))
                {
                    records = new byte[stream.Length];
                    stream.ReadFully(records);
                }
            }

            // Keep the cells on Excel row 22, but remove its BIFF ROW record.
            // A single sheet keeps the BOUNDSHEET start offset unchanged.
            using (var modified = new MemoryStream())
            {
                var removed = 0;
                for (var offset = 0; offset < records.Length;)
                {
                    var id = BitConverter.ToUInt16(records, offset);
                    var size = BitConverter.ToUInt16(records, offset + 2);
                    if (id == 0x0208 && BitConverter.ToUInt16(records, offset + 4) == 21)
                        removed++;
                    else
                        modified.Write(records, offset, size + 4);
                    offset += size + 4;
                }
                Assert.AreEqual(1, removed, "The fixture must omit exactly one ROW record.");
                modified.Position = 0;
                var fileSystem = new POIFSFileSystem();
                fileSystem.CreateDocument(modified, "Workbook");
                using (var output = File.Create(path))
                    fileSystem.WriteFileSystem(output);
            }
            return path;
        }
    }
}
