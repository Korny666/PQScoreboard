﻿using log4net;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PQScoreboard
{
    public partial class EditorForm : Form
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(EditorForm));

        private Scoreboard scoreboard;

        public EditorForm()
        {
            log.Info("EditorForm::ctor");

            InitializeComponent();
            DataGridViewScores.RowHeadersVisible = true;
            DataGridViewScores.ColumnHeadersVisible = true;
            DataGridViewScores.AllowUserToAddRows = false;
            DataGridViewScores.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            DataGridViewScores.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;

            SystemEvents.DisplaySettingsChanged += (s, e) =>
            {
                UpdateScreens();
            };
            UpdateScreens();

            scoreboard = null;
            UpdateControls();
            UpdateScores();
        }

        #region private functions

        private void UpdateScreens()
        {
            string selected = ComboBoxScreen.SelectedItem?.ToString();

            ComboBoxScreen.Items.Clear();
            for (int i = 0; i < Screen.AllScreens.Length; ++i)
            {
                string deviceName = Screen.AllScreens[i].DeviceName;
                ComboBoxScreen.Items.Add(deviceName);

                if (selected == deviceName)
                {
                    ComboBoxScreen.SelectedIndex = i;
                }
            }

            if (ComboBoxScreen.SelectedIndex < 0)
            {
                ComboBoxScreen.SelectedIndex = 0;
            }
        }

        private void UpdateScores()
        {
            if (scoreboard == null)
            {
                DataGridViewScores.Enabled = false;
                return;
            }

            string[] teams = scoreboard.Teams;

            if (teams.Length == 0)
            {
                DataGridViewScores.Enabled = false;
                return;
            }

            // TODO: implement soft update

            DataGridViewScores.Columns.Clear();
            DataGridViewScores.Rows.Clear();

            foreach (string team in teams)
            {
                DataGridViewScores.Columns.Add(team, team);
                DataGridViewScores.Columns[DataGridViewScores.Columns.Count - 1].SortMode =
                    DataGridViewColumnSortMode.NotSortable;
            }

            string[] categories = scoreboard.Categories;
            if (categories.Length == 0)
            {
                DataGridViewScores.Enabled = false;
                return;
            }

            DataGridViewScores.Rows.Add(categories.Length);

            for (int i = categories.Length - 1; i >= 0; --i)
            {
                DataGridViewScores.Rows[i].HeaderCell.Value = categories[i];
            }

            DataGridViewScores.Rows.Add();
            DataGridViewScores.Rows[categories.Length].HeaderCell.Value = "\u03a3";
            DataGridViewScores.Rows[categories.Length].ReadOnly = true;

            decimal[,] scores = scoreboard.Scores;

            for (int i = scores.GetLength(0) - 1; i >= 0; --i)
            {
                decimal totalScore = 0m;

                for (int j = scores.GetLength(1) - 1; j >= 0; --j)
                {
                    DataGridViewScores.Rows[j].Cells[i].Value = scores[i, j];
                    totalScore += scores[i, j];
                }

                DataGridViewScores.Rows[categories.Length].Cells[i].Value = totalScore;
            }

            DataGridViewScores.Enabled = true;
        }

        private void UpdateControls()
        {
            if (scoreboard == null)
            {
                MenuEdit.Enabled = false;
                MenuFileSave.Enabled = false;
                ButtonAnimate.Enabled = false;
            }
            else
            {
                MenuEdit.Enabled = true;
                MenuFileSave.Enabled = true;

                ButtonAnimate.Enabled = scoreboard.IsValid;
            }
        }

        private void LoadFromFile()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                //openFileDialog.InitialDirectory = "c:\\";
                openFileDialog.Filter = "CSV files (*.csv)|*.csv|JSON files (*.json)|*.json";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    switch (Path.GetExtension(openFileDialog.FileName))
                    {
                        case ".csv":
                            scoreboard = CsvHandler.LoadFromFile(openFileDialog.OpenFile());
                            UpdateScores();
                            UpdateControls();
                            break;
                    }
                }
            }
        }

        private void SaveToFile()
        {
            if (scoreboard == null)
            {
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "CSV files (*.csv)|*.csv|JSON files (*.json)|*.json";
                saveFileDialog.FilterIndex = 1;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.DefaultExt = "csv";
                saveFileDialog.AddExtension = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {

                    switch (Path.GetExtension(saveFileDialog.FileName))
                    {
                        default:
                        case ".csv":
                            CsvHandler.SaveToFile(scoreboard, saveFileDialog.OpenFile());
                            break;
                    }

                }
            }
        }

        #endregion

        #region interface actions

        private void MenuFileNew_Click(object sender, EventArgs e)
        {
            // TODO: check save

            try
            {
                NewScoreboardForm newScoreboardForm = new NewScoreboardForm();
                DialogResult result = newScoreboardForm.ShowDialog(this);

                if (result != DialogResult.OK)
                {
                    return;
                }

                scoreboard = new Scoreboard(newScoreboardForm.NumberOfTeams, newScoreboardForm.NumberOfCategories);

                UpdateScores();
                UpdateControls();

            }
            catch (Exception ex)
            {
                log.Error("Failed to create new scoreboard.", ex);
                MessageBox.Show("Failed to create new scoreboard: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuFileOpen_Click(object sender, EventArgs e)
        {
            try
            {
                LoadFromFile();
            }
            catch (Exception ex)
            {
                log.Error("Failed to open scoreboard.", ex);
                MessageBox.Show("Failed to open scoreboard: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuFileSave_Click(object sender, EventArgs e)
        {
            try
            {
                SaveToFile();
            }
            catch (Exception ex)
            {
                log.Error("Failed to save scoreboard.", ex);
                MessageBox.Show("Failed to save scoreboard: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuFileClose_Click(object sender, EventArgs e)
        {
            // TODO: save
            try
            {
                scoreboard = null;
                UpdateScores();
                UpdateControls();
            }
            catch (Exception ex)
            {
                log.Error("Failed to close scoreboard.", ex);
                MessageBox.Show("Failed to close scoreboard: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MenuEditAddTeam_Click(object sender, EventArgs e)
        {
            AddTeamForm addTeamForm = new AddTeamForm();
            DialogResult result = addTeamForm.ShowDialog(this);

            if (result != DialogResult.OK)
            {
                return;
            }

            try
            {
                scoreboard.AddTeam(addTeamForm.TeamName);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("Failed to add team: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UpdateScores();
            UpdateControls();
        }

        private void MenuEditAddCategory_Click(object sender, EventArgs e)
        {
            if (scoreboard == null)
            {
                // TODO: write to log
                return;
            }

            AddCategoryForm addCategoryForm = new AddCategoryForm(scoreboard.Teams);
            DialogResult result = addCategoryForm.ShowDialog(this);

            if (result != DialogResult.OK)
            {
                return;
            }

            try
            {
                scoreboard.AddCategory(addCategoryForm.CategoryName, addCategoryForm.Scores);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show("Failed to add category: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UpdateScores();
            UpdateControls();
        }

        private void ButtonAnimate_Click(object sender, EventArgs e)
        {
            ResultForm inputDialog = new ResultForm();

            inputDialog.WindowState = FormWindowState.Normal;
            inputDialog.FormBorderStyle = FormBorderStyle.None;
            inputDialog.StartPosition = FormStartPosition.Manual;
            Screen screen = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == ComboBoxScreen.SelectedItem.ToString());
            inputDialog.Bounds = (screen ?? Screen.PrimaryScreen).Bounds;

            inputDialog.StartAnimation(scoreboard, false);

            if (inputDialog.ShowDialog() != DialogResult.OK)
            {
            }

            inputDialog.Dispose();
        }

        #endregion

        #region interface view events

        private void DataGridViewScores_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            int team = e.ColumnIndex;
            int category = e.RowIndex;
            if (team < 0 || category < 0 || category == DataGridViewScores.Rows.Count - 1)
            {
                // column or row header update or total scores row
                return;
            }

            if (!decimal.TryParse(DataGridViewScores.Rows[category].Cells[team].Value.ToString(), out decimal score))
            {
                DataGridViewScores.Rows[category].Cells[team].Value =
                                    scoreboard.GetScore(team, category);
                MessageBox.Show("Failed to set score for team '" + scoreboard.GetTeamName(team)
                    + "' for category '" + scoreboard.GetCategoryName(category) + "': '"
                    + DataGridViewScores.Rows[category].Cells[team].Value.ToString()
                    + "' is not a valid score.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (scoreboard.SetScore(team, category, score))
            {
                // update total score
                DataGridViewScores.Rows[DataGridViewScores.Rows.Count - 1].Cells[team].Value =
                    scoreboard.GetTotalScore(team);
            }
        }

        #endregion
    }
}
