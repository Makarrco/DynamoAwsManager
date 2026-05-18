using Amazon.DynamoDBv2.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DynamoAwsManager
{
    public partial class MainWindow : Window
    {
        private readonly DynamoDbService _service = new();

        private string? _currentTable;
        private TableDescription? _currentTableDesc;
        private List<DynamicItem> _allItems = new();
        private DynamicItem? _selected;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                Log("Application started.");
                await RefreshTableListAsync();
            };
        }

        private void Log(string msg)
        {
            LogTextBlock.Text += $"[{DateTime.Now:HH:mm:ss}]  {msg}{Environment.NewLine}";
            LogScrollViewer.ScrollToBottom();
        }

        private void ClearLog_Click(object s, RoutedEventArgs e)
        {
            LogTextBlock.Text = string.Empty;
            Log("Log cleared.");
        }

        private async Task RefreshTableListAsync()
        {
            try
            {
                Log("Fetching table list...");
                var tables = await _service.ListTablesAsync();
                TablesListBox.ItemsSource = tables;
                Log($"Found {tables.Count} table(s): {string.Join(", ", tables)}");
            }
            catch (Exception ex) { Log($"ERROR listing tables: {ex.Message}"); }
        }

        private async void RefreshTables_Click(object s, RoutedEventArgs e)
            => await RefreshTableListAsync();

        private async void TablesListBox_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            var table = TablesListBox.SelectedItem as string;
            if (table == null) return;

            _currentTable = table;
            DropTableButton.IsEnabled = true;
            AddItemButton.IsEnabled = true;
            RefreshItemsButton.IsEnabled = true;
            EditItemButton.IsEnabled = false;
            DeleteItemButton.IsEnabled = false;

            TableTitleBlock.Text = table;

            try
            {
                _currentTableDesc = await _service.DescribeTableAsync(table);
                var keys = string.Join(", ", _currentTableDesc.KeySchema
                    .Select(k => $"{k.AttributeName} ({k.KeyType})"));
                var count = _currentTableDesc.ItemCount;
                TableInfoBlock.Text = $"Keys: {keys}   |   Items: {count}";
                Log($"Selected table '{table}'.  {TableInfoBlock.Text}");
            }
            catch (Exception ex) { Log($"WARN describing table: {ex.Message}"); }

            await LoadItemsAsync();
        }

        private async void NewTable_Click(object s, RoutedEventArgs e)
        {
            var dlg = new CreateTableDialog { Owner = this };
            if (dlg.ShowDialog() != true) return;

            try
            {
                Log($"Creating table '{dlg.TableName}'...");
                await _service.CreateTableAsync(dlg.TableName, dlg.HashKeyName, dlg.HashKeyType,
                                                dlg.RangeKeyName, dlg.RangeKeyType);
                Log($"Waiting for '{dlg.TableName}' to become ACTIVE...");
                await _service.WaitUntilTableActiveAsync(dlg.TableName);
                Log($"Table '{dlg.TableName}' is ACTIVE.");
                await RefreshTableListAsync();
            }
            catch (Exception ex) { Log($"ERROR creating table: {ex.Message}"); }
        }

        private async void DropTable_Click(object s, RoutedEventArgs e)
        {
            if (_currentTable == null) return;
            var confirm = MessageBox.Show(
                $"Permanently delete table '{_currentTable}' and ALL its data?",
                "Confirm Drop", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Log($"Dropping table '{_currentTable}'...");
                await _service.DeleteTableAsync(_currentTable);
                Log($"Table '{_currentTable}' deleted.");
                _currentTable = null;
                _currentTableDesc = null;
                TableTitleBlock.Text = "← Select a table";
                TableInfoBlock.Text = "";
                ItemsDataGrid.Columns.Clear();
                ItemsDataGrid.ItemsSource = null;
                SetItemButtonsEnabled(false);
                DropTableButton.IsEnabled = false;
                await RefreshTableListAsync();
            }
            catch (Exception ex) { Log($"ERROR dropping table: {ex.Message}"); }
        }

        private async Task LoadItemsAsync()
        {
            if (_currentTable == null) return;
            try
            {
                Log($"Scanning '{_currentTable}'...");
                var raw = await _service.ScanTableAsync(_currentTable);
                _allItems = raw.Select(DynamicItem.FromRaw).ToList();
                RebuildColumns();
                ApplySearch();
                Log($"Loaded {_allItems.Count} item(s).");
            }
            catch (Exception ex) { Log($"ERROR scanning table: {ex.Message}"); }
        }

        private void RebuildColumns()
        {
            ItemsDataGrid.Columns.Clear();
            if (_allItems.Count == 0) return;

            var keyNames = _currentTableDesc?.KeySchema.Select(k => k.AttributeName).ToList()
                           ?? new List<string>();

            var allKeys = _allItems
                .SelectMany(i => i.Attrs.Keys)
                .Distinct()
                .OrderBy(k => keyNames.Contains(k) ? keyNames.IndexOf(k) : 999)
                .ThenBy(k => k)
                .ToList();

            foreach (var col in allKeys)
            {
                var colDef = new DataGridTextColumn
                {
                    Header = col,
                    Width = col == allKeys.Last() ? new DataGridLength(1, DataGridLengthUnitType.Star)
                                                     : new DataGridLength(140),
                    IsReadOnly = true,
                    Binding = new Binding($"Attrs[{col}]")
                };
                ItemsDataGrid.Columns.Add(colDef);
            }
        }

        private void ApplySearch()
        {
            var text = SearchBox.Text.ToLower();
            ItemsDataGrid.ItemsSource = string.IsNullOrWhiteSpace(text)
                ? _allItems
                : _allItems.Where(i => i.Attrs.Values.Any(v => v.ToLower().Contains(text))).ToList();
        }

        private async void RefreshItems_Click(object s, RoutedEventArgs e) => await LoadItemsAsync();

        private void SearchBox_TextChanged(object s, TextChangedEventArgs e) => ApplySearch();

        private void ItemsDataGrid_SelectionChanged(object s, SelectionChangedEventArgs e)
        {
            _selected = ItemsDataGrid.SelectedItem as DynamicItem;
            EditItemButton.IsEnabled = _selected != null;
            DeleteItemButton.IsEnabled = _selected != null;
        }

        private async void AddItem_Click(object s, RoutedEventArgs e)
        {
            if (_currentTable == null || _currentTableDesc == null) return;

            var keySchema = BuildKeySchemaDict();
            var dlg = new AddItemDialog(keySchema) { Owner = this };
            if (dlg.ShowDialog() != true) return;

            try
            {
                Log($"Putting item into '{_currentTable}'...");
                await _service.PutItemAsync(_currentTable, dlg.Item);
                Log("Item saved.");
                await LoadItemsAsync();
            }
            catch (Exception ex) { Log($"ERROR putting item: {ex.Message}"); }
        }

        private async void EditItem_Click(object s, RoutedEventArgs e)
        {
            if (_currentTable == null || _currentTableDesc == null || _selected == null) return;

            var keySchema = BuildKeySchemaDict();
            var dlg = new AddItemDialog(keySchema, _selected) { Owner = this };
            if (dlg.ShowDialog() != true) return;

            try
            {
                Log($"Updating item in '{_currentTable}'...");
                await _service.PutItemAsync(_currentTable, dlg.Item);
                Log("Item updated.");
                await LoadItemsAsync();
            }
            catch (Exception ex) { Log($"ERROR updating item: {ex.Message}"); }
        }

        private async void DeleteItem_Click(object s, RoutedEventArgs e)
        {
            if (_currentTable == null || _currentTableDesc == null || _selected == null) return;

            var confirm = MessageBox.Show("Delete this item?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var key = new Dictionary<string, AttributeValue>();
            foreach (var ks in _currentTableDesc.KeySchema)
                if (_selected.Raw.TryGetValue(ks.AttributeName, out var av))
                    key[ks.AttributeName] = av;

            try
            {
                Log($"Deleting item from '{_currentTable}'...");
                await _service.DeleteItemAsync(_currentTable, key);
                Log("Item deleted.");
                await LoadItemsAsync();
            }
            catch (Exception ex) { Log($"ERROR deleting item: {ex.Message}"); }
        }


        private Dictionary<string, string> BuildKeySchemaDict()
        {
            var result = new Dictionary<string, string>();
            if (_currentTableDesc == null) return result;

            foreach (var ks in _currentTableDesc.KeySchema)
            {
                var attrDef = _currentTableDesc.AttributeDefinitions
                    .FirstOrDefault(a => a.AttributeName == ks.AttributeName);
                var typeName = attrDef?.AttributeType.Value == "N" ? "N" : "S";
                result[ks.AttributeName] = typeName;
            }
            return result;
        }

        private void SetItemButtonsEnabled(bool enabled)
        {
            AddItemButton.IsEnabled = enabled;
            RefreshItemsButton.IsEnabled = enabled;
            EditItemButton.IsEnabled = false;
            DeleteItemButton.IsEnabled = false;
        }
    }
}