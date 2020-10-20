// Game.ModsManager
using Engine;
using Game;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using XmlUtilities;

public static class ModsManager
{
	public static List<Assembly> loadedAssemblies;

	public static string Extension;

	public static HashSet<string> DisabledMods;

	public static HashSet<string> CachedMods;

	public static bool ReadZip;

	public static bool AutoCleanCache;

	public static int SearchDepth;

	public static List<string> Files;

	public static List<string> Directories;

	public static Dictionary<string, ZipArchive> Archives;

	public static string CacheDir;

	public static Action<FileEntry, Exception> ErrorHandler;

	public static Action<StreamWriter> ConfigSaved;

	public static Action Initialized;

	public static List<ModInfo> LoadedMods;

	public static Dictionary<string, string> customer_Strings;

	public static Func<XElement, IEnumerable<FileEntry>, string, string, string, XElement> CombineXml1;

	public static string ModsPath = "android:SurvivalCraft2.2/Mods";

	public static string ModsSetPath = "android:SurvivalCraft2.2/ModSettings.xml";

	public static string path;//移动端mods数据文件夹

	public static Dictionary<string, ZipArchive> zip_filelist;
	public static List<FileEntry> quickAddModsFileList = new List<FileEntry>();
	public static string[] acceptTypes = new string[] {".dll",".csv",".xdb",".clo",".cr" };
	public static XElement CombineXml(XElement node, IEnumerable<FileEntry> files, string attr1 = null, string attr2 = null, string type = null)
	{
		Func<XElement, IEnumerable<FileEntry>, string, string, string, XElement> combineXml = CombineXml1;
		if (combineXml != null)
		{
			return combineXml(node, files, attr1, attr2, type);
		}
		IEnumerator<FileEntry> enumerator = files.GetEnumerator();
		while (enumerator.MoveNext())
		{
			try
			{
				XElement src = XmlUtils.LoadXmlFromStream(enumerator.Current.Stream, null, throwOnError: true);
				Modify(node, src, attr1, attr2, type);
			}
			catch (Exception arg)
			{
				ModsManager.ErrorHandler(enumerator.Current, arg);
			}
		}
		return node;
	}
	public static void Modify(XElement dst, XElement src, string attr1 = null, string attr2 = null, XName type = null)
	{
		List<XElement> list = new List<XElement>();
		IEnumerator<XElement> enumerator = src.Elements().GetEnumerator();
		while (enumerator.MoveNext())
		{
			XElement current = enumerator.Current;
			string localName = current.Name.LocalName;
			string text = current.Attribute(attr1)?.Value;
			string text2 = current.Attribute(attr2)?.Value;
			int num = (localName.Length >= 2 && localName[0] == 'r' && localName[1] == '-') ? (current.IsEmpty ? 2 : (-2)) : 0;
			IEnumerator<XElement> enumerator2 = dst.DescendantsAndSelf((localName.Length == 2 && num != 0) ? type : ((XName)current.Name.LocalName.Substring(Math.Abs(num)))).GetEnumerator();
			while (enumerator2.MoveNext())
			{
				XElement current2 = enumerator2.Current;
				IEnumerator<XAttribute> enumerator3 = current2.Attributes().GetEnumerator();
				while (true)
				{
					if (enumerator3.MoveNext())
					{
						localName = enumerator3.Current.Name.LocalName;
						string value = enumerator3.Current.Value;
						XAttribute xAttribute;
						if (text != null && string.Equals(localName, attr1))
						{
							if (!string.Equals(value, text))
							{
								break;
							}
						}
						else if (text2 != null && string.Equals(localName, attr2))
						{
							if (!string.Equals(value, text2))
							{
								break;
							}
						}
						else if ((xAttribute = current.Attribute(XName.Get("new-" + localName))) != null)
						{
							current2.SetAttributeValue(XName.Get(localName), xAttribute.Value);
						}
						continue;
					}
					if (num < 0)
					{
						current2.RemoveNodes();
						current2.Add(current.Elements());
					}
					else if (num > 0)
					{
						list.Add(current2);
					}
					else if (!current.IsEmpty)
					{
						current2.Add(current.Elements());
					}
					break;
				}
			}
		}
		List<XElement>.Enumerator enumerator4 = list.GetEnumerator();
		while (enumerator4.MoveNext())
		{
			enumerator4.Current.Remove();
		}
	}

    public static void Initialize()
	{
		loadedAssemblies = new List<Assembly>();
		LoadedMods = new List<ModInfo>();
		CachedMods = new HashSet<string>();
		DisabledMods = new HashSet<string>();
		Files = new List<string>();
		Directories = new List<string>();
		customer_Strings = new Dictionary<string, string>();
		ReadZip = true;
		SearchDepth = 3;
		ErrorHandler = LogException;
		zip_filelist = new Dictionary<string, ZipArchive>();
		if (!Storage.DirectoryExists(ModsPath)) Storage.CreateDirectory(ModsPath);
		getFiles();//获取zip列表
		List<FileEntry> dlls = GetEntries(".dll");
        foreach(FileEntry item in dlls){
			LoadMod(Assembly.Load(StreamToBytes(item.Stream)));
		}
		Log.Information("mods manager initialize success");
	}
	public static void getFiles() {//获取zip包列表，变成ZipArchive
		foreach (string item in Storage.ListFileNames(ModsPath)) {
			string ms = Storage.GetExtension(item);
			string ks = Storage.CombinePaths(ModsPath, item);
//			string lk = Storage.GetSystemPath(ks);
			Stream stream = Storage.OpenFile(ks,OpenFileMode.Read);
			if (acceptTypes.Contains(ms))
			{
				quickAddModsFileList.Add(new FileEntry() { Stream=stream , Filename=item });
				continue;
			}

			try
			{
				if (ms == ".zip" || ms == ".scmod")
				{
					ZipArchive zipArchive = ZipArchive.Open(stream, true);
					zip_filelist.Add(item, zipArchive);
				}
			}
			catch (Exception e) {
				Log.Error("load file ["+ks+"] error."+e.ToString());
			}
		}
	}
	public static List<FileEntry> GetEntries(string ext) {//获取制定后缀的文件集
		List<FileEntry> fileEntries = new List<FileEntry>();
		foreach (FileEntry fileEntry1 in quickAddModsFileList) {
			if(Storage.GetExtension(fileEntry1.Filename)==ext)fileEntries.Add(fileEntry1);
		}
		foreach (ZipArchive zipArchive in zip_filelist.Values) {
			foreach (ZipArchiveEntry zipArchiveEntry in zipArchive.ReadCentralDir()) {
				string fn = zipArchiveEntry.FilenameInZip;
				if (Storage.GetExtension(fn) == ext)
                {
					MemoryStream stream = new MemoryStream();
					zipArchive.ExtractFile(zipArchiveEntry, stream);
					FileEntry fileEntry = new FileEntry();
					fileEntry.Filename = fn;
					stream.Position = 0L;
					fileEntry.Stream = stream;
					fileEntries.Add(fileEntry);
				}
			}
		}
		return fileEntries;	
	}
	public static void LogException(FileEntry file, Exception ex)
	{
		Log.Warning("Loading \"" + file.Filename.Substring(path.Length + 1) + "\" failed: " + ex.ToString());
		file.Stream.Close();
	}
	/// <summary> 
	/// 将 Stream 转成 byte[] 
	/// </summary> 
	public static byte[] StreamToBytes(Stream stream)
	{
		byte[] bytes = new byte[stream.Length];
		stream.Read(bytes, 0, bytes.Length);

		// 设置当前流的位置为流的开始 
		stream.Seek(0, SeekOrigin.Begin);
		return bytes;
	}

	/// <summary> 
	/// 将 byte[] 转成 Stream 
	/// </summary> 
	public static Stream BytesToStream(byte[] bytes)
	{
		Stream stream = new MemoryStream(bytes);
		return stream;
	}

	/// <summary> 
	/// 将 Stream 写入文件 
	/// </summary> 
	public static void StreamToFile(Stream stream, string fileName)
	{
		// 把 Stream 转换成 byte[] 
		byte[] bytes = new byte[stream.Length];
		stream.Read(bytes, 0, bytes.Length);
		// 设置当前流的位置为流的开始 
		stream.Seek(0, SeekOrigin.Begin);

		// 把 byte[] 写入文件 
		FileStream fs = new FileStream(fileName, FileMode.Create);
		BinaryWriter bw = new BinaryWriter(fs);
		bw.Write(bytes);
		bw.Close();
		fs.Close();
	}

	/// <summary> 
	/// 从文件读取 Stream 
	/// </summary> 
	public static Stream FileToStream(string fileName)
	{
		// 打开文件 
		FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
		// 读取文件的 byte[] 
		byte[] bytes = new byte[fileStream.Length];
		fileStream.Read(bytes, 0, bytes.Length);
		fileStream.Close();
		// 把 byte[] 转换成 Stream 
		Stream stream = new MemoryStream(bytes);
		return stream;
	}

	public static void LoadMod(Assembly asm)
	{
		Type typeFromHandle = typeof(PluginLoaderAttribute);
		Type[] types = asm.GetTypes();
		for (int i = 0; i < types.Length; i++)
		{
			PluginLoaderAttribute pluginLoaderAttribute = (PluginLoaderAttribute)Attribute.GetCustomAttribute(types[i], typeFromHandle);
			if (pluginLoaderAttribute != null)
			{
				MethodInfo method;
				if ((method = types[i].GetMethod("Initialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) != null)
				{
					method.Invoke(Activator.CreateInstance(types[i]), null);
				}
				LoadedMods.Add(pluginLoaderAttribute.ModInfo);
				Log.Information("loaded mod ["+pluginLoaderAttribute.ModInfo.Name+"]");
			}
		}
		loadedAssemblies.Add(asm);
	}
	public static string GetMd5(string input)
	{
		// Create a new instance of the MD5CryptoServiceProvider object.
		MD5 md5Hasher = MD5.Create();
		// Convert the input string to a byte array and compute the hash.
		byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));
		// Create a new Stringbuilder to collect the bytes
		// and create a string.
		StringBuilder sBuilder = new StringBuilder();
		// Loop through each byte of the hashed data
		// and format each one as a hexadecimal string.
		for (int i = 0; i < data.Length; i++)
		{
			sBuilder.Append(data[i].ToString("x2"));
		}


		// Return the hexadecimal string.
		return sBuilder.ToString();
	}
}
