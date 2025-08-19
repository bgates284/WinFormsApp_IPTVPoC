using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace WinFormsApp_IPTVPoC
{
    public class MainForm : Form
    {
        // UI controls
        private Panel leftPanel;
        private TreeView tvChannels;
        private Button btnToggleList;
        private Button btnUp;
        private Button btnDown;
        private Button btnSettings;
        private Button btnFullscreen;
        private VideoView videoView;
        private PictureBox picPoster;
        private TextBox txtOverview;
        private Label lblNowPlaying;

        // VLC
        private LibVLC _libVLC;
        private MediaPlayer _mediaPlayer;

        // Settings
        private AppSettings _appSettings;

        public MainForm()
        {
            Text = "IPTV PoC - WinForms";
            MinimumSize = new Size(900, 600);
            KeyPreview = true;
            BackColor = Color.Black;
            ForeColor = Color.White;

            // Enable form-wide key handling
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;

            BuildUI();

            // Initialize VLC
            Core.Initialize();
            _libVLC = new LibVLC();
            _mediaPlayer = new MediaPlayer(_libVLC);
            videoView.MediaPlayer = _mediaPlayer;

            // Load settings
            LoadAppSettings();

            // Load last playlist if available
            if (_appSettings.Playlists.Count > 0)
            {
                int last = _appSettings.LastSelectedIndex;
                if (last >= 0 && last < _appSettings.Playlists.Count)
                {
                    _ = LoadPlaylistAndPopulateAsync(_appSettings.Playlists[last]);
                }
            }

            // Handle ESC for fullscreen exit
            KeyDown += MainForm_KeyDown;
        }

        private void BuildUI()
        {

            // Create top toolbar
            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5),
                BackColor = Color.FromArgb(40, 40, 40)
            };
            Controls.Add(topPanel);

            var btnFullscreenTop = new Button
            {
                Text = "Fullscreen",
                Width = 90
            };
            btnFullscreenTop.Click += BtnFullscreen_Click;
            topPanel.Controls.Add(btnFullscreenTop);

            // Always-visible toggle list button
            var btnToggleListTop = new Button
            {
                Text = "Toggle List",
                Width = 90
            };
            btnToggleListTop.Click += BtnToggleList_Click;
            topPanel.Controls.Add(btnToggleListTop);

            // Always-visible settings button
            var btnSettingsTop = new Button
            {
                Text = "Settings",
                Width = 90
            };
            btnSettingsTop.Click += BtnSettings_Click;
            topPanel.Controls.Add(btnSettingsTop);

            //END OF TOP PANEL
            // Left panel
            leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            Controls.Add(leftPanel);

            // Channel list
            tvChannels = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None,
                HideSelection = false
            };
            tvChannels.AfterSelect += TvChannels_AfterSelect;
            leftPanel.Controls.Add(tvChannels);


            // Buttons under list
            var buttonsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(5),
                BackColor = Color.FromArgb(20, 20, 20)
            };
            leftPanel.Controls.Add(buttonsPanel);

            btnToggleList = MakeButton("Toggle List", BtnToggleList_Click);
            buttonsPanel.Controls.Add(btnToggleList);

            btnUp = MakeButton("Up", BtnUp_Click);
            buttonsPanel.Controls.Add(btnUp);

            btnDown = MakeButton("Down", BtnDown_Click);
            buttonsPanel.Controls.Add(btnDown);

            btnSettings = MakeButton("Settings", BtnSettings_Click);
            buttonsPanel.Controls.Add(btnSettings);

            btnFullscreen = MakeButton("Fullscreen", BtnFullscreen_Click);
            buttonsPanel.Controls.Add(btnFullscreen);

            // Video view
            videoView = new VideoView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black
            };
            Controls.Add(videoView);

            // Poster
            picPoster = new PictureBox
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            Controls.Add(picPoster);

            // Overview text
            txtOverview = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9),
                Visible = false
            };
            Controls.Add(txtOverview);

            // Now playing label
            lblNowPlaying = new Label
            {
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(50, 50, 50),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Text = "Now Playing: None"
            };
            Controls.Add(lblNowPlaying);
        }

        private void TvChannels_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is string url)
            {
                PlayUrl(url);
                lblNowPlaying.Text = "Now Playing: " + e.Node.Text;
            }
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

        private async Task LoadPlaylistAndPopulateAsync(string playlistUrl)
        {
            try
            {
                using var http = new HttpClient();
                var data = await http.GetStringAsync(playlistUrl);

                // Clear old nodes
                tvChannels.BeginInvoke(new Action(() => tvChannels.Nodes.Clear()));

                var lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                    {
                        string currentGroup = "Other";

                        // Extract group-title
                        var groupMatch = Regex.Match(line, @"group-title\s*=\s*""([^""]+)""", RegexOptions.IgnoreCase);
                        if (groupMatch.Success)
                            currentGroup = groupMatch.Groups[1].Value.Trim();

                        // Extract channel label (after last comma)
                        string label = line.Contains(",") ? line.Substring(line.LastIndexOf(',') + 1).Trim() : "Unknown";

                        // Next line should be the URL
                        if (i + 1 < lines.Length)
                        {
                            string url = lines[i + 1].Trim();
                            if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                            {
                                tvChannels.BeginInvoke(new Action(() =>
                                {
                                    // Ensure group node exists
                                    TreeNode groupNode = tvChannels.Nodes.Cast<TreeNode>()
                                        .FirstOrDefault(n => n.Text == currentGroup);

                                    if (groupNode == null)
                                    {
                                        groupNode = new TreeNode(currentGroup);
                                        tvChannels.Nodes.Add(groupNode);
                                    }

                                    // Add channel under group
                                    var channelNode = new TreeNode(label) { Tag = url };
                                    groupNode.Nodes.Add(channelNode);
                                }));
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load playlist: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PlayUrl(string url)
        {
            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.Stop();

            var media = new Media(_libVLC, url, FromType.FromLocation);
            _mediaPlayer.Play(media);
        }

        private void BtnToggleList_Click(object sender, EventArgs e)
        {
            leftPanel.Visible = !leftPanel.Visible;
        }

        private void BtnUp_Click(object sender, EventArgs e)
        {
            if (tvChannels.SelectedNode == null) return;

            TreeNode current = tvChannels.SelectedNode;

            // If it's a child node, move to the previous sibling or wrap up
            if (current.Parent != null)
            {
                int index = current.Index;
                if (index > 0)
                {
                    tvChannels.SelectedNode = current.Parent.Nodes[index - 1];
                    tvChannels.SelectedNode.EnsureVisible();
                }
                else if (current.Parent.PrevNode != null) // go to previous group’s last channel
                {
                    TreeNode prevGroup = current.Parent.PrevNode;
                    if (prevGroup.Nodes.Count > 0)
                    {
                        tvChannels.SelectedNode = prevGroup.Nodes[prevGroup.Nodes.Count - 1];
                        tvChannels.SelectedNode.EnsureVisible();
                    }
                }
            }
        }

        private void BtnDown_Click(object sender, EventArgs e)
        {
            if (tvChannels.SelectedNode == null) return;

            TreeNode current = tvChannels.SelectedNode;

            // If it's a child node, move to the next sibling or wrap down
            if (current.Parent != null)
            {
                int index = current.Index;
                if (index < current.Parent.Nodes.Count - 1)
                {
                    tvChannels.SelectedNode = current.Parent.Nodes[index + 1];
                    tvChannels.SelectedNode.EnsureVisible();
                }
                else if (current.Parent.NextNode != null) // go to next group’s first channel
                {
                    TreeNode nextGroup = current.Parent.NextNode;
                    if (nextGroup.Nodes.Count > 0)
                    {
                        tvChannels.SelectedNode = nextGroup.Nodes[0];
                        tvChannels.SelectedNode.EnsureVisible();
                    }
                }
            }
        }

        private void BtnSettings_Click(object sender, EventArgs e)
        {
            using var settingsForm = new SettingsForm(_appSettings);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                _appSettings = settingsForm.UpdatedSettings;
            }
        }

        private void BtnFullscreen_Click(object sender, EventArgs e)
        {
            ToggleFullscreen();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && FormBorderStyle == FormBorderStyle.None)
            {
                ToggleFullscreen();
            }
        }

        private void ToggleFullscreen()
        {
            if (FormBorderStyle == FormBorderStyle.None)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Normal;
                btnFullscreen.Text = "Fullscreen";
                leftPanel.Visible = true;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
                btnFullscreen.Text = "Windowed";
                leftPanel.Visible = false;
            }
        }

        private void LoadAppSettings()
        {
            _appSettings = new AppSettings
            {
                Playlists = new List<string>
        {
            "https://iptv-org.github.io/iptv/index.m3u"
        },
                LastSelectedIndex = 0
            };
        }
    }

    public class AppSettings
    {
        public List<string> Playlists { get; set; } = new List<string>();
        public int LastSelectedIndex { get; set; } = -1;
    }
}
