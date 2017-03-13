using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Shell.Execute
{
	public class ProgramLauncher
	{
		public event EventHandler StandardOutput;
		public event EventHandler ErrorOutput;

		private Process proc;
		private bool stopping;
		private Thread stdThread;
		private Thread errThread;
		private string exe;

		public ProgramLauncher()
		{
		}

		public ProgramLauncher(string exe, string args)
		{
			Launch(exe, args);
		}

		public void Launch(string exe, string args)
		{
			this.exe = exe;
			stopping = false;
			ProcessStartInfo oInfo = new ProcessStartInfo(exe, args);
			oInfo.UseShellExecute = false;
			oInfo.CreateNoWindow = true;

			oInfo.RedirectStandardOutput = true;
			oInfo.RedirectStandardError = true;

			StreamReader srOutput = null;
			StreamReader srError = null;

			proc = Process.Start(oInfo);
			Debug.WriteLine("Launched Program: {0}", exe);
			srOutput = proc.StandardOutput;
			srError = proc.StandardError;
			stdThread = new Thread(new ThreadStart(() =>
			{
				while (!(stopping || proc.HasExited))
				{
					string str = srOutput.ReadLine();
					if (StandardOutput != null)
					{
						StandardOutput(str, null);
					}
				}
			}));

			errThread = new Thread(new ThreadStart(() =>
			{
				while (!(stopping || proc.HasExited))
				{
					string str = srError.ReadLine();
					if (ErrorOutput != null)
					{
						ErrorOutput(str, null);
					}
				}
			}));

			errThread.Start();
			stdThread.Start();
		}

		public void Kill()
		{
			Debug.WriteLine("Killing Program: {0}", exe);
			stopping = true;
			int counter = 0;
			while (stdThread.IsAlive || errThread.IsAlive)
			{
				Thread.Sleep(100);
				counter++;
				if (counter > 50)
				{
					if (stdThread.IsAlive)
					{
						stdThread.Abort();
						Debug.WriteLine("Standard Output Thread Force Killed.");
					}
					if (errThread.IsAlive)
					{
						errThread.Abort();
						Debug.WriteLine("Error Output Thread Force Killed.");
					}
				}
			}
			if (!proc.HasExited)
			{
				proc.Kill();
				Debug.WriteLine("Force killing program: {0}", exe);
			}
			proc.Close();
			Debug.WriteLine("Closed program: {0}", exe);
		}

		public static int Execute(string exe, string args)
		{
			int exitCode = -1;
			ProcessStartInfo oInfo = new ProcessStartInfo(exe, args);
			oInfo.UseShellExecute = false;
			oInfo.CreateNoWindow = true;

			Process proc = Process.Start(oInfo);

			proc.WaitForExit();
			exitCode = proc.ExitCode;
			proc.Close();
			return exitCode;
		}
	}
}
