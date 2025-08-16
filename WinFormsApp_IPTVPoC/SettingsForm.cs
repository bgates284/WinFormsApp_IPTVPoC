using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFormsApp_IPTVPoC
{
    public class SettingsForm : Form
    {
        private ListBox lstPlaylists;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnOK;
        private Button btnCancel;
        private TextBox txtNewPlaylist;

        private AppSettings _settingsCopy;

        public AppSettings UpdatedSettings => _settingsCopy;

        public SettingsForm(AppSettings currentSettings)
        {
            Text = "Settings";
            Size = new Size(500, 400);
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9);
            StartPosition = FormStartPosition.CenterParent;

            // Make a copy so we only update if user clicks OK
            _settingsCopy = new AppSettings
            {
                Playlists = new List<string>(currentSettings.Playlists),
                LastSelectedIndex = currentSettings.LastSelectedIndex
            };

            BuildUI();
            LoadPlaylists();
        }

        private void BuildUI()
        {
            // Playlist list
            lstPlaylists = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 200,
                BackColor = Color.Black,
                ForeColor = Color.White
            };
            Controls.Add(lstPlaylists);

            // New playlist textbox
            txtNewPlaylist = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.Black,
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Enter new playlist URL..."
            };
            Controls.Add(txtNewPlaylist);

            // Add / Remove buttons
            var panelButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5),
                BackColor = Color.FromArgb(20, 20, 20)
            };
            Controls.Add(panelButtons);

            btnAdd = MakeButton("Add", BtnAdd_Click);
            panelButtons.Controls.Add(btnAdd);

            btnRemove = MakeButton("Remove", BtnRemove_Click);
            panelButtons.Controls.Add(btnRemove);

            // OK / Cancel buttons
            var panelBottom = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5),
                BackColor = Color.FromArgb(20, 20, 20)
            };
            Controls.Add(panelBottom);

            btnOK = MakeButton("OK", BtnOK_Click);
            panelBottom.Controls.Add(btnOK);

            btnCancel = MakeButton("Cancel", BtnCancel_Click);
            panelBottom.Controls.Add(btnCancel);
        }

        private Button MakeButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Width = 80,
                Height = 30,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private void LoadPlaylists()
        {
            lstPlaylists.Items.Clear();
            foreach (var url in _settingsCopy.Playlists)
            {
                lstPlaylists.Items.Add(url);
            }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            var url = txtNewPlaylist.Text.Trim();
            if (!string.IsNullOrEmpty(url))
            {
                _settingsCopy.Playlists.Add(url);
                lstPlaylists.Items.Add(url);
                txtNewPlaylist.Clear();
            }
            else
            {
                MessageBox.Show("Please enter a playlist URL.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (lstPlaylists.SelectedItem is string selected)
            {
                _settingsCopy.Playlists.Remove(selected);
                lstPlaylists.Items.Remove(selected);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
