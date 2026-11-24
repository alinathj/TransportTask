using System;
using System.Data;
using System.IO;
using ClosedXML.Excel;

namespace TransportTask
{
    public static class ExcelService
    {
        public static DataTable Import(string path)
        {
            XLWorkbook wb = new XLWorkbook(path);
            IXLWorksheet ws = wb.Worksheet(1);

            int headerRow = -1;
            for (int r = 1; r <= 20 && headerRow < 0; r++)
            {
                for (int c = 1; c <= 10; c++)
                {
                    string text = ws.Cell(r, c).GetString().Trim();
                    if (text.Equals("В1", StringComparison.OrdinalIgnoreCase))
                    {
                        headerRow = r;
                        break;
                    }
                }
            }
            if (headerRow < 0) throw new InvalidDataException("Не найдена строка с заголовком 'В1'.");

            int n = 0;
            for (int c = 2; c <= 30; c++)
            {
                string text = ws.Cell(headerRow, c).GetString().Trim();
                if (text.Length == 0 || text.Equals("Запасы", StringComparison.OrdinalIgnoreCase))
                    break;
                n++;
            }
            if (n == 0) throw new InvalidDataException("Не найдено ни одного потребителя.");

            int startData = headerRow + 1;

            int needsRow = -1;
            for (int r = startData; r <= startData + 200 && needsRow < 0; r++)
            {
                string a = ws.Cell(r, 1).GetString().Trim();
                if (a.Equals("Потребности", StringComparison.OrdinalIgnoreCase))
                    needsRow = r;
            }
            if (needsRow < 0) throw new InvalidDataException("Не найдена строка 'Потребности'.");

            int m = needsRow - startData;
            if (m <= 0) throw new InvalidDataException("Не найдено ни одного поставщика.");

            DataTable dt = new DataTable();
            dt.Columns.Add("Пункт");
            for (int j = 0; j < n; j++) dt.Columns.Add("В" + (j + 1), typeof(int));
            dt.Columns.Add("Запасы", typeof(int));

            for (int i = 0; i < m; i++)
            {
                DataRow row = dt.NewRow();
                row[0] = ws.Cell(startData + i, 1).GetString().Trim();

                for (int j = 0; j < n; j++)
                    row[j + 1] = GetIntValue(ws.Cell(startData + i, 2 + j));

                row["Запасы"] = GetIntValue(ws.Cell(startData + i, 2 + n));

                dt.Rows.Add(row);
            }

            DataRow needRow = dt.NewRow();
            needRow[0] = "Потребности";
            for (int j = 0; j < n; j++)
                needRow[j + 1] = GetIntValue(ws.Cell(needsRow, 2 + j));
            dt.Rows.Add(needRow);

            wb.Dispose();
            return dt;
        }

        private static int GetIntValue(IXLCell cell)
        {
            if (cell.IsEmpty()) return 0;

            double val;
            if (cell.TryGetValue(out val))
                return (int)val;

            string text = cell.GetString().Trim();
            if (string.IsNullOrEmpty(text)) return 0;

            int intVal;
            if (int.TryParse(text, out intVal))
                return intVal;

            return 0;
        }

        public static void Export(string path, TransportInput p, SolveResult nw, SolveResult min)
        {
            XLWorkbook wb = new XLWorkbook();
            IXLWorksheet ws = wb.Worksheets.Add("Решение");
            int row = 1;

            ws.Cell(row, 1).Value = "Исходные данные";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row += 2;

            WriteTableHeader(ws, row, p.N);
            row++;
            for (int i = 0; i < p.M; i++)
            {
                ws.Cell(row, 1).Value = "А" + (i + 1);
                for (int j = 0; j < p.N; j++) ws.Cell(row, 2 + j).Value = p.Costs[i, j];
                ws.Cell(row, 2 + p.N).Value = p.Stocks[i];
                row++;
            }
            ws.Cell(row, 1).Value = "Потребности";
            for (int j = 0; j < p.N; j++) ws.Cell(row, 2 + j).Value = p.Needs[j];
            row += 2;

            row = WriteResult(ws, row, "Метод северо-западного угла", p, nw);
            row++;
            row = WriteResult(ws, row, "Метод минимального элемента", p, min);
            row++;

            ws.Columns(1, 2 + p.N).AdjustToContents();
            ws.SheetView.FreezeRows(1);

            wb.SaveAs(path);
            wb.Dispose();
        }

        private static void WriteTableHeader(IXLWorksheet ws, int row, int n)
        {
            ws.Cell(row, 1).Value = "Пункт";
            for (int j = 0; j < n; j++) ws.Cell(row, 2 + j).Value = "В" + (j + 1);
            ws.Cell(row, 2 + n).Value = "Запасы";
            ws.Range(row, 1, row, 2 + n).Style.Font.Bold = true;
        }

        private static int WriteResult(IXLWorksheet ws, int row, string title, TransportInput p, SolveResult r)
        {
            ws.Cell(row, 1).Value = title;
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;

            WriteTableHeader(ws, row, p.N);
            row++;
            for (int i = 0; i < p.M; i++)
            {
                ws.Cell(row, 1).Value = "А" + (i + 1);
                for (int j = 0; j < p.N; j++) ws.Cell(row, 2 + j).Value = r.Allocation[i, j];
                ws.Cell(row, 2 + p.N).Value = p.Stocks[i];
                row++;
            }
            ws.Cell(row, 1).Value = "Потребности";
            for (int j = 0; j < p.N; j++) ws.Cell(row, 2 + j).Value = p.Needs[j];
            row++;

            ws.Cell(row, 1).Value = "Суммарные затраты = " + r.TotalCost;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontColor = XLColor.DarkGreen;
            row++;

            return row;
        }
    }
}