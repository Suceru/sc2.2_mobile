using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Game
{
	public class TransferShExternalContentProvider : IExternalContentProvider, IDisposable
	{
		public string DisplayName => "transfer.sh";

		public string Description => "No login required";

		public bool SupportsListing => false;

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
			failure(new NotSupportedException());
		}

		public void Download(string path, CancellableProgress progress, Action<Stream> success, Action<Exception> failure)
		{
			failure(new NotSupportedException());
		}

		public void Upload(string path, Stream stream, CancellableProgress progress, Action<string> success, Action<Exception> failure)
		{
			try
			{
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				dictionary.Add("Content-Type", "application/octet-stream");
				WebManager.Put("https://transfer.sh/" + path, null, dictionary, stream, progress, delegate(byte[] result)
				{
					string obj2 = Encoding.UTF8.GetString(result, 0, result.Length).Trim();
					success(obj2);
				}, delegate(Exception error)
				{
					failure(error);
				});
			}
			catch (Exception obj)
			{
				failure(obj);
			}
		}

		public void Link(string path, CancellableProgress progress, Action<string> success, Action<Exception> failure)
		{
			failure(new NotSupportedException());
		}
	}
}
