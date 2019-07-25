using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace Sudoku
{
    public partial class Form1 : Form
    {
        // cell dimensions
        const int CellHeight = 32;
        const int CellWidth = 32;

        // window top-left corner offsets
        // offsets describes location of a piece of data
        // in relation to another location.
        const int xOffset = -20;
        const int yOffset = 20;

        // default empty square color
        private Color DEFAULT_BACKCOLOR = Color.White;

        // colors for default puzzle values
        private Color FIXED_BACKCOLOR = Color.LightSteelBlue;
        private Color FIXED_FORECOLOR = Color.Blue;

        // colors for values inserted by user. 
        private Color USER_BACKCOLOR = Color.Black;
        private Color USER_FORECOLOR = Color.LightYellow;

        // current number selected for insertion
        private int selectedNumber;

        // stack for keeping track of all moves (LIFO data structure)
        private Stack<string> moves;
        private Stack<string> redoMoves;

        // track filename to save to
        private string saveFileName = String.Empty;

        // representation of file in the grid
        private int[,] actualGrid = new int[10, 10];

        // to track the time elapsed
        private int seconds = 0;

        // game started or not?
        private bool gameStarted = false;

        // possible values for a sell stored in thsi array
        private string[,] possible = new string[10, 10];

        // recognises if user wants cell hint or have the whole
        // puzzle sovled.
        private bool hintMode;

        /********************************************
         DRAWING THE CELLS AND INITIALIZING THE GRID   
        *********************************************/

        public void DrawBoard()
        {
            // default selected number = 1
            toolStripButton1.Checked = true;
            selectedNumber = 1;

            // used to store location of the cell

            Point location = new Point();
            // drawing the cells
            for (int row = 1; row <= 9; row++)
            {
                for (int col = 1; col <= 9; col++)
                {
                    location.X = col * (CellWidth + 1) + xOffset; //  horizontal axis 13, 46.. next row: 13, 46 -->
                    location.Y = row * (CellHeight + 1) + yOffset; // vertical axis 53, 53.. next row: 86, 86 -->
                    Label lblCell = new Label
                    {
                        Name = col.ToString() + row.ToString(),
                        BorderStyle = BorderStyle.Fixed3D,
                        Location = location,
                        Width = CellWidth,
                        Height = CellHeight,
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = DEFAULT_BACKCOLOR,
                        Font = new Font(Font, FontStyle.Bold),
                        Tag = 1
                    };
                    lblCell.Click += Cell_Click;
                    Controls.Add(lblCell);
                }
            }
        } // end DrawBoard()

        /// <summary>
        /// Handles cell click event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cell_Click(object sender, EventArgs e)
        {
            // check to see if game has started or not
            if (!gameStarted)
            {
                DisplayActivity($"Click File->New to start a new " +
                    $"game or File->Open to load an existing game", true);
                return;
            }

            Label cellLabel = sender as Label;

            // if cell is not erasable then exit
            if (cellLabel.Tag.ToString().Equals(0))
            {
                DisplayActivity($"You cannot erase this cell", false);
                return;
            }

            // determine the col and row of the selected cell
            int col = int.Parse(cellLabel.Name[0].ToString());
            int row = int.Parse(cellLabel.Name[1].ToString());

            // if erasing a cell
            if (selectedNumber.Equals(0))
            {
                // if cell is empty then no need to erase
                if (actualGrid[col, row] == 0) { return; }

                // save the value in the grid array
                SetCell(col, row, selectedNumber, 1);
                DisplayActivity($"Number erased at ({col}, {row})", false);
            }
            else if (cellLabel.Text == String.Empty)
            {
                // else set a value; check if move is valid
                if (!IsMoveValid(col, row, selectedNumber))
                {
                    DisplayActivity($"Invalid move at ({col}, {row})", false);
                    return;
                }
                // save the value in the grid array
                SetCell(col, row, selectedNumber, 1);
                DisplayActivity($"Number placed at ({col}, {row})", false);

                // saves the move into the stack
                moves.Push(cellLabel.Name.ToString() + selectedNumber);

                // check if the puzzle is solved
                if (IsPuzzleSolved())
                {
                    timer1.Enabled = false;
                    Beep();
                    toolStripStatusLabel1.Text = "****Puzzle Solved****";
                }
            }
        }

        /// <summary>
        /// Checks if puzzle is solved
        /// </summary>
        /// <returns></returns>
        private bool IsPuzzleSolved()
        {
            // check row by row
            string pattern;
            int r, c;
            for (r = 1; r <= 9; r++)
            {
                pattern = "123456789";
                for (c = 1; c <= 9; c++)
                {
                    pattern = pattern.Replace(actualGrid[c, r].ToString(), String.Empty);
                }
                if (pattern.Length > 0) { return false; }
            }

            // check col by col
            for (c = 1; c <= 9; c++)
            {
                pattern = "123456789"; // each row should have values 1 - 9
                for (r = 1; r <= 9; r++)
                {
                    pattern = pattern.Replace(actualGrid[c, r].ToString(), String.Empty);
                }
                if (pattern.Length > 0) { return false; }
            }

            // check minigrid
            for (c = 1; c <= 9; c += 3)
            {
                pattern = "123456789";
                for (r = 1; r <= 9; r += 3)
                {
                    for (int x = 0; x <= 2; x++)
                    {
                        for (int y = 0; y <= 2; y++)
                        {
                            pattern = pattern.Replace(actualGrid[c + x, r + y].ToString(), String.Empty);
                        }
                    }
                }
                if (pattern.Length > 0) { return false; }
            }
            return true;
        }

        public bool IsMoveValid(int col, int row, int value)
        {
            bool puzzleSolved = true;

            // scan through specific column
            for (int colScan = 1; colScan <= 9; colScan++)
            {
                if (actualGrid[col, colScan] == value)
                    return false;
            }

            // scan through specific row
            for (int rowScan = 1; rowScan <= 9; rowScan++)
            {
                if (actualGrid[rowScan, row] == value)
                    return false;
            }

            // scan through minigrid
            // starting column and row should be within minigrid
            int startColumn = col - ((col - 1) % 3); // ???
            int startRow = row - ((row - 1) % 3);
            // iterate through the minigrid
            for (int x = 0; x <= 2; x++)
            {
                for (int y = 0; y <= 2; y++)
                {
                    // go through each row in minigrid
                    if (actualGrid[startColumn + y, startRow + x] == value)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Displays message in textbox control
        /// </summary>
        /// <param name="str"></param>
        /// <param name="soundBeep"></param>
        public void DisplayActivity(string str, bool soundBeep)
        {
            if (soundBeep) { Beep(); }
            txtActivities.Text += str + Environment.NewLine;
        }

        private void Beep()
        {
            // throw new NotImplementedException();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //initialize status bar
            toolStripStatusLabel1.Text = String.Empty;
            toolStripStatusLabel2.Text = String.Empty;
            //Draw boards
            DrawBoard();
        }



        public Form1()
        {
            InitializeComponent();
        }



        // Draw the lines outlining the minigrids 
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            int x1, y1, x2, y2;
            //Draw the horizontal lines
            x1 = 1 * (CellWidth + 1) + xOffset - 1;
            x2 = 9 * (CellWidth + 1) + xOffset + CellWidth;
            for (int r = 1; r <= 10; r = r + 3)
            {
                y1 = r * (CellHeight + 1) + yOffset - 1;
                y2 = y1;
                e.Graphics.DrawLine(Pens.DarkGreen, x1, y1, x2, y2);
            }
            //Draw vertical lines
            y1 = 1 * (CellHeight + 1) + yOffset - 1;
            y2 = 9 * (CellHeight + 1) + yOffset + CellHeight;
            for (int c = 1; c <= 10; c = c + 3)
            {
                x1 = c * (CellHeight + 1) + xOffset - 1;
                x2 = x1;
                e.Graphics.DrawLine(Pens.DarkGreen, x1, y1, x2, y2);
            }
        }

        // Start new game
        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (gameStarted)
            {
                DialogResult response = MessageBox.Show("Do you want to save current game?", "Save current game", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                if (response == DialogResult.Yes)
                {
                    SaveGameToDisk(false);
                }
                else if (response == DialogResult.Cancel)
                {
                    return;
                }
            }
            StartNewGame();
        }

        /// <summary>
        /// Saves the game to disk
        /// </summary>
        /// <param name="saveAs"></param>
        public void SaveGameToDisk(bool saveAs)
        {
            // if saveFileName is empty, means game 
            // has not been saved before
            if (saveFileName == String.Empty || saveAs)
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog()
                {
                    Filter = "SDO files (*.sdo) |*.sdo|All files (*.*) | *.*",
                    FilterIndex = 1,
                    RestoreDirectory = false
                })
                {
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // store the filename first
                        saveFileName = saveFileDialog.FileName;
                    }
                }
            }
            else { return; }

            // formulate the string representing the values to store
            StringBuilder str = new StringBuilder();
            for (int row = 1; row <= 9; row++)
                for (int col = 1; col <= 9; col++)
                    str.Append(actualGrid[col, row]).ToString();

            // save the value to file

            bool fileExists;
            fileExists = File.Exists(saveFileName);
            try
            {
                File.WriteAllText(saveFileName, str.ToString());
                toolStripStatusLabel1.Text = "Puzzle saved in " + saveFileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving game. Please try again. {Environment.NewLine} Error: {ex}");
            }

        }

        // Save as....menu item


        // Save menu item


        // Open saved game


        // 

        public void StartNewGame()
        {
            saveFileName = String.Empty;
            txtActivities.Text = String.Empty;
            seconds = 0;
            ClearBoard();
            gameStarted = true;
            timer1.Enabled = true;
            toolStripStatusLabel1.Text = "New game started";
            toolTipHint.RemoveAll();
        }

        public void ClearBoard()
        {
            // initialize the stacks
            moves = new Stack<string>();
            redoMoves = new Stack<string>();

            // intialize the cells in the board
            for (int row = 1; row <= 9; row++)
                for (int col = 1; col <= 9; col++)
                    SetCell(col, row, 0, 1);
        }

        /// <summary>
        /// assigns a value to a cell
        /// </summary>
        /// <param name="col">Column number</param>
        /// <param name="row">Row number</param>
        /// <param name="value">Value to be set</param>
        /// <param name="erasable">0 or 1</param>
        private void SetCell(int col, int row, int value, short erasable)
        {
            // locate particular label control
            Control[] controls = this.Controls.Find(col.ToString() + row.ToString(), true);
            Label lblCell = controls[0] as Label;

            // save the value in the grid array
            actualGrid[col, row] = value;

            // if erasing a cell you need to reset the possible values
            if (value.Equals(0))
            {
                for (int r = 1; r <= 9; r++)
                    for (int c = 1; c <= 9; c++)
                        if (actualGrid[c, r] == 0)
                            possible[c, r] = string.Empty;
            }
            else
            {
                possible[col, row] = value.ToString();
            }

            // set label control appearance
            if (value.Equals(0))
            {
                lblCell.Text = String.Empty;
                lblCell.Tag = erasable;
                lblCell.BackColor = DEFAULT_BACKCOLOR;
            }
            else
            {
                if (erasable.Equals(0))
                {
                    lblCell.BackColor = FIXED_BACKCOLOR;
                    lblCell.ForeColor = FIXED_FORECOLOR;
                }
                else
                {
                    lblCell.BackColor = USER_BACKCOLOR;
                    lblCell.ForeColor = USER_FORECOLOR;
                }
                lblCell.Text = value.ToString();
                lblCell.Tag = erasable;
            }

        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel2.Text = "Elapsed time: " + seconds + " second(s)";
            seconds++;
        }

        // event handler for the ToolStripButton controls
        private void ToolStripButton_Click(object sender, EventArgs e)
        {
            var selectedButton = sender as ToolStripButton;

            // uncheck all the Button controls in the ToolStrip
            // ten with the inclusion of erase button
            for (int i = 1; i <= 10; i++)
            {
                var btn = toolStrip1.Items["toolStripButton" + i.ToString()] as ToolStripButton;
                btn.Checked = false;
            }
            // set the selected button to "checked"
            selectedButton.Checked = true;
            // set the appropriate number selected
            selectedNumber = selectedButton.Text.Equals("Erase") ? 0 : int.Parse(selectedButton.Text);
        }

        /// <summary>
        /// Undo moves
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if no previous moves then exit'
            if (moves.Count == 0) { return; }

            // remove from the moves stack and push into the redoMoves stack
            string str = moves.Pop();
            redoMoves.Push(str);

            // save value in array 
            SetCell(int.Parse(str[0].ToString()), int.Parse(str[1].ToString()), 0, 1);
            DisplayActivity($"Value removed at ({int.Parse(str[0].ToString())}," +
                $" {int.Parse(str[1].ToString())})", false);
        }

        /// <summary>
        /// Redo move
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // if redoMove stack is empty, then exit
            if (redoMoves.Count == 0) { return; }
            // remove from the redoMoves stack and push into
            // moves stack
            string str = redoMoves.Pop();
            moves.Push(str);
            // save the value in the array 
            SetCell(int.Parse(str[0].ToString()), int.Parse(str[1].ToString()), 0, 1);
            DisplayActivity($"Value removed at ({int.Parse(str[0].ToString())}," +
                $" {int.Parse(str[1].ToString())})", false);
        }

        /// <summary>
        /// Open saved game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (gameStarted)
            {
                DialogResult response = MessageBox.Show
                    ("Do you want to save current game?", "Save current game",
                    MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (response == DialogResult.Yes)
                {
                    SaveGameToDisk(false);
                }
                else if (response == DialogResult.Cancel)
                {
                    return;
                }
            }

            // load game from disk
            string fileContents = "";
            using (OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = "SDO files (*.sdo) |*.sdo|All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = false
            })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        fileContents = File.ReadAllText(openFileDialog.FileName);
                        toolStripStatusLabel1.Text = openFileDialog.FileName;
                        saveFileName = openFileDialog.FileName;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"There was a problem opening the file." +
                            $"{Environment.NewLine} Error: {ex}");
                    }
                }
                else
                { return; }
            }
            StartNewGame();

            // initialize the board
            short counter = 0;
            for (int row = 1; row <= 9; row++)
            {
                for (int col = 1; col <= 9; col++)
                {
                    try
                    {
                        // zero values represent empty cells
                        if (!int.Parse(fileContents[counter].ToString()).Equals(0))
                            SetCell(col, row, int.Parse(fileContents[counter].ToString()), 0);
                    }
                    catch
                    {
                        MessageBox.Show($"File does not contain a valid Sudoku puzzle");
                    }
                    counter++;
                }
            }
        }

        /// <summary>
        /// Ending the game
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (gameStarted)
            {
                DialogResult response = MessageBox.Show("Do you want to save current game?", "Save current game", MessageBoxButtons.YesNoCancel);

                if (response == DialogResult.Yes)
                {
                    SaveGameToDisk(false);
                }
                else if (response == DialogResult.Cancel)
                { return; }
                // exit app;
            }
        }

        /// <summary>
        /// Save as event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!gameStarted)
            {
                DisplayActivity("Game not started yet.", true);
                return;
            }

            SaveGameToDisk(true);
        }

        /// <summary>
        /// Save game event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!gameStarted)
            {
                DisplayActivity("Game not started yet.", true);
                return;
            }
            SaveGameToDisk(false);
        }

        /// <summary>
        /// set the tooltip for a label control
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <param name="possibleValues"></param>
        public void SetToolTip(int col, int row, string possibleValues)
        {
            Control[] lblCellHint = Controls.Find(col.ToString() + row.ToString(), true);
            toolTipHint.SetToolTip(lblCellHint[0] as Label, possibleValues);
        }

        /// <summary>
        /// calculate all the possible values for a cell
        /// </summary>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public string CalculatePossibleValues(int col, int row)
        {
            // get the current possible values for a cell
            // eliminates the values not possible from the string str
            string str;
            str = possible[col, row] == string.Empty ?
                "123456789" : possible[col, row];

            // check column
            int r, c;
            for (r = 1; r <= 9; r++)
                if (!actualGrid[col, r].Equals(0)) // there is an actual value
                    str = str.Replace(actualGrid[col, r].ToString(), string.Empty);

            // check row
            for (c = 1; c <= 9; c++)
                if (!actualGrid[c, row].Equals(0))
                    str = str.Replace(actualGrid[c, row].ToString(), string.Empty);

            // check minigrid
            int startColumn, startRow;
            startColumn = col - ((col - 1) % 3);
            startRow = row - ((row - 1) % 3);
            for (int miniGridRow = startRow; miniGridRow <= startRow + 2; miniGridRow++)
                for (int miniGridColumn = startColumn; miniGridColumn <= startColumn + 2; miniGridColumn++)
                    if (!actualGrid[miniGridColumn, miniGridRow].Equals(0))
                        str = str.Replace(actualGrid[miniGridColumn, miniGridRow].ToString(), string.Empty);

            // if possible value is an empty string then error because
            // of invalid move.
            if (str == string.Empty)
                throw new Exception("Invalid move");
            return str;
        }
        /// <summary>
        /// Calculate possible values for all cells 
        /// </summary>
        /// <returns></returns>
        public bool CheckColumnsAndRows()
        {
            bool changes = false;
            // check all cells
            for (int row = 1; row <= 9; row++)
            {
                for (int col = 1; col <= 9; col++)
                {
                    if (actualGrid[col, row] == 0)
                    {
                        try
                        {
                            possible[col, row] = CalculatePossibleValues(col, row);
                        }
                        catch
                        {
                            DisplayActivity("Invalid placement, please undo move", false);
                            throw new Exception("Invalid Move");
                        }
                        // display the possible values in the Tooltip
                        SetToolTip(col, row, possible[col, row]);

                        if (possible[col, row].Length == 1)
                        {
                            // means a number has been confirmed
                            SetCell(col, row, int.Parse(possible[col, row].ToString()), 1);
                            // number is confirmed
                            actualGrid[col, row] = int.Parse(possible[col, row].ToString());
                            DisplayActivity("Col/Row and Minigrid Elimination", false);
                            DisplayActivity("==========================", false);
                            DisplayActivity($"Inserted value {actualGrid[col, row]} in ({col}, {row}) ", false);

                            // get the UI of the application to refresh
                            // with the newly confirmed number
                            Application.DoEvents();

                            // saves the move into the stack
                            moves.Push($"{col}{row}{possible[col, row]}");
                            changes = true;

                            // if user only asks for a hint, stop at this point
                            if (hintMode)
                            {
                                hintMode = false;
                                return true;
                            }
                        }
                    }
                }
            }
            return changes;
        }

        // hint button
        private void BtnHint_Click(object sender, EventArgs e)
        {
            // show hints one cell at a time
            hintMode = true;
            try
            {
                SolvePuzzle();
            }
            catch
            {
                MessageBox.Show("Please undo your move", "invalid move",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // solve puzzle button
        private void BtnSolvePuzzle_Click(object sender, EventArgs e)
        {
            // solve puzzle
            hintMode = false;
            try
            {
                SolvePuzzle();
            }
            catch
            {
                MessageBox.Show("Please undo you move", "Invalid move",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool SolvePuzzle()
        {
            bool changes;
            bool exitLoop = false;

            try
            {
                do
                {
                    // perform col/row and minigrid elimination
                    changes = CheckColumnsAndRows();
                    if ((hintMode && changes) || IsPuzzleSolved())
                    {
                        exitLoop = true;
                    }
                } while (!changes);
            }
            catch
            {
                throw new Exception("Invalid move");
            }

            if (IsPuzzleSolved())
            {
                timer1.Enabled = false;
                Beep();
                toolStripStatusLabel1.Text = "***Puzzle Solved***";
                MessageBox.Show("Puzzle solved");
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}