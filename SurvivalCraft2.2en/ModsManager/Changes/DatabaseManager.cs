using Engine.Serialization;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using TemplatesDatabase;

namespace Game
{
	public static class DatabaseManager
	{
		public static GameDatabase m_gameDatabase;

		public static Dictionary<string, ValuesDictionary> m_valueDictionaries = new Dictionary<string, ValuesDictionary>();

		public static GameDatabase GameDatabase
		{
			get
			{
				if (m_gameDatabase != null)
				{
					return m_gameDatabase;
				}
				throw new InvalidOperationException("Database not loaded.");
			}
		}

		public static ICollection<ValuesDictionary> EntitiesValuesDictionaries => m_valueDictionaries.Values;

		public static void Initialize()
		{
			if (m_gameDatabase == null)
			{
				XElement node = ContentManager.Get<XElement>("Database");
				ContentManager.Dispose("Database");
				ModsManager.CombineXml(node, ModsManager.GetEntries(".xdb"), "Guid", "Name");
				m_gameDatabase = new GameDatabase(XmlDatabaseSerializer.LoadDatabase(node));
				foreach (DatabaseObject explicitNestingChild in GameDatabase.Database.Root.GetExplicitNestingChildren(GameDatabase.EntityTemplateType, directChildrenOnly: false))
				{
					ValuesDictionary valuesDictionary = new ValuesDictionary();
					valuesDictionary.PopulateFromDatabaseObject(explicitNestingChild);
					m_valueDictionaries.Add(explicitNestingChild.Name, valuesDictionary);
				}
				return;
			}
			throw new InvalidOperationException("Database already loaded.");
		}

		public static ValuesDictionary FindEntityValuesDictionary(string entityTemplateName, bool throwIfNotFound)
		{
			if (!m_valueDictionaries.TryGetValue(entityTemplateName, out ValuesDictionary value) && throwIfNotFound)
			{
				throw new InvalidOperationException($"EntityTemplate \"{entityTemplateName}\" not found.");
			}
			return value;
		}

		public static ValuesDictionary FindValuesDictionaryForComponent(ValuesDictionary entityVd, Type componentType)
		{
			foreach (ValuesDictionary item in entityVd.Values.OfType<ValuesDictionary>())
			{
				if (item.DatabaseObject.Type == GameDatabase.MemberComponentTemplateType)
				{
					Type type = TypeCache.FindType(item.GetValue<string>("Class"), skipSystemAssemblies: true, throwIfNotFound: true);
					if (componentType.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
					{
						return item;
					}
				}
			}
			return null;
		}

		public static Entity CreateEntity(Project project, string entityTemplateName, bool throwIfNotFound)
		{
			ValuesDictionary valuesDictionary = FindEntityValuesDictionary(entityTemplateName, throwIfNotFound);
			if (valuesDictionary == null)
			{
				return null;
			}
			return project.CreateEntity(valuesDictionary);
		}

		public static Entity CreateEntity(Project project, string entityTemplateName, ValuesDictionary overrides, bool throwIfNotFound)
		{
			ValuesDictionary valuesDictionary = FindEntityValuesDictionary(entityTemplateName, throwIfNotFound);
			if (valuesDictionary != null)
			{
				valuesDictionary.ApplyOverrides(overrides);
				return project.CreateEntity(valuesDictionary);
			}
			return null;
		}
	}
}
