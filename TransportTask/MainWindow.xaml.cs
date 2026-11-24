using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace TransportTask
{
    public partial class MainWindow : Window 
    {
        private TransportInput _lastInput;
        private SolveResult _lastNw;
        private SolveResult _lastMin;

        public MainWindow()
        {
            InitializeComponent();
            LoadDefaultData();
        }

        private void LoadDefaultData()
        {
            CreateEmptyTable(3, 5);

            DataTable dt = ((DataView)InputGrid.ItemsSource).Table;
            int[] stocks = { 300, 250, 200 };
            int[] needs = { 210, 150, 120, 135, 135 };
            int[,] costs = { { 4, 8, 13, 2, 7 }, { 9, 4, 11, 9, 17 }, { 3, 16, 10, 1, 4 } };

            for (int i = 0; i < 3; i++)
            {
                dt.Rows[i]["Запасы"] = stocks[i];
                for (int j = 0; j < 5; j++)
                    dt.Rows[i][j + 1] = costs[i, j];
            }
            for (int j = 0; j < 5; j++)
                dt.Rows[3][j + 1] = needs[j];
        }

        private void CreateEmptyTable(int rowsCount, int colsCount)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Пункт");
            for (int j = 0; j < colsCount; j++) dt.Columns.Add("В" + (j + 1), typeof(int));
            dt.Columns.Add("Запасы", typeof(int));

            for (int i = 0; i < rowsCount; i++)
            {
                DataRow row = dt.NewRow();
                row[0] = "А" + (i + 1);
                row["Запасы"] = 0;
                for (int j = 0; j < colsCount; j++) row[j + 1] = 0;
                dt.Rows.Add(row);
            }

            DataRow needRow = dt.NewRow();
            needRow[0] = "Потребности";
            for (int j = 0; j < colsCount; j++) needRow[j + 1] = 0;
            dt.Rows.Add(needRow);

            InputGrid.ItemsSource = dt.DefaultView;
            RowsTextBox.Text = rowsCount.ToString();
            ColsTextBox.Text = colsCount.ToString();

            _lastInput = null; _lastNw = null; _lastMin = null;
            NwResultGrid.ItemsSource = null; NwCostText.Text = "";
            MinResultGrid.ItemsSource = null; MinCostText.Text = "";
        }

        private void OnApplySizeClick(object sender, RoutedEventArgs e)
        {
            int r, c;
            if (!int.TryParse(RowsTextBox.Text, out r) || !int.TryParse(ColsTextBox.Text, out c))
            {
                MessageBox.Show("Введите целые числа для размеров.", "Ошибка");
                return;
            }
            if (r < 1 || r > 20 || c < 1 || c > 20)
            {
                MessageBox.Show("Размер должен быть от 1 до 20.", "Ошибка");
                return;
            }
            CreateEmptyTable(r, c);
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            if (InputGrid.ItemsSource == null) return;

            DataTable dt = ((DataView)InputGrid.ItemsSource).Table;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                for (int j = 1; j < dt.Columns.Count; j++)
                {
                    dt.Rows[i][j] = 0;
                }
            }
            _lastInput = null; _lastNw = null; _lastMin = null;
            NwResultGrid.ItemsSource = null; NwCostText.Text = "";
            MinResultGrid.ItemsSource = null; MinCostText.Text = "";
        }

        private void OnDeleteSelectedClick(object sender, RoutedEventArgs e)
        {
            if (InputGrid.SelectedCells.Count == 0)
            {
                MessageBox.Show("Выделите ячейку в той строке или столбце, которую хотите удалить.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DataGridCellInfo cellInfo = InputGrid.SelectedCells[0];
            int colIndex = InputGrid.Columns.IndexOf(cellInfo.Column);
            int rowIndex = InputGrid.Items.IndexOf(cellInfo.Item);

            DataView dv = InputGrid.ItemsSource as DataView;
            if (dv == null) return;
            DataTable dt = dv.Table;

            if (colIndex == 0)
            {
                MessageBox.Show("Нельзя удалить первый столбец (Пункт).", "Ошибка");
                return;
            }
            if (rowIndex == dt.Rows.Count - 1)
            {
                MessageBox.Show("Нельзя удалить строку 'Потребности'.", "Ошибка");
                return;
            }
            if (dt.Columns.Count <= 2 || dt.Rows.Count <= 2)
            {
                MessageBox.Show("В таблице должна остаться хотя бы одна строка и один столбец.", "Ошибка");
                return;
            }

            dt.Columns.RemoveAt(colIndex);
            dt.Rows.RemoveAt(rowIndex);

            RowsTextBox.Text = (dt.Rows.Count - 1).ToString();
            ColsTextBox.Text = (dt.Columns.Count - 2).ToString();

            _lastInput = null; _lastNw = null; _lastMin = null;
            NwResultGrid.ItemsSource = null; NwCostText.Text = "";
            MinResultGrid.ItemsSource = null; MinCostText.Text = "";
        }

        private void OnSolveBothClick(object sender, RoutedEventArgs e)
        {
            TransportInput input;
            if (!TryReadInput(out input)) return;

            _lastInput = input;
            _lastNw = TransportSolver.NorthWestCorner(input);
            _lastMin = TransportSolver.MinElement(input);

            ShowResult(NwResultGrid, NwCostText, input, _lastNw, "СЗУ");
            ShowResult(MinResultGrid, MinCostText, input, _lastMin, "мин. элемент");
        }

        private void OnSolveNwClick(object sender, RoutedEventArgs e)
        {
            TransportInput input;
            if (!TryReadInput(out input)) return;

            _lastInput = input;
            _lastNw = TransportSolver.NorthWestCorner(input);
            ShowResult(NwResultGrid, NwCostText, input, _lastNw, "СЗУ");

            MinResultGrid.ItemsSource = null;
            MinCostText.Text = "";
            _lastMin = null;
        }

        private void OnSolveMinClick(object sender, RoutedEventArgs e)
        {
            TransportInput input;
            if (!TryReadInput(out input)) return;

            _lastInput = input;
            _lastMin = TransportSolver.MinElement(input);
            ShowResult(MinResultGrid, MinCostText, input, _lastMin, "мин. элемент");

            NwResultGrid.ItemsSource = null;
            NwCostText.Text = "";
            _lastNw = null;
        }

        private void OnImportClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Excel (*.xlsx)|*.xlsx";
            dlg.Title = "Импорт исходных данных";

            if (dlg.ShowDialog() != true) return;

            try
            {
                DataTable dt = ExcelService.Import(dlg.FileName);
                InputGrid.ItemsSource = dt.DefaultView;

                RowsTextBox.Text = (dt.Rows.Count - 1).ToString();
                ColsTextBox.Text = (dt.Columns.Count - 2).ToString();

                _lastInput = null; _lastNw = null; _lastMin = null;
                NwResultGrid.ItemsSource = null; NwCostText.Text = "";
                MinResultGrid.ItemsSource = null; MinCostText.Text = "";

                MessageBox.Show("Импортировано: " + (dt.Rows.Count - 1) + " поставщиков, " + (dt.Columns.Count - 2) + " потребителей.",
                                "Импорт", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка импорта:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            if (_lastNw == null || _lastMin == null || _lastInput == null)
            {
                MessageBox.Show("Сначала решите задачу.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Excel (*.xlsx)|*.xlsx";
            dlg.FileName = "TransportResult.xlsx";
            dlg.Title = "Экспорт решения";

            if (dlg.ShowDialog() != true) return;

            try
            {
                ExcelService.Export(dlg.FileName, _lastInput, _lastNw, _lastMin);
                MessageBox.Show("Файл сохранён:\n" + dlg.FileName, "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка экспорта:\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool TryReadInput(out TransportInput input)
        {
            input = null;
            try
            {
                DataView dv = InputGrid.ItemsSource as DataView;
                if (dv == null)
                {
                    MessageBox.Show("Нет исходных данных.", "Ошибка");
                    return false;
                }
                DataTable dt = dv.Table;

                int m = dt.Rows.Count - 1;
                int n = dt.Columns.Count - 2;

                if (m <= 0 || n <= 0)
                {
                    MessageBox.Show("Заполните хотя бы одного поставщика и одного потребителя.", "Ошибка");
                    return false;
                }

                int[,] costs = new int[m, n];
                int[] stocks = new int[m];
                int[] needs = new int[n];

                for (int i = 0; i < m; i++)
                {
                    DataRow row = dt.Rows[i];
                    for (int j = 0; j < n; j++)
                    {
                        object val = row[j + 1];
                        costs[i, j] = (val == DBNull.Value) ? 0 : Convert.ToInt32(val);
                    }
                    object stockVal = row["Запасы"];
                    stocks[i] = (stockVal == DBNull.Value) ? 0 : Convert.ToInt32(stockVal);
                }

                DataRow needRow = dt.Rows[m];
                for (int j = 0; j < n; j++)
                {
                    object val = needRow[j + 1];
                    needs[j] = (val == DBNull.Value) ? 0 : Convert.ToInt32(val);
                }

                int sumStocks = 0, sumNeeds = 0;
                for (int i = 0; i < m; i++) sumStocks += stocks[i];
                for (int j = 0; j < n; j++) sumNeeds += needs[j];

                if (sumStocks > sumNeeds)
                {
                    n++;
                    int[] newNeeds = new int[n];
                    Array.Copy(needs, newNeeds, n - 1);
                    newNeeds[n - 1] = sumStocks - sumNeeds;
                    needs = newNeeds;

                    int[,] newCosts = new int[m, n];
                    for (int i = 0; i < m; i++)
                    {
                        for (int j = 0; j < n - 1; j++) newCosts[i, j] = costs[i, j];
                        newCosts[i, n - 1] = 0; 
                    }
                    costs = newCosts;
                }
                else if (sumNeeds > sumStocks)
                {
                    m++;
                    int[] newStocks = new int[m];
                    Array.Copy(stocks, newStocks, m - 1);
                    newStocks[m - 1] = sumNeeds - sumStocks;
                    stocks = newStocks;

                    int[,] newCosts = new int[m, n];
                    for (int i = 0; i < m - 1; i++)
                        for (int j = 0; j < n; j++) newCosts[i, j] = costs[i, j];

                    for (int j = 0; j < n; j++)
                        newCosts[m - 1, j] = 0; 
                    costs = newCosts;
                }

                input = new TransportInput();
                input.M = m; input.N = n; input.Costs = costs; input.Stocks = stocks; input.Needs = needs;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка чтения данных (проверьте, что везде введены числа): " + ex.Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private static void ShowResult(DataGrid grid, System.Windows.Controls.TextBlock costText,
                                       TransportInput p, SolveResult res, string methodName)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Пункт");
            for (int j = 0; j < p.N; j++) dt.Columns.Add("В" + (j + 1));
            dt.Columns.Add("Запасы");

            for (int i = 0; i < p.M; i++)
            {
                DataRow row = dt.NewRow();
                bool isFictitious = (i == p.M - 1 && p.Costs[i, 0] == 0 && p.Stocks[i] > 0);
                row[0] = isFictitious ? "Ф" : "А" + (i + 1);

                for (int j = 0; j < p.N; j++) row[j + 1] = res.Allocation[i, j];
                row["Запасы"] = p.Stocks[i];
                dt.Rows.Add(row);
            }

            DataRow bottom = dt.NewRow();
            bottom[0] = "Потребности";
            for (int j = 0; j < p.N; j++) bottom[j + 1] = p.Needs[j];
            dt.Rows.Add(bottom);

            grid.ItemsSource = dt.DefaultView;
            costText.Text = methodName + ": суммарные затраты = " + res.TotalCost;
        }
    }
}