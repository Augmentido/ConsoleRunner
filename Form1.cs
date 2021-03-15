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
    public partial class Form1 : Form
    {
        Runner runner = null;
        RunnerNode[] runNodes = { };
        string selectedNode = "";
        private delegate void SafeCallDelegate(RunnerNode node);
        private delegate void SafeCallDelegate2(int id, string data);
        Form2 consoleForm;

        public Form1(Runner cmdRunner)
        {
            runner = cmdRunner;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            runner.setCallbacks(this.runnerStatusChange, this.runnerLogEvent);
            this.reloadList();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string cmd = textBox1.Text.Trim();
            if (cmd == "")
                return;
            runner.add(cmd);
            this.reloadList();
            this.listView1.Focus();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var fi = this.listView1.SelectedItems;
            if (fi.Count > 0)
            {
                button4.Enabled = true;
                var st = this.runner.getStatus(int.Parse(fi[0].Text));
                if (st == true)
                {
                    button2.Enabled = true;
                    button5.Enabled = true;
                    button3.Enabled = false;
                    button4.Enabled = false;
                }
                else if (st == false)
                {
                    button2.Enabled = false;
                    button5.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                }
            }
            else {
                // no selected items
                button2.Enabled = false;
                button5.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var fi = this.listView1.SelectedItems;
            this.listView1.Focus();
            if (fi.Count > 0) {
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                fi[0].SubItems[1].Text = "...";
                if (this.runner.start(int.Parse(fi[0].Text)))
                {
                    // Тут можно что-то выполнить если запрос на запуск прошел успешно.
                    // ! в момент выполнения этого кода приложение еще не запущено!
                    // По факту запуска будет вызван каллбек.
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var fi = this.listView1.SelectedItems;
            this.listView1.Focus();
            if (fi.Count > 0)
            {
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                fi[0].SubItems[1].Text = "...";
                if (this.runner.stop(int.Parse(fi[0].Text)))
                {
                    // Тут можно что-то выполнить если запрос на остановку прошел успешно.
                    // ! в момент выполнения этого кода приложение еще не остановлено!
                    // По факту остановки будет вызван каллбек.
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var fi = this.listView1.SelectedItems;
            this.listView1.Focus();
            if (fi.Count > 0)
            {
                button2.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
                button5.Enabled = false;
                this.runner.remove(int.Parse(fi[0].Text));
                this.reloadList();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var fi = this.listView1.SelectedItems;
            this.listView1.Focus();
            if (fi.Count == 0)
            {
                return;
            }
            consoleForm = new Form2();
            int id = int.Parse(fi[0].Text);
            consoleForm.setParams(fi[0].SubItems[2].Text, id, this.runner.getLog(id,true), this.runner);
            if(fi[0].SubItems[1].Text == " - " && this.runner.isTerminated(id))
                this.consoleForm.processTerminated(id);

            consoleForm.Show();
        }

        public void runnerLogEvent(int id, string data)
        {
            if (this.listView1.InvokeRequired)
            {
                var d = new SafeCallDelegate2(runnerLogEvent);
                this.consoleForm.Invoke(d, new object[] { id, data });
            }
            else
            {
                consoleForm.gotNewConsoleData(id, data);
            }
        }

        public void runnerStatusChange(RunnerNode node) {
            if (this.listView1.InvokeRequired)
            {
                var d = new SafeCallDelegate(runnerStatusChange);
                this.listView1.Invoke(d, new object[] { node });
            }
            else
            {
                string nodeId = node.id.ToString();
                ListViewItem fi = null;
                for (int i = 0; i < listView1.Items.Count; i++)
                {
                    if (listView1.Items[i].Text == nodeId)
                    {
                        fi = this.listView1.Items[i];
                        break;
                    }
                }
                if (fi == null) {
                    return;
                }
                var fo = this.listView1.SelectedItems;
                if (node.active)
                {
                    button2.Enabled = true;
                    button5.Enabled = true;
                    button3.Enabled = false;
                    button4.Enabled = false;
                    fi.SubItems[1].Text = "YES";
                }
                else
                {
                    button2.Enabled = false;
                    button5.Enabled = true;
                    button3.Enabled = true;
                    button4.Enabled = true;
                    fi.SubItems[1].Text = " - ";
                    if (this.consoleForm != null) {
                        this.consoleForm.processTerminated(node.id);
                    }
                }
            }
        }

        private void reloadList()
        {
            this.runNodes = this.runner.getList();
            var fi = listView1.FocusedItem;
            if (fi != null)
            {
                this.selectedNode = fi.Text;
            }
            else
            {
                this.selectedNode = "";
            }

            listView1.Items.Clear();
            for (int i = 0; i < this.runNodes.Length; i++)
            {
                ListViewItem listItem = new ListViewItem(this.runNodes[i].id.ToString());
                listItem.SubItems.Add(this.runNodes[i].active ? "YES" : " - ");
                listItem.SubItems.Add(this.runNodes[i].command);
                listView1.Items.Add(listItem);
            }
            if (this.selectedNode != "")
            {
                for (int i = 0; i < listView1.Items.Count; i++)
                {
                    if (listView1.Items[i].Text == this.selectedNode)
                    {
                        listView1.Items[i].Selected = true;
                        break;
                    }
                }
            }
        }

    }
}
