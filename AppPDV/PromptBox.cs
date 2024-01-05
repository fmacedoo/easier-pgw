namespace PGW
{
    public static class PromptBox
    {
        public static string? Show(string title, string prompt)
        {
            Form promptForm = new Form()
            {
                Width = 300,
                Height = 300,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterScreen,
            };

            Label label = new Label() { Left = 50, Top = 20, Width = 200, Text = prompt };
            TextBox textBox = new TextBox() { Left = 50, Top = 20 + 50, Width = 200 };
            Button confirmation = new Button() { Text = "OK", Left = 100, Width = 70, Top = 20 + 50 + 50, DialogResult = DialogResult.OK };

            confirmation.Click += (sender, e) => { promptForm.Close(); };

            promptForm.Controls.Add(label);
            promptForm.Controls.Add(textBox);
            promptForm.Controls.Add(confirmation);

            promptForm.AcceptButton = confirmation;

            return promptForm.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }

        public static bool ShowConfirmation(string title, string message)
        {
            Form promptForm = new Form()
            {
                Width = 300,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterScreen
            };

            Label messageLabel = new Label() { Left = 50, Top = 20, Width = 200, Text = message };
            Button okButton = new Button() { Text = "OK", Left = 50, Width = 70, Top = 70, DialogResult = DialogResult.OK };
            Button cancelButton = new Button() { Text = "Cancel", Left = 130, Width = 70, Top = 70, DialogResult = DialogResult.Cancel };

            promptForm.Controls.Add(messageLabel);
            promptForm.Controls.Add(okButton);
            promptForm.Controls.Add(cancelButton);

            promptForm.AcceptButton = okButton;
            promptForm.CancelButton = cancelButton;

            return promptForm.ShowDialog() == DialogResult.OK;
        }


        public static string? ShowList(string prompt, IEnumerable<string> options)
        {
            Form promptForm = new Form()
            {
                Width = 300,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = prompt,
                StartPosition = FormStartPosition.CenterScreen
            };

            ListBox listBox = new ListBox() { Left = 50, Top = 20, Width = 200, Height = 80 };
            Button confirmation = new Button() { Text = "OK", Left = 100, Width = 70, Top = 120, DialogResult = DialogResult.OK };

            foreach (string option in options)
            {
                listBox.Items.Add(option);
            }

            confirmation.Click += (sender, e) => { promptForm.Close(); };

            promptForm.Controls.Add(listBox);
            promptForm.Controls.Add(confirmation);

            promptForm.AcceptButton = confirmation;

            return promptForm.ShowDialog() == DialogResult.OK ? listBox.SelectedItem?.ToString() : null;
        }
    }
}