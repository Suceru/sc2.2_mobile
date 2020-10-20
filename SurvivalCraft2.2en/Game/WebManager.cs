using Android.Net;
using Engine;
using SimpleJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
	public static class WebManager
	{
		public class ProgressHttpContent : HttpContent
		{
			public Stream m_sourceStream;

			public CancellableProgress m_progress;

			public ProgressHttpContent(Stream sourceStream, CancellableProgress progress)
			{
				m_sourceStream = sourceStream;
				m_progress = (progress ?? new CancellableProgress());
			}

			protected override bool TryComputeLength(out long length)
			{
				length = m_sourceStream.Length;
				return true;
			}

			protected override async Task SerializeToStreamAsync(Stream targetStream, TransportContext context)
			{
				byte[] buffer = new byte[1024];
				long written = 0L;
				while (true)
				{
					m_progress.Total = m_sourceStream.Length;
					m_progress.Completed = written;
					if (m_progress.CancellationToken.IsCancellationRequested)
					{
						break;
					}
					int read = m_sourceStream.Read(buffer, 0, buffer.Length);
					if (read > 0)
					{
						await targetStream.WriteAsync(buffer, 0, read, m_progress.CancellationToken);
						written += read;
					}
					if (read <= 0)
					{
						return;
					}
				}
				throw new OperationCanceledException("Operation cancelled.");
			}
		}

		public static bool IsInternetConnectionAvailable()
		{
			try
			{
				return ((ConnectivityManager)Window.Activity.GetSystemService("connectivity")).ActiveNetworkInfo?.IsConnected ?? false;
			}
			catch (Exception e)
			{
				Log.Warning(ExceptionManager.MakeFullErrorMessage("Could not check internet connection availability.", e));
			}
			return true;
		}

		public static void Get(string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			MemoryStream targetStream = default(MemoryStream);
			Exception e = default(Exception);
			Task.Run(async delegate
			{
				_ = 3;
				try
				{
					progress = (progress ?? new CancellableProgress());
					if (!IsInternetConnectionAvailable())
					{
						throw new InvalidOperationException("Internet connection is unavailable.");
					}
					using (HttpClient client = new HttpClient())
					{
						System.Uri requestUri = (parameters != null && parameters.Count > 0) ? new System.Uri($"{address}?{UrlParametersToString(parameters)}") : new System.Uri(address);
						client.DefaultRequestHeaders.Referrer = new System.Uri(address);
						if (headers != null)
						{
							foreach (KeyValuePair<string, string> header in headers)
							{
								client.DefaultRequestHeaders.Add(header.Key, header.Value);
							}
						}
						HttpResponseMessage responseMessage = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, progress.CancellationToken);
						await VerifyResponse(responseMessage);
						long? contentLength = responseMessage.Content.Headers.ContentLength;
						progress.Total = contentLength.GetValueOrDefault();
						using (Stream responseStream = await responseMessage.Content.ReadAsStreamAsync())
						{
							targetStream = new MemoryStream();
							try
							{
								long written = 0L;
								byte[] buffer = new byte[1024];
								int num;
								do
								{
									num = await responseStream.ReadAsync(buffer, 0, buffer.Length, progress.CancellationToken);
									if (num > 0)
									{
										targetStream.Write(buffer, 0, num);
										written += num;
										progress.Completed = written;
									}
								}
								while (num > 0);
								if (success != null)
								{
									Dispatcher.Dispatch(delegate
									{
										success(targetStream.ToArray());
									});
								}
							}
							finally
							{
								if (targetStream != null)
								{
									((IDisposable)targetStream).Dispose();
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					e = ex;
					Log.Error(ExceptionManager.MakeFullErrorMessage(e));
					if (failure != null)
					{
						Dispatcher.Dispatch(delegate
						{
							failure(e);
						});
					}
				}
			});
		}

		public static void Put(string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, Stream data, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			PutOrPost(isPost: false, address, parameters, headers, data, progress, success, failure);
		}

		public static void Post(string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, Stream data, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			PutOrPost(isPost: true, address, parameters, headers, data, progress, success, failure);
		}

		public static string UrlParametersToString(Dictionary<string, string> values)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string value = string.Empty;
			foreach (KeyValuePair<string, string> value2 in values)
			{
				stringBuilder.Append(value);
				value = "&";
				stringBuilder.Append(System.Uri.EscapeDataString(value2.Key));
				stringBuilder.Append('=');
				if (!string.IsNullOrEmpty(value2.Value))
				{
					stringBuilder.Append(System.Uri.EscapeDataString(value2.Value));
				}
			}
			return stringBuilder.ToString();
		}

		public static byte[] UrlParametersToBytes(Dictionary<string, string> values)
		{
			return Encoding.UTF8.GetBytes(UrlParametersToString(values));
		}

		public static MemoryStream UrlParametersToStream(Dictionary<string, string> values)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(UrlParametersToString(values)));
		}

		public static Dictionary<string, string> UrlParametersFromString(string s)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			string[] array = s.Split(new char[1]
			{
				'&'
			}, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string[] array2 = System.Uri.UnescapeDataString(array[i]).Split('=', StringSplitOptions.None);
				if (array2.Length == 2)
				{
					dictionary[array2[0]] = array2[1];
				}
			}
			return dictionary;
		}

		public static Dictionary<string, string> UrlParametersFromBytes(byte[] bytes)
		{
			return UrlParametersFromString(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
		}

		public static object JsonFromString(string s)
		{
			return SimpleJson.SimpleJson.DeserializeObject(s);
		}

		public static object JsonFromBytes(byte[] bytes)
		{
			return JsonFromString(Encoding.UTF8.GetString(bytes, 0, bytes.Length));
		}

		public static void PutOrPost(bool isPost, string address, Dictionary<string, string> parameters, Dictionary<string, string> headers, Stream data, CancellableProgress progress, Action<byte[]> success, Action<Exception> failure)
		{
			byte[] responseData = default(byte[]);
			Exception e = default(Exception);
			Task.Run(async delegate
			{
				_ = 5;
				try
				{
					if (!IsInternetConnectionAvailable())
					{
						throw new InvalidOperationException("Internet connection is unavailable.");
					}
					using (HttpClient client = new HttpClient())
					{
						Dictionary<string, string> dictionary = new Dictionary<string, string>();
						if (headers != null)
						{
							foreach (KeyValuePair<string, string> header in headers)
							{
								if (!client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value))
								{
									dictionary.Add(header.Key, header.Value);
								}
							}
						}
						System.Uri requestUri = (parameters != null && parameters.Count > 0) ? new System.Uri($"{address}?{UrlParametersToString(parameters)}") : new System.Uri(address);
						HttpContent httpContent = (progress != null) ? ((HttpContent)new ProgressHttpContent(data, progress)) : ((HttpContent)new StreamContent(data));
						foreach (KeyValuePair<string, string> item in dictionary)
						{
							httpContent.Headers.Add(item.Key, item.Value);
						}
						HttpResponseMessage responseMessage = isPost ? ((progress == null) ? (await client.PostAsync(requestUri, httpContent)) : (await client.PostAsync(requestUri, httpContent, progress.CancellationToken))) : ((progress == null) ? (await client.PutAsync(requestUri, httpContent)) : (await client.PutAsync(requestUri, httpContent, progress.CancellationToken)));
						await VerifyResponse(responseMessage);
						responseData = await responseMessage.Content.ReadAsByteArrayAsync();
						if (success != null)
						{
							Dispatcher.Dispatch(delegate
							{
								success(responseData);
							});
						}
					}
				}
				catch (Exception ex)
				{
					e = ex;
					Log.Error(ExceptionManager.MakeFullErrorMessage(e));
					if (failure != null)
					{
						Dispatcher.Dispatch(delegate
						{
							failure(e);
						});
					}
				}
			});
		}

		public static async Task VerifyResponse(HttpResponseMessage message)
		{
			if (!message.IsSuccessStatusCode)
			{
				string responseText = string.Empty;
				try
				{
					responseText = await message.Content.ReadAsStringAsync();
				}
				catch
				{
				}
				throw new InvalidOperationException($"{message.StatusCode.ToString()} ({(int)message.StatusCode})\n{responseText}");
			}
		}
	}
}
