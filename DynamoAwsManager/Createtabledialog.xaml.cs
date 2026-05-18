using System.Windows;

namespace DynamoAwsManager
{
    public partial class CreateTableDialog : Window
    {
        public string TableName { get; private set; } = "";
        public string HashKeyName { get; private set; } = "";
        public string HashKeyType { get; private set; } = "String";
        public string? RangeKeyName { get; private set; }
        public string? RangeKeyType { get; private set; }

        public CreateTableDialog() => InitializeComponent();

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TableNameBox.Text) ||
                string.IsNullOrWhiteSpace(HashKeyBox.Text))
            {
                MessageBox.Show("Table name and Partition Key are required.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TableName = TableNameBox.Text.Trim();
            HashKeyName = HashKeyBox.Text.Trim();
            HashKeyType = (HashTypeBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "String";

            var rangeKey = RangeKeyBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(rangeKey))
            {
                RangeKeyName = rangeKey;
                RangeKeyType = (RangeTypeBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "String";
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}