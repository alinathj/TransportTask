using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransportTask
{
    public class TransportInput
    {
        public int M;
        public int N;
        public int[,] Costs;
        public int[] Stocks;
        public int[] Needs;
    }

    public class SolveResult
    {
        public int[,] Allocation;
        public int TotalCost;
    }

    public static class TransportSolver
    {
        public static SolveResult NorthWestCorner(TransportInput p)
        {
            int m = p.M, n = p.N;
            int[,] alloc = new int[m, n];
            int[] stock = (int[])p.Stocks.Clone();
            int[] need = (int[])p.Needs.Clone();
            SolveResult res = new SolveResult();
            res.Allocation = alloc;

            int i = 0, j = 0;
            while (i < m && j < n)
            {
                int qty = Math.Min(stock[i], need[j]);
                alloc[i, j] = qty;
                stock[i] -= qty;
                need[j] -= qty;

                if (stock[i] == 0 && need[j] == 0) { i++; j++; }
                else if (stock[i] == 0) i++;
                else j++;
            }

            res.TotalCost = CalculateCost(p, alloc);
            return res;
        }

        public static SolveResult MinElement(TransportInput p)
        {
            int m = p.M, n = p.N;
            int[,] alloc = new int[m, n];
            int[] stock = (int[])p.Stocks.Clone();
            int[] need = (int[])p.Needs.Clone();
            bool[,] used = new bool[m, n];
            SolveResult res = new SolveResult();
            res.Allocation = alloc;

            List<int[]> cells = new List<int[]>();
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    cells.Add(new int[] { i, j, p.Costs[i, j] });
                }
            }

            cells.Sort(delegate (int[] a, int[] b) { return a[2].CompareTo(b[2]); });

            foreach (int[] cell in cells)
            {
                int i = cell[0];
                int j = cell[1];

                if (used[i, j] || stock[i] == 0 || need[j] == 0) continue;

                int qty = Math.Min(stock[i], need[j]);
                alloc[i, j] = qty;
                stock[i] -= qty;
                need[j] -= qty;

                if (stock[i] == 0)
                {
                    for (int k = 0; k < n; k++) used[i, k] = true;
                }
                if (need[j] == 0)
                {
                    for (int k = 0; k < m; k++) used[k, j] = true;
                }
            }

            res.TotalCost = CalculateCost(p, alloc);
            return res;
        }

        private static int CalculateCost(TransportInput p, int[,] alloc)
        {
            int total = 0;
            for (int i = 0; i < p.M; i++)
            {
                for (int j = 0; j < p.N; j++)
                {
                    total += alloc[i, j] * p.Costs[i, j];
                }
            }
            return total;
        }
    }
}
