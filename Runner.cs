using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace ConsoleRunner
{

	public class RunnerNode {
		public int id;
		public bool active;
		public string command;
	}
	public delegate void CallBack_StatusChange(RunnerNode node);
	public delegate void CallBack_CommandStatusChange(int id);
	public delegate void CallBack_logEventCallback(int id, string data);

	public class RunnerCommand
    {
		private string command = null;
		private bool active = false;
		private int id;
		private Process app = null;
		private CallBack_CommandStatusChange statusCallback;

		private SortedDictionary<int, string> log = null;
		private int logMinIndex = 0;
		private int logMaxIndex = 0;
		private bool logActiveMode = false;
		private CallBack_logEventCallback logEventCallback;
		private int logLinesTostore = 100;
		private bool Terminated = false;

		public RunnerCommand(string cmd, int logLinesTostore, CallBack_CommandStatusChange appStopCallback, int id, CallBack_logEventCallback logEventCallback)
        {
			this.command = cmd;
			this.active = false;
			this.id = id;
			this.statusCallback = appStopCallback;
			this.log = logLinesTostore > 0 ? new SortedDictionary<int, string>() : null;
			this.logLinesTostore = logLinesTostore;
			this.logMaxIndex = -1;
			this.logMinIndex = 0;
			this.logEventCallback = logEventCallback;
		}

		~RunnerCommand()
		{
			if( this.active)
				this.Stop();
		}

		public bool Run() 
		{
			if (this.active)
				return true;

			this.log = this.logLinesTostore > 0 ? new SortedDictionary<int, string>() : null;
			this.logMaxIndex = -1;
			this.logMinIndex = 0;

			Task.Factory.StartNew(() => this.runConsole());

			this.active = true;
			return true;
		}

		public bool Stop()
		{
			if (!this.active)
				return true;

			this.app.Kill();
			this.app = null;
			this.Terminated = true;

			this.active = false;
			return true;
		}

		public string getCommand() {
			return this.command;
		}

		public bool getStatus() 
		{
			return this.active;
		}

		public bool isTerminated()
		{
			return this.Terminated;
		}

		public string[] getLog(bool setLogActiveMode = false)
		{
			string[] buf = new string[this.log.Count];
			int i = 0;
			foreach (string s in this.log.Values) {
				buf[i] = s;
				i++;
			}
			if (setLogActiveMode) {
				this.logActiveMode = true;
			}
			return buf;
		}

		public void setLogActiveMode(bool active = true) {
			this.logActiveMode = active;
		}

		private void runConsole() {
			int i = this.command.IndexOf(' ');
			string cmd;
			string args;
			if (i == -1)
			{
				cmd = this.command;
				args = "";
			}
			else
			{
				cmd = this.command.Substring(0, i);
				args = this.command.Substring(i + 1);
			}

			try
			{
				using (this.app = new Process())
				{
					this.app.StartInfo.UseShellExecute = false;
					this.app.StartInfo.FileName = cmd;
					this.app.StartInfo.Arguments = args;
					this.app.StartInfo.CreateNoWindow = true;
					if (this.logLinesTostore > 0)
					{
						this.app.StartInfo.RedirectStandardOutput = true;
						this.app.OutputDataReceived += new DataReceivedEventHandler((s, e) =>
						{
							this.logMaxIndex++;
							this.log.Add(this.logMaxIndex, e.Data);
							if (this.log.Count > this.logLinesTostore)
							{
								this.log.Remove(this.logMinIndex);
								this.logMinIndex++;
							}
							if (this.logActiveMode && this.logEventCallback != null && e.Data != null)
							{
								this.logEventCallback(this.id, e.Data);
							}
						});
						/* DISABLE COLLECTING STDERR DATA
						this.app.StartInfo.RedirectStandardError = true;
						this.app.ErrorDataReceived += new DataReceivedEventHandler((s, e) =>
						{
							this.logMaxIndex++;
							this.log.Add(this.logMaxIndex, e.Data);
							if (this.log.Count > this.logLinesTostore)
							{
								this.log.Remove(this.logMinIndex);
								this.logMinIndex++;
							}
							if (this.logActiveMode && this.logEventCallback != null && e.Data != null)
							{
								this.logEventCallback(this.id, e.Data);
							}
						});*/
					}
					this.app.Start();
					if (this.logLinesTostore > 0)
					{
						this.app.BeginOutputReadLine();
						// DISABLE COLLECTING STDERR DATA this.app.BeginErrorReadLine();
					}
					this.app.WaitForExit();
				}
			}
			catch (Exception)
			{
				//Console.WriteLine(e.Message);
			}
			this.app = null;
			this.active = false;
			this.Terminated = true;
			this.statusCallback(this.id);
		}

	}

	public class Runner
	{
		SortedDictionary<int, RunnerCommand> cmds = new SortedDictionary<int, RunnerCommand> ();
		int lastIndex = -1;
		CallBack_StatusChange statusCallback;
		CallBack_logEventCallback logEventOuterCallback;
		string loadAndSaveFile = null;

		public Runner(string loadAndSaveFile = null)
		{
			this.loadAndSaveFile = loadAndSaveFile;
			if (loadAndSaveFile != null)
				this.LoadFromFile();
		}

		public void setCallbacks(CallBack_StatusChange statusCallback, CallBack_logEventCallback logEventCallback) {
			this.statusCallback = statusCallback;
			this.logEventOuterCallback = logEventCallback;
		}

		public bool? getStatus(int id)
		{
			if (!this.cmds.ContainsKey(id))
				return null;
			return this.cmds[id].getStatus();
		}

		public bool isTerminated(int id) {
			if (!this.cmds.ContainsKey(id))
				return false;
			return this.cmds[id].isTerminated();
		}

		public int add(string cmd) 
		{
			this.lastIndex++;
			this.cmds.Add(lastIndex, new RunnerCommand(cmd,100, this.commandStopCallback, lastIndex, this.logEventCallback));
			if (loadAndSaveFile != null)
				this.SaveTofile();
			return this.lastIndex;
		}

		public bool start(int id) 
		{
			if (!this.cmds.ContainsKey(id))
				return false;
			var r = this.cmds[id].Run();
			this.statusCallback(new RunnerNode { id = id, active = this.cmds[id].getStatus(), command = this.cmds[id].getCommand() });
			return r;
		}

		public bool stop(int id)
		{
			if (!this.cmds.ContainsKey(id))
				return false;
			var r = this.cmds[id].Stop();
			this.statusCallback(new RunnerNode { id = id, active = this.cmds[id].getStatus(), command = this.cmds[id].getCommand() });
			return r;
		}

		public string[] getLog(int id, bool setActiveMode = false)
		{
			if (!this.cmds.ContainsKey(id))
				return null;
			return this.cmds[id].getLog(setActiveMode);
		}

		public void setLogActiveMode(int id, bool active) {
			if (!this.cmds.ContainsKey(id))
				return;
			this.cmds[id].setLogActiveMode(active);
		}

		public bool remove(int id)
		{
			if (!this.cmds.ContainsKey(id))
				return false;
			this.cmds[id].Stop();
			this.cmds.Remove(id);
			if (loadAndSaveFile != null)
				this.SaveTofile();
			return true;
		}

		public RunnerNode[] getList() {
			RunnerNode[] r = new RunnerNode[this.cmds.Count];

			int i = 0;
			foreach (KeyValuePair<int, RunnerCommand> kv in this.cmds)
			{
				RunnerNode n = new RunnerNode();
				n.active = kv.Value.getStatus();
				n.command = kv.Value.getCommand();
				n.id = kv.Key;
				r[i] = n;
				i++;
			}
			return r;
		}


		private void logEventCallback(int id, string data) {
			if(this.logEventOuterCallback != null)
				this.logEventOuterCallback(id, data);
		}

		private void commandStopCallback(int id) {
			this.statusCallback(new RunnerNode { id = id, active = this.cmds[id].getStatus(), command = this.cmds[id].getCommand() });
		}

		private void SaveTofile() {
			string[] buf = new string[this.cmds.Count];
			int i = 0;
			foreach (KeyValuePair<int, RunnerCommand> kv in this.cmds) {
				buf[i] = kv.Value.getCommand();
				i++;
			}
			System.IO.File.WriteAllLines(this.loadAndSaveFile, buf);
		}
		private void LoadFromFile()
		{
			if (!File.Exists(this.loadAndSaveFile)) {
				return;
			}
			string[] buf = File.ReadAllLines(this.loadAndSaveFile);
			foreach (string cmd in buf)
			{
				var cmdt = cmd.Trim();
				if (cmdt == "")
					continue;

				this.add(cmdt);
			}
		}

		~Runner() {
			foreach (KeyValuePair<int, RunnerCommand> keyValue in this.cmds)
			{
				keyValue.Value.Stop();
			}
			this.cmds.Clear();
		}
	}
}