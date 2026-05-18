using Amazon.DynamoDBv2.Model;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DynamoAwsManager
{
    public partial class AddItemDialog : Window
    {
        private readonly Dictionary<string, string> _keySchema;

        private readonly Dictionary<string, TextBox> _fixedBoxes = new();

        private readonly List<(TextBox Key, TextBox Value)> _extraRows = new();

        public Dictionary<string, AttributeValue> Item { get; private set; } = new();

        public AddItemDialog(Dictionary<string, string> keySchema, DynamicItem? existing = null)
        {
            InitializeComponent();
            _keySchema = keySchema;

            foreach (var kv in keySchema)
            {
                var label = new TextBlock
                {
                    Text = $"{kv.Key}  ({kv.Value})",
                    Margin = new Thickness(0, 4, 0, 2)
                };
                var box = new TextBox { Height = 34 };
                if (existing != null) box.Text = existing.Get(kv.Key);
                _fixedBoxes[kv.Key] = box;
                FieldsPanel.Children.Add(label);
                FieldsPanel.Children.Add(box);
            }

            if (existing != null)
            {
                foreach (var kv in existing.Attrs)
                {
                    if (_keySchema.ContainsKey(kv.Key)) continue;
                    AddExtraRow(kv.Key, kv.Value);
                }
            }
        }

        private void AddExtraRow(string key = "", string value = "")
        {
            var panel = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var keyBox = new TextBox { Height = 32, Text = key, Margin = new Thickness(0, 0, 6, 0) };
            var valBox = new TextBox { Height = 32, Text = value, Margin = new Thickness(0, 0, 6, 0) };
            var removeBtn = new Button
            {
                Content = "✕",
                Width = 32,
                Height = 32,
                Background = new System.Windows.Media.SolidColorBrush(
                                 (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444")),
                Foreground = System.Windows.Media.Brushes.White
            };

            var entry = (keyBox, valBox);
            removeBtn.Click += (_, __) =>
            {
                FieldsPanel.Children.Remove(panel);
                _extraRows.Remove(entry);
            };

            Grid.SetColumn(keyBox, 0);
            Grid.SetColumn(valBox, 1);
            Grid.SetColumn(removeBtn, 2);
            panel.Children.Add(keyBox);
            panel.Children.Add(valBox);
            panel.Children.Add(removeBtn);

            FieldsPanel.Children.Add(panel);
            _extraRows.Add(entry);
        }

        private void AddExtraAttr_Click(object sender, RoutedEventArgs e)
        {
            AddExtraRow(ExtraKeyBox.Text.Trim(), ExtraValueBox.Text.Trim());
            ExtraKeyBox.Clear();
            ExtraValueBox.Clear();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var item = new Dictionary<string, AttributeValue>();

            foreach (var kv in _keySchema)
            {
                var text = _fixedBoxes[kv.Key].Text.Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    MessageBox.Show($"'{kv.Key}' is required.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                item[kv.Key] = kv.Value == "N"
                    ? new AttributeValue { N = text }
                    : new AttributeValue { S = text };
            }

            foreach (var (keyBox, valBox) in _extraRows)
            {
                var k = keyBox.Text.Trim();
                var v = valBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(k))
                    item[k] = new AttributeValue { S = v };
            }

            Item = item;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}