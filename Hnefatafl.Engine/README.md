# Krzaq.Hnefatafl.Console
Extension that adds few method to `IReadOnlyTable` interface.

## v1.0.0
Supports:
* `IEnumerable<T> GetColumn<T>(this IReadOnlyTable<T> table, int index)`
* `IEnumerable<T> GetRow<T>(this IReadOnlyTable<T> table, int index)`
* `IEnumerable<IEnumerable<T>> GetColumns<T>(this IReadOnlyTable<T> table)`
* `IEnumerable<IEnumerable<T>> GetRows<T>(this IReadOnlyTable<T> table)`
