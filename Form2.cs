using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConsoleRunner
{
    public partial class Form2 : Form
    {
        private string cmd;
        private int cmdId;
        private Runner runner;
        public Form2()
        {
            InitializeComponent();
        }

        public void setParams(string cmd, int id, string[] logData, Runner runner) 
        {
            this.Text = cmd;
            this.cmd = cmd;
            this.cmdId = id;
            this.richTextBox1.Lines = logData;
            this.richTextBox1.AppendText("\r\n");
            this.richTextBox1.ScrollToCaret();
            this.runner = runner;
        }

        public void gotNewConsoleData(int id, string data) {
            if (this.cmdId != id)
                return;
            this.richTextBox1.AppendText(data + "\r\n");
            this.richTextBox1.SelectionStart = this.richTextBox1.Text.Length;
            this.richTextBox1.ScrollToCaret();
        }

        public void processTerminated(int id) {
            if (this.cmdId != id || this.cmdId == -1)
                return;
            string termMes = "<process terminated>";
            this.richTextBox1.AppendText("\r\n"+ termMes + "\r\n");
            this.richTextBox1.Select(this.richTextBox1.Text.IndexOf(termMes), termMes.Length);
            this.richTextBox1.SelectionColor = Color.Red;
            this.richTextBox1.SelectionStart = this.richTextBox1.Text.Length;
            this.richTextBox1.ScrollToCaret();
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.runner.setLogActiveMode(this.cmdId, false);
            this.cmdId = -1;
        }
    }
}
