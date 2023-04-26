using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Collections;
using System.IO;

namespace _002_Sudoku
{
    public partial class Form1 : Form
    {
        //Size gridSize = new Size(10, 10);
        Brush backgroundBrush;
        int thickPenWidth = 5;
        int thinPenWidth = 1;
        Pen penThin;
        Pen penThick;
        Font font = new Font("Calibri", 9);
        Font fontLarge = new Font("Calibri", 18);
        StringFormat stringFormat = new StringFormat();
        int windowDimension = 675; // should be divisible by 27
        //the width and height of the various rectangles
        int tinySquare, smallSquare, bigSquare;
        Point contextMouse = new Point();

        int[,] data = new int[9, 9];

        // a data store for all thesudoku problems
        enum problemTypes { easy = 0, intermediate = 1, hard = 2, expert = 3 };
        ArrayList[] problems = new ArrayList[4];

        public Form1()
        {
            InitializeComponent();

            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;

            //initialize all basic parameters

            penThin = new Pen(Color.Black, thinPenWidth);
            penThick = new Pen(Color.Black, thickPenWidth);

            //set the window size
            this.Width = windowDimension + 
                2 * SystemInformation.FixedFrameBorderSize.Width + 
                2 * thickPenWidth;
            this.Height = windowDimension +
                2 * SystemInformation.FixedFrameBorderSize.Height + 
                2 * thickPenWidth + menuStrip1.Height + 
                statusStrip1.Height + 
                SystemInformation.CaptionHeight;
            tinySquare = windowDimension / 27;
            smallSquare = windowDimension / 9;
            bigSquare = windowDimension / 3;
            backgroundBrush = new LinearGradientBrush(ClientRectangle, Color.AliceBlue, Color.CornflowerBlue, 39);

            //read in all the sudoku problems
            //place them into an arraylist
            //set up the data store
            for (int i = 0; i < 4; i++)
                problems[i] = new ArrayList();
            //read in all the problems
            Assembly assembly = Assembly.GetExecutingAssembly();
            StreamReader problemFile = new StreamReader(assembly.GetManifestResourceStream("_002_Sudoku.SudokuProblems.txt"));
            while (!problemFile.EndOfStream)
            {
                string line = problemFile.ReadLine();
                //split the line between the difficulty level and the problem itself
                string[] info = line.Split(';');
                if(info.Length == 2)
                {
                    switch(info[0])
                    {
                        case "Easy": problems[(int)problemTypes.easy].Add(info[1]); break;
                        case "Intermediate": problems[(int)problemTypes.intermediate].Add(info[1]); break;
                        case "Hard": problems[(int)problemTypes.hard].Add(info[1]); break;
                        case "Expert": problems[(int)problemTypes.expert].Add(info[1]); break;
                    }
                }
            }
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics gr = e.Graphics;
            gr.TranslateTransform(0, menuStrip1.Height);
            gr.FillRectangle(backgroundBrush, ClientRectangle);

            //draw the small rectangles
            for (int i = 0; i < 9; i++)           
                for (int j = 0; j < 9; j++)                
                    gr.DrawRectangle(penThin, i * smallSquare, j * smallSquare, smallSquare, smallSquare);
                            
            //draw the big rectangle
            for (int i = 0; i < 3; i++)          
                for (int j = 0; j < 3; j++)                
                    gr.DrawRectangle(penThick, i * bigSquare, j * bigSquare, bigSquare, bigSquare);

            //draw the numbers to the screen
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                {
                    if (data[i, j] == 0)
                    {
                        //display the hint values
                        //first eliminate any values appearing in this cell's
                        //row, column, or large square
                        string integers = EliminateOptions(i, j);

                        //now display the remaining values
                        for (int x = 0; x < 3; x++)
                            for (int y = 0; y < 3; y++)
                            {
                                Rectangle rect = new Rectangle(
                                    i * smallSquare + x * tinySquare,
                                    j * smallSquare + y * tinySquare,
                                    tinySquare, tinySquare);
                                int index = x + 3 * y;
                                if (toggleHintsToolStripMenuItem.Checked)
                                    gr.DrawString(integers.Substring(index, 1), font, Brushes.Black, rect, stringFormat);
                            }
                    }

                    else
                    {
                        //draw out the data value using the big font in the middle of the rectangle
                        Rectangle rect = new Rectangle(smallSquare*i, smallSquare*j, smallSquare, smallSquare);
                        gr.DrawString(data[i, j].ToString(), fontLarge, Brushes.Black, rect, stringFormat);
                    }
                }             
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePt = new Point(e.X, e.Y - menuStrip1.Height);
            // which large cell are we in?
            Point bigCell = new Point(mousePt.X / bigSquare, mousePt.Y / bigSquare);
            //which small cell are we in?
            Point smallCell = new Point((mousePt.X / smallSquare)%3, 
                (mousePt.Y / smallSquare)%3);
            //which tiny cell are we in?
            Point tinyCell = new Point((mousePt.X / tinySquare)%3, 
                (mousePt.Y / tinySquare)%3);
            int index = tinyCell.X + 3 * tinyCell.Y; //index into the integer array

            toolStripStatusLabel1.Text = "Mouse: " + mousePt.ToString();
            toolStripStatusLabel1.Text += " BigSq: " + bigCell.ToString();
            toolStripStatusLabel1.Text += " SmallSq: " + smallCell.ToString();
            toolStripStatusLabel1.Text += " TinySq: " + tinyCell.ToString();
            toolStripStatusLabel1.Text += " Value: " + (index + 1).ToString();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                return;

            if (!toggleHintsToolStripMenuItem.Checked)
                return;

            Point mousePt = new Point(e.X, e.Y - menuStrip1.Height);
            // which large cell are we in?
            Point bigCell = new Point(mousePt.X / bigSquare, mousePt.Y / bigSquare);
            //which small cell are we in?
            Point smallCell = new Point((mousePt.X / smallSquare) % 3,
                (mousePt.Y / smallSquare) % 3);
            //which tiny cell are we in?
            Point tinyCell = new Point((mousePt.X / tinySquare) % 3,
                (mousePt.Y / tinySquare) % 3);
            int index = tinyCell.X + 3 * tinyCell.Y; //index into the integer array
            string integers = EliminateOptions(smallCell.X + bigCell.X * 3, smallCell.Y + bigCell.Y * 3);
            if (integers[index] == ' ')
                return;

            //int value = Convert.ToInt32("123456789".Substring(index, 1));
            //now place that value into the correct data array location
            if(data[smallCell.X + bigCell.X * 3, smallCell.Y + bigCell.Y * 3] == 0)
                data[smallCell.X + bigCell.X * 3, smallCell.Y + bigCell.Y * 3] = (index + 1);
            this.Invalidate();

            CheckWin();
        }

        private void Form1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //idk if any of this is right, i'm going off instinct
            Point mousePt = new Point(e.X, e.Y - menuStrip1.Height);
            // which large cell are we in?
            Point bigCell = new Point(mousePt.X / bigSquare, mousePt.Y / bigSquare);
            //which small cell are we in?
            Point smallCell = new Point((mousePt.X / smallSquare) % 3,
                (mousePt.Y / smallSquare) % 3);
            //which tiny cell are we in?
            Point tinyCell = new Point((mousePt.X / tinySquare) % 3,
                (mousePt.Y / tinySquare) % 3);

            int index = tinyCell.X + 3 * tinyCell.Y; //index into the integer array
            if (data[smallCell.X + bigCell.X * 3, smallCell.Y + bigCell.Y * 3] != 0)
                data[smallCell.X + bigCell.X * 3, smallCell.Y + bigCell.Y * 3] = 0;
            this.Invalidate();
        }
        /// <summary>
        /// This function eliminates from a base string of "123456789"
        /// those values already used by the user in conflict with the passed
        /// i,j coordinate location
        /// </summary>
        /// <param name="i">The x-direction coordinate</param>
        /// <param name="j">The y-direction coordinate</param>
        /// <returns>The integer value options available for the i, j cell.</returns>
        private string EliminateOptions(int i, int j)
        {
            string integers = "123456789";
            for (int y = 0; y < 9; y++)
            {
                //process the vertical column
                if (data[i, y] != 0)
                    integers = integers.Replace(data[i, y].ToString(), " ");

                //process the horizontal row
                if (data[y, j]!= 0)
                    integers = integers.Replace(data[y, j].ToString(), " ");
            }

            //process the big cell
            //the big cell location is:
            int x_cell = i / 3;
            int y_cell = j / 3;
            //now perform a double for loop 3x3 to process the 9 subcells in the big cell
            for(i=0; i<3; i++)
                for(j=0; j<3; j++)
                    if(data[i + x_cell*3, j + y_cell*3] != 0)
                        integers = integers.Replace(data[i + x_cell * 3, j + y_cell * 3].ToString(), " ");

            return integers;
        }

        private void easyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Random rand = new Random();
            int index = rand.Next(problems[(int)problemTypes.easy].Count);
            string problemSelected = (string)problems[(int)problemTypes.easy][index];
            //now place the problem into the data array
            LoadProblem(problemSelected);
            this.Invalidate();
        }

        private void intermediateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Random rand = new Random();
            int index = rand.Next(problems[(int)problemTypes.intermediate].Count);
            string problemSelected = (string)problems[(int)problemTypes.intermediate][index];
            //now place the problem into the data array
            LoadProblem(problemSelected);
            this.Invalidate();
        }

        private void hardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Random rand = new Random();
            int index = rand.Next(problems[(int)problemTypes.hard].Count);
            string problemSelected = (string)problems[(int)problemTypes.hard][index];
            //now place the problem into the data array
            LoadProblem(problemSelected);
            this.Invalidate();
        }

        private void expertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Random rand = new Random();
            int index = rand.Next(problems[(int)problemTypes.expert].Count);
            string problemSelected = (string)problems[(int)problemTypes.expert][index];
            //now place the problem into the data array
            LoadProblem(problemSelected);
            this.Invalidate();
        }

        private void LoadProblem(string problemSelected)
        {
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                {
                    int key = j * 9 + i;
                    string value = problemSelected.Substring(key, 1);
                    if (value == ".") data[i, j] = 0;
                    else data[i, j] = Convert.ToInt32(value);
                }
        }

        private void AutoFillCells()
        {
            //in here, we want to "automatically" fill the cells with only one number left inside
            bool repeat = true;
            while(repeat)
            {
                repeat = false;
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        if (data[i, j] != 0)
                            continue;

                        string integers = EliminateOptions(i, j);
                        integers = integers.Replace(" ", "");

                        if (integers.Length == 1)
                        {
                            data[i, j] = Convert.ToInt32(integers);
                            repeat = true;
                        }
                    }

                }
            }
        }

        private void CheckWin()
        {
            //bool win = true;
            int sumOnBoard = 0;
            int sum = 405;

            //check to see if the player has "won" the game
            //add all values on the board to a variable
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    sumOnBoard += data[i,j];

            //does the sum on the board match what it should add up to
            if (sumOnBoard == sum)
            {
                DialogResult d = MessageBox.Show("Congrats! You won! Play again?", "", MessageBoxButtons.YesNo);
                if (d == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(Application.ExecutablePath); //start the new instance of the application
                    this.Close();
                }

                else if (d == DialogResult.No)
                    Application.Exit();

            }


        }

        private void solveSinglesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AutoFillCells();
            this.Invalidate();
            CheckWin();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            //we need to know where on the grid the mouse is located
            contextMouse = Cursor.Position; //this is screen coordinate
            contextMouse = this.PointToClient(contextMouse);
            //adjust for the menu height
            contextMouse.Y -= menuStrip1.Height;

            //which cell are we in?
            Point bigCell = new Point(contextMouse.X / bigSquare, contextMouse.Y / bigSquare);
            //which small cell are we in?
            Point smallCell = new Point((contextMouse.X / smallSquare) % 3,
                (contextMouse.Y / smallSquare) % 3);
            //which tiny cell are we in?
            Point tinyCell = new Point((contextMouse.X / tinySquare) % 3,
                (contextMouse.Y / tinySquare) % 3);
            //int index = tinyCell.X + 3 * tinyCell.Y; //index into the integer array
            string integers = EliminateOptions(smallCell.X + bigCell.X * 3, smallCell.Y + bigCell.Y * 3);

            //now populate the context menu with the appropriate options
            contextMenuStrip1.Items.Clear();
            for(int i = 0; i <9; i++)
                if (integers[i] != ' ')
                    contextMenuStrip1.Items.Add(integers[i].ToString(), null, NumberSelection_Click);
        }

        private void NumberSelection_Click(object sender, EventArgs e)
        {
            // get the value selected from the menu
            ToolStripMenuItem tsmi = (ToolStripMenuItem)sender;
            int value = Convert.ToInt32(tsmi.Text);

            //which cell are we in?
            Point bigCell = new Point(contextMouse.X / bigSquare, contextMouse.Y / bigSquare);
            //which small cell are we in?
            Point smallCell = new Point((contextMouse.X / smallSquare) % 3,
                (contextMouse.Y / smallSquare) % 3);
            //which tiny cell are we in?
            Point tinyCell = new Point((contextMouse.X / tinySquare) % 3,
                (contextMouse.Y / tinySquare) % 3);

            data[smallCell.X + bigCell.X * 3, smallCell.Y + bigCell.Y * 3] = value;
            this.Invalidate();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //about box
            AboutBox1 aboutBox1 = new AboutBox1();
            aboutBox1.ShowDialog();
        }

        //exit
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void toggleHintsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //where the numbers should be displayed, 1-9 in each small cell
            toggleHintsToolStripMenuItem.Checked = !toggleHintsToolStripMenuItem.Checked;
            this.Invalidate();
        }
    }
}

