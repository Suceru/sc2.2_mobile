using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class PluginLoaderAttribute : Attribute
{
	public readonly ModInfo info;

	public ModInfo ModInfo => info;

	public PluginLoaderAttribute(string name, string description, uint version, string scversion, string url, string updateUrl, string authorList, string credits, string logo, string screenshots, string parent, string dependency = null, string dependants = null, bool usedependencyInfo = false)
	{
		info = new ModInfo(name, description, version, scversion, url, updateUrl, authorList, credits, logo, screenshots, parent, dependency, dependants, usedependencyInfo);
	}

	public PluginLoaderAttribute(string name, string description, uint version)
	{
		info.Name = name;
		info.Description = description;
		info.Version = version;
	}
}
