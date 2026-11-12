using System.Collections.ObjectModel;

namespace TMS2000.Mobile.Helpers;

public static class CollectionSynchronizer
{
    public static void Synchronize<TRow, TSource>(
        ObservableCollection<TRow> rows,
        IReadOnlyList<TSource> orderedSources,
        Func<TRow, TSource, bool> rowMatchesSource,
        Func<TSource, TRow> createRow,
        Action<TRow, TSource> updateRow)
    {
        for (int sourceIndex = 0; sourceIndex < orderedSources.Count; sourceIndex++)
        {
            TSource source = orderedSources[sourceIndex];

            int existingRowIndex = FindRowIndex(rows, source, rowMatchesSource, sourceIndex);
            TRow row;

            if (existingRowIndex < 0)
            {
                row = createRow(source);
                rows.Insert(sourceIndex, row);
            }
            else
            {
                row = rows[existingRowIndex];
                if (existingRowIndex != sourceIndex)
                {
                    rows.Move(existingRowIndex, sourceIndex);
                }
            }

            updateRow(row, source);
        }

        while (rows.Count > orderedSources.Count)
        {
            rows.RemoveAt(rows.Count - 1);
        }
    }

    private static int FindRowIndex<TRow, TSource>(
        ObservableCollection<TRow> rows,
        TSource source,
        Func<TRow, TSource, bool> rowMatchesSource,
        int firstIndexToSearch)
    {
        for (int rowIndex = firstIndexToSearch; rowIndex < rows.Count; rowIndex++)
        {
            if (rowMatchesSource(rows[rowIndex], source))
            {
                return rowIndex;
            }
        }

        return -1;
    }
}
