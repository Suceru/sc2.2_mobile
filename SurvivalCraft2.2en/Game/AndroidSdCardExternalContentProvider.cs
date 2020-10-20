using Android.Content;
using Engine;
using Game;
using Java.IO;
using System;
using System.IO;
using System.Threading;

public class AndroidSdCardExternalContentProvider : IExternalContentProvider, IDisposable
{
	private string m_rootDirectory;

	public string DisplayName => "SD Card";

	public string Description
	{
		get
		{
			InitializeFilesystemAccess();
			return m_rootDirectory;
		}
	}

	public bool SupportsListing => true;

	public bool SupportsLinks => false;

	public bool RequiresLogin => false;

	public bool IsLoggedIn => true;

	public void Dispose()
	{
	}

	public void Login(CancellableProgress progress, Action success, Action<Exception> failure)
	{
		failure(new NotSupportedException());
	}

	public void Logout()
	{
		throw new NotSupportedException();
	}

	public void List(string path, CancellableProgress progress, Action<ExternalContentEntry> success, Action<Exception> failure)
	{
		ExternalContentEntry entry = default(ExternalContentEntry);
		Exception e = default(Exception);
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				InitializeFilesystemAccess();
				string internalPath = ToInternalPath(path);
				entry = GetDirectoryEntry(internalPath, scanContents: true);
				Dispatcher.Dispatch(delegate
				{
					success(entry);
				});
			}
			catch (Exception ex)
			{
				e = ex;
				Dispatcher.Dispatch(delegate
				{
					failure(e);
				});
			}
		});
	}

	public void Download(string path, CancellableProgress progress, Action<Stream> success, Action<Exception> failure)
	{
		FileStream stream = default(FileStream);
		Exception e = default(Exception);
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				InitializeFilesystemAccess();
				string path2 = ToInternalPath(path);
				stream = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.Read);
				Dispatcher.Dispatch(delegate
				{
					success(stream);
				});
			}
			catch (Exception ex)
			{
				e = ex;
				Dispatcher.Dispatch(delegate
				{
					failure(e);
				});
			}
		});
	}

	public void Upload(string path, Stream stream, CancellableProgress progress, Action<string> success, Action<Exception> failure)
	{
		Exception e = default(Exception);
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				InitializeFilesystemAccess();
				string uniquePath = GetUniquePath(ToInternalPath(path));
				Directory.CreateDirectory(Path.GetDirectoryName(uniquePath));
				using (FileStream destination = new FileStream(uniquePath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					stream.CopyTo(destination);
				}
				Dispatcher.Dispatch(delegate
				{
					success(null);
				});
			}
			catch (Exception ex)
			{
				e = ex;
				Dispatcher.Dispatch(delegate
				{
					failure(e);
				});
			}
		});
	}

	public void Link(string path, CancellableProgress progress, Action<string> success, Action<Exception> failure)
	{
		failure(new NotSupportedException());
	}

	private ExternalContentEntry GetDirectoryEntry(string internalPath, bool scanContents)
	{
		ExternalContentEntry externalContentEntry = new ExternalContentEntry();
		externalContentEntry.Type = ExternalContentType.Directory;
		externalContentEntry.Path = ToExternalPath(internalPath);
		externalContentEntry.Time = new DateTime(1970, 1, 1);
		if (scanContents)
		{
			string[] directories = Directory.GetDirectories(internalPath);
			foreach (string internalPath2 in directories)
			{
				externalContentEntry.ChildEntries.Add(GetDirectoryEntry(internalPath2, scanContents: false));
			}
			directories = Directory.GetFiles(internalPath);
			foreach (string text in directories)
			{
				FileInfo fileInfo = new FileInfo(text);
				ExternalContentEntry externalContentEntry2 = new ExternalContentEntry();
				externalContentEntry2.Type = ExternalContentManager.ExtensionToType(Path.GetExtension(text));
				externalContentEntry2.Path = ToExternalPath(text);
				externalContentEntry2.Size = fileInfo.Length;
				externalContentEntry2.Time = fileInfo.CreationTime;
				externalContentEntry.ChildEntries.Add(externalContentEntry2);
			}
		}
		return externalContentEntry;
	}

	private static string GetUniquePath(string path)
	{
		int num = 1;
		string text = path;
		while (System.IO.File.Exists(text) && num < 1000)
		{
			string path2 = Path.GetFileNameWithoutExtension(path) + num.ToString() + Path.GetExtension(path);
			text = Path.Combine(Path.GetDirectoryName(path), path2);
			num++;
		}
		return text;
	}

	private string ToExternalPath(string internalPath)
	{
		return Path.GetFullPath(internalPath).Remove(0, m_rootDirectory.Length);
	}

	private string ToInternalPath(string externalPath)
	{
		return Path.GetFullPath(Path.Combine(m_rootDirectory, externalPath.TrimStart(new char[1]
		{
			'/'
		})));
	}

	private void InitializeFilesystemAccess()
	{
		Java.IO.File externalFilesDir = ((Context)Window.Activity).GetExternalFilesDir((string)null);
		m_rootDirectory = ((externalFilesDir != null) ? externalFilesDir.AbsolutePath : "/data");
	}
}
