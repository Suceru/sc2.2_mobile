using Engine;
using Engine.Media;
using System.Xml.Linq;

namespace Game
{
	public class EditPaletteDialog : Dialog
	{
		public ContainerWidget m_listPanel;

		public ButtonWidget m_okButton;

		public ButtonWidget m_cancelButton;

		public LinkWidget[] m_labels = new LinkWidget[16];

		public BevelledButtonWidget[] m_rectangles = new BevelledButtonWidget[16];

		public ButtonWidget[] m_resetButtons = new ButtonWidget[16];

		public WorldPalette m_palette;

		public WorldPalette m_tmpPalette;

		public EditPaletteDialog(WorldPalette palette)
		{
			XElement node = ContentManager.Get<XElement>("Dialogs/EditPaletteDialog");
			LoadContents(this, node);
			m_listPanel = Children.Find<ContainerWidget>("EditPaletteDialog.ListPanel");
			m_okButton = Children.Find<ButtonWidget>("EditPaletteDialog.OK");
			m_cancelButton = Children.Find<ButtonWidget>("EditPaletteDialog.Cancel");
			for (int i = 0; i < 16; i++)
			{
				StackPanelWidget obj = new StackPanelWidget
				{
					Direction = LayoutDirection.Horizontal,
					Children = 
					{
						(Widget)new CanvasWidget
						{
							Size = new Vector2(32f, 60f),
							Children = 
							{
								(Widget)new LabelWidget
								{
									Text = (i + 1).ToString() + ".",
									Color = Color.Gray,
									HorizontalAlignment = WidgetAlignment.Far,
									VerticalAlignment = WidgetAlignment.Center,
									Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
								}
							}
						},
						(Widget)new CanvasWidget
						{
							Size = new Vector2(10f, 0f)
						}
					}
				};
				obj.Children.Add(m_labels[i] = new LinkWidget
				{
					Size = new Vector2(300f, -1f),
					VerticalAlignment = WidgetAlignment.Center,
					Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
				});
				obj.Children.Add(new CanvasWidget
				{
					Size = new Vector2(10f, 0f)
				});
				obj.Children.Add(m_rectangles[i] = new BevelledButtonWidget
				{
					Size = new Vector2(1f / 0f, 60f),
					BevelSize = 1f,
					AmbientLight = 1f,
					DirectionalLight = 0.4f,
					VerticalAlignment = WidgetAlignment.Center
				});
				obj.Children.Add(new CanvasWidget
				{
					Size = new Vector2(10f, 0f)
				});
				obj.Children.Add(m_resetButtons[i] = new BevelledButtonWidget
				{
					Size = new Vector2(160f, 60f),
					VerticalAlignment = WidgetAlignment.Center,
					Text = "Reset"
				});
				obj.Children.Add(new CanvasWidget
				{
					Size = new Vector2(10f, 0f)
				});
				StackPanelWidget widget = obj;
				m_listPanel.Children.Add(widget);
			}
			m_palette = palette;
			m_tmpPalette = new WorldPalette();
			m_palette.CopyTo(m_tmpPalette);
		}

		public override void Update()
		{
			for (int j = 0; j < 16; j++)
			{
				m_labels[j].Text = m_tmpPalette.Names[j];
				m_rectangles[j].CenterColor = m_tmpPalette.Colors[j];
				m_resetButtons[j].IsEnabled = (m_tmpPalette.Colors[j] != WorldPalette.DefaultColors[j] || m_tmpPalette.Names[j] != WorldPalette.DefaultNames[j]);
			}
			for (int k = 0; k < 16; k++)
			{
				int i = k;
				if (m_labels[k].IsClicked)
				{
					DialogsManager.ShowDialog(this, new TextBoxDialog("Edit Color Name", m_labels[k].Text, 16, delegate(string s)
					{
						if (s != null)
						{
							if (WorldPalette.VerifyColorName(s))
							{
								m_tmpPalette.Names[i] = s;
							}
							else
							{
								DialogsManager.ShowDialog(this, new MessageDialog("Invalid name", null, "OK", null, null));
							}
						}
					}));
				}
				if (m_rectangles[k].IsClicked)
				{
					DialogsManager.ShowDialog(this, new EditColorDialog(m_tmpPalette.Colors[k], delegate(Color? color)
					{
						if (color.HasValue)
						{
							m_tmpPalette.Colors[i] = color.Value;
						}
					}));
				}
				if (m_resetButtons[k].IsClicked)
				{
					m_tmpPalette.Colors[k] = WorldPalette.DefaultColors[k];
					m_tmpPalette.Names[k] = WorldPalette.DefaultNames[k];
				}
			}
			if (m_okButton.IsClicked)
			{
				m_tmpPalette.CopyTo(m_palette);
				Dismiss();
			}
			if (base.Input.Cancel || m_cancelButton.IsClicked)
			{
				Dismiss();
			}
		}

		public void Dismiss()
		{
			DialogsManager.HideDialog(this);
		}
	}
}
