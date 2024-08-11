using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ThreadPool01
{
	public partial class Form1 : Form
	{
		const string LogFile = "log.txt";

		int Cores = 1;
		PerformanceCounter CpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
		Process ProcessCpuUsage = null;

		public Form1()
		{
			InitializeComponent();

			string[] args = Environment.GetCommandLineArgs();
			if (args.Length == 1)
			{
				int workerMin;
				int ioMin;

				ThreadPool.GetMinThreads(out workerMin, out ioMin);

				Cores = workerMin;

				label2.Text = $"CPUコア数: {Cores}";

				for (int i = 1; i <= Cores; i++)
				{
					comboBox1.Items.Add(i);
				}
				comboBox1.SelectedIndex = Cores - 1;

				ProcessStartInfo psi = new ProcessStartInfo();
				psi.FileName = args[0];
				psi.Arguments = "abc";
				psi.WindowStyle = ProcessWindowStyle.Minimized;
				ProcessCpuUsage = Process.Start(psi);
			}
			else
			{
				this.Text = "CpuUsage";

				WriteCpuUsage();
			}


		}

		private void WriteCpuUsage()
		{
			if (File.Exists(LogFile))
			{
				File.Delete(LogFile);
			}

			System.Timers.Timer timer = new System.Timers.Timer();
			timer.Elapsed += new System.Timers.ElapsedEventHandler(MyTimer);
			timer.Interval = 500;
			timer.Start();
		}

		private void MyTimer(object sender, ElapsedEventArgs e)
		{
			DateTime dt = DateTime.Now;

			float cpu = CpuCounter.NextValue();

			string msg = $"{dt.ToString("yyyy/MM/dd HH:mm:ss.fff")}, {cpu}" + Environment.NewLine;

			File.AppendAllText(LogFile, msg);
		}

		private async void buttonDo_Click(object sender, EventArgs e)
		{
			buttonDo.Enabled = false;
			comboBox1.Enabled = false;

			SetCoreNum(comboBox1.SelectedIndex + 1);

			await F1();

			buttonDo.Enabled = true;
			comboBox1.Enabled = true;
		}

		private void SetCoreNum(int n)
		{
			int workerMin;
			int ioMin;
			int workerMax;
			int ioMax;

			ThreadPool.GetMinThreads(out workerMin, out ioMin);
			ThreadPool.GetMaxThreads(out workerMax, out ioMax);
			while (true)
			{
				ThreadPool.SetMinThreads(n, ioMin);
				ThreadPool.SetMaxThreads(n, ioMin);

				ThreadPool.GetMinThreads(out workerMin, out ioMin);
				ThreadPool.GetMaxThreads(out workerMax, out ioMax);
				if (workerMin == n && workerMax == n)
				{
					break;
				}
			}

			textBox1.AppendText($"使用するコア数を {n} に設定" + Environment.NewLine);
		}

		private async Task F1()
		{
			int nThreads = Cores;
			int max = 50_000_000;

			List<Task> tasks = new List<Task>();

			DateTime dtStart = DateTime.Now;

			for (int i = 0; i < nThreads; i++)
			{
				Task task = Task.Run(() =>
				{
					int idx = i;

					DateTime dt = DateTime.Now;
					string msg = $"{dt.ToString("HH:mm:ss.fff")} thread {Thread.CurrentThread.ManagedThreadId} start";
					this.Invoke(new Action(() =>
					{
						textBox1.AppendText(msg + Environment.NewLine);
					}));

					for (int j = idx; j < max; j += nThreads)
					{
						int x = (j + 1) * 2 + 1;
						IsPrime(x);
					}

					dt = DateTime.Now;
					msg = $"{dt.ToString("HH:m:ss.fff")} thread {Thread.CurrentThread.ManagedThreadId} end";
					this.Invoke(new Action(() =>
					{
						textBox1.AppendText(msg + Environment.NewLine);
					}));

				});
				tasks.Add(task);
			}

			await Task.WhenAll(tasks);

			DateTime dtEnd = DateTime.Now;
			TimeSpan span = dtEnd - dtStart;

			textBox1.AppendText($"実行時間: {span.ToString("hh':'mm':'ss'.'fff")}" + Environment.NewLine);

			float cpuUsage = GetCpuUsage(dtStart, dtEnd);
			textBox1.AppendText($"CPU使用率: {cpuUsage} %" + Environment.NewLine);
		}

		private float GetCpuUsage(DateTime dtStart, DateTime dtEnd)
		{
			float sum = 0;
			int count = 0;

			string[] lines = File.ReadAllLines(LogFile);

			foreach (string line in lines)
			{
				string[] strs = line.Split(',');

				DateTime dt = DateTime.Parse(strs[0]);

				if (dtStart < dt &&  dt < dtEnd)
				{
					sum += float.Parse(strs[1]);
					count++;

					// textBox1.AppendText(strs[1] + Environment.NewLine);
				}
			}

			return sum / count;
		}

		private bool IsPrime(int x)
		{
			if (x < 2)
			{
				return false;
			}
			if (x == 2)
			{
				return true;
			}
			if (x % 2 == 0)
			{
				return false;
			}

			double sqrt = Math.Sqrt(x);
			for (int i = 3; i < sqrt; i += 2)
			{
				if (x % i == 0)
				{
					return false;
				}
			}
			return true;
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (ProcessCpuUsage != null)
			{
				try
				{
					ProcessCpuUsage.CloseMainWindow();
					ProcessCpuUsage.Close();
				}
				catch
				{
				}
			}
		}
	}
}
