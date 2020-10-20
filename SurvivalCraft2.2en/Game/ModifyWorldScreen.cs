using System.Collections.Generic;
using System.Xml.Linq;
using TemplatesDatabase;

namespace Game
{
	public class ModifyWorldScreen : Screen
	{
		public TextBoxWidget m_nameTextBox;

		public LabelWidget m_seedLabel;

		public ButtonWidget m_gameModeButton;

		public ButtonWidget m_worldOptionsButton;

		public LabelWidget m_errorLabel;

		public LabelWidget m_descriptionLabel;

		public ButtonWidget m_applyButton;

		public ButtonWidget m_deleteButton;

		public ButtonWidget m_uploadButton;

		public string m_directoryName;

		public WorldSettings m_worldSettings;

		public ValuesDictionary m_currentWorldSettingsData = new ValuesDictionary();

		public ValuesDictionary m_originalWorldSettingsData = new ValuesDictionary();

		public ModifyWorldScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/ModifyWorldScreen");
			LoadContents(this, node);
			m_nameTextBox = Children.Find<TextBoxWidget>("Name");
			m_seedLabel = Children.Find<LabelWidget>("Seed");
			m_gameModeButton = Children.Find<ButtonWidget>("GameMode");
			m_worldOptionsButton = Children.Find<ButtonWidget>("WorldOptions");
			m_errorLabel = Children.Find<LabelWidget>("Error");
			m_descriptionLabel = Children.Find<LabelWidget>("Description");
			m_applyButton = Children.Find<ButtonWidget>("Apply");
			m_deleteButton = Children.Find<ButtonWidget>("Delete");
			m_uploadButton = Children.Find<ButtonWidget>("Upload");
			m_nameTextBox.TextChanged += delegate
			{
				m_worldSettings.Name = m_nameTextBox.Text;
			};
		}

		public override void Enter(object[] parameters)
		{
			if (ScreensManager.PreviousScreen.GetType() != typeof(WorldOptionsScreen))
			{
				m_directoryName = (string)parameters[0];
				m_worldSettings = (WorldSettings)parameters[1];
				m_originalWorldSettingsData.Clear();
				m_worldSettings.Save(m_originalWorldSettingsData, liveModifiableParametersOnly: true);
			}
		}

		public override void Update()
		{
			if (m_gameModeButton.IsClicked && m_worldSettings.GameMode != GameMode.Cruel)
			{
				IList<int> enumValues = EnumUtils.GetEnumValues(typeof(GameMode));
				do
				{
					m_worldSettings.GameMode = (GameMode)((enumValues.IndexOf((int)m_worldSettings.GameMode) + 1) % enumValues.Count);
				}
				while (m_worldSettings.GameMode == GameMode.Cruel);
				m_descriptionLabel.Text = StringsManager.GetString("GameMode." + m_worldSettings.GameMode.ToString() + ".Description");
			}
			m_currentWorldSettingsData.Clear();
			m_worldSettings.Save(m_currentWorldSettingsData, liveModifiableParametersOnly: true);
			bool flag = !CompareValueDictionaries(m_originalWorldSettingsData, m_currentWorldSettingsData);
			bool flag2 = WorldsManager.ValidateWorldName(m_worldSettings.Name);
			m_nameTextBox.Text = m_worldSettings.Name;
			m_seedLabel.Text = m_worldSettings.Seed;
			m_gameModeButton.Text = m_worldSettings.GameMode.ToString();
			m_gameModeButton.IsEnabled = (m_worldSettings.GameMode != GameMode.Cruel);
			m_errorLabel.IsVisible = !flag2;
			m_descriptionLabel.IsVisible = flag2;
			m_uploadButton.IsEnabled = (flag2 && !flag);
			m_applyButton.IsEnabled = (flag2 && flag);
			m_descriptionLabel.Text = StringsManager.GetString("GameMode." + m_worldSettings.GameMode.ToString() + ".Description");
			if (m_worldOptionsButton.IsClicked)
			{
				ScreensManager.SwitchScreen("WorldOptions", m_worldSettings, true);
			}
			if (m_deleteButton.IsClicked)
			{
				MessageDialog dialog = null;
				dialog = new MessageDialog("Are you sure?", "The world will be irrecoverably deleted.", "Yes", "No", delegate(MessageDialogButton button)
				{
					if (button == MessageDialogButton.Button1)
					{
						WorldsManager.DeleteWorld(m_directoryName);
						ScreensManager.SwitchScreen("Play");
						DialogsManager.HideDialog(dialog);
					}
					else
					{
						DialogsManager.HideDialog(dialog);
					}
				});
				dialog.AutoHide = false;
				DialogsManager.ShowDialog(null, dialog);
			}
			if (m_uploadButton.IsClicked && flag2 && !flag)
			{
				ExternalContentManager.ShowUploadUi(ExternalContentType.World, m_directoryName);
			}
			if ((m_applyButton.IsClicked && flag2) & flag)
			{
				if (m_worldSettings.GameMode != 0 && m_worldSettings.GameMode != GameMode.Adventure)
				{
					m_worldSettings.ResetOptionsForNonCreativeMode();
				}
				WorldsManager.ChangeWorld(m_directoryName, m_worldSettings);
				ScreensManager.SwitchScreen("Play");
			}
			if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				if (flag)
				{
					DialogsManager.ShowDialog(null, new MessageDialog("Abandon changes?", "You changed some of the world properties, but they were not applied yet.", "Yes", "No", delegate(MessageDialogButton button)
					{
						if (button == MessageDialogButton.Button1)
						{
							ScreensManager.SwitchScreen("Play");
						}
					}));
				}
				else
				{
					ScreensManager.SwitchScreen("Play");
				}
			}
		}

		public static bool CompareValueDictionaries(ValuesDictionary d1, ValuesDictionary d2)
		{
			if (d1.Count != d2.Count)
			{
				return false;
			}
			foreach (KeyValuePair<string, object> item in d1)
			{
				object value = d2.GetValue<object>(item.Key, null);
				ValuesDictionary valuesDictionary = value as ValuesDictionary;
				if (valuesDictionary != null)
				{
					ValuesDictionary valuesDictionary2 = item.Value as ValuesDictionary;
					if (valuesDictionary2 == null || !CompareValueDictionaries(valuesDictionary, valuesDictionary2))
					{
						return false;
					}
				}
				else if (!object.Equals(value, item.Value))
				{
					return false;
				}
			}
			return true;
		}
	}
}
