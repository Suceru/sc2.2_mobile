using Engine;
using Engine.Media;
using Engine.Serialization;
using System;

namespace Game
{
	public class EditColorDialog : Dialog
	{
		public BevelledButtonWidget m_rectangle;

		public SliderWidget m_sliderR;

		public SliderWidget m_sliderG;

		public SliderWidget m_sliderB;

		public LabelWidget m_label;

		public ButtonWidget m_okButton;

		public ButtonWidget m_cancelButton;

		public Action<Color?> m_handler;

		public Color m_color;

		public EditColorDialog(Color color, Action<Color?> handler)
		{
			WidgetsList children = Children;
			CanvasWidget obj = new CanvasWidget
			{
				Size = new Vector2(660f, 420f),
				HorizontalAlignment = WidgetAlignment.Center,
				VerticalAlignment = WidgetAlignment.Center,
				Children = 
				{
					(Widget)new RectangleWidget
					{
						FillColor = new Color(0, 0, 0, 255),
						OutlineColor = new Color(128, 128, 128, 128),
						OutlineThickness = 2f
					}
				}
			};
			WidgetsList children2 = obj.Children;
			StackPanelWidget obj2 = new StackPanelWidget
			{
				Direction = LayoutDirection.Vertical,
				Margin = new Vector2(15f),
				HorizontalAlignment = WidgetAlignment.Center,
				Children = 
				{
					(Widget)new LabelWidget
					{
						Text = "Edit Color",
						HorizontalAlignment = WidgetAlignment.Center
					},
					(Widget)new CanvasWidget
					{
						Size = new Vector2(0f, 1f / 0f)
					}
				}
			};
			WidgetsList children3 = obj2.Children;
			StackPanelWidget obj3 = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal
			};
			WidgetsList children4 = obj3.Children;
			StackPanelWidget obj4 = new StackPanelWidget
			{
				Direction = LayoutDirection.Vertical,
				VerticalAlignment = WidgetAlignment.Center
			};
			WidgetsList children5 = obj4.Children;
			StackPanelWidget obj5 = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal,
				HorizontalAlignment = WidgetAlignment.Far,
				Margin = new Vector2(0f, 10f),
				Children = 
				{
					(Widget)new LabelWidget
					{
						Text = "Red:",
						Color = Color.Gray,
						VerticalAlignment = WidgetAlignment.Center,
						Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
					},
					(Widget)new CanvasWidget
					{
						Size = new Vector2(10f, 0f)
					}
				}
			};
			WidgetsList children6 = obj5.Children;
			SliderWidget obj6 = new SliderWidget
			{
				Size = new Vector2(300f, 50f),
				IsLabelVisible = false,
				MinValue = 0f,
				MaxValue = 255f,
				Granularity = 1f,
				SoundName = ""
			};
			SliderWidget widget = obj6;
			m_sliderR = obj6;
			children6.Add(widget);
			children5.Add(obj5);
			WidgetsList children7 = obj4.Children;
			StackPanelWidget obj7 = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal,
				HorizontalAlignment = WidgetAlignment.Far,
				Margin = new Vector2(0f, 10f),
				Children = 
				{
					(Widget)new LabelWidget
					{
						Text = "Green:",
						Color = Color.Gray,
						VerticalAlignment = WidgetAlignment.Center,
						Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
					},
					(Widget)new CanvasWidget
					{
						Size = new Vector2(10f, 0f)
					}
				}
			};
			WidgetsList children8 = obj7.Children;
			SliderWidget obj8 = new SliderWidget
			{
				Size = new Vector2(300f, 50f),
				IsLabelVisible = false,
				MinValue = 0f,
				MaxValue = 255f,
				Granularity = 1f,
				SoundName = ""
			};
			widget = obj8;
			m_sliderG = obj8;
			children8.Add(widget);
			children7.Add(obj7);
			WidgetsList children9 = obj4.Children;
			StackPanelWidget obj9 = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal,
				HorizontalAlignment = WidgetAlignment.Far,
				Margin = new Vector2(0f, 10f),
				Children = 
				{
					(Widget)new LabelWidget
					{
						Text = "Blue:",
						Color = Color.Gray,
						VerticalAlignment = WidgetAlignment.Center,
						Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
					},
					(Widget)new CanvasWidget
					{
						Size = new Vector2(10f, 0f)
					}
				}
			};
			WidgetsList children10 = obj9.Children;
			SliderWidget obj10 = new SliderWidget
			{
				Size = new Vector2(300f, 50f),
				IsLabelVisible = false,
				MinValue = 0f,
				MaxValue = 255f,
				Granularity = 1f,
				SoundName = ""
			};
			widget = obj10;
			m_sliderB = obj10;
			children10.Add(widget);
			children9.Add(obj9);
			children4.Add(obj4);
			obj3.Children.Add(new CanvasWidget
			{
				Size = new Vector2(20f, 0f)
			});
			WidgetsList children11 = obj3.Children;
			CanvasWidget canvasWidget = new CanvasWidget();
			WidgetsList children12 = canvasWidget.Children;
			BevelledButtonWidget obj11 = new BevelledButtonWidget
			{
				Size = new Vector2(200f, 240f),
				AmbientLight = 1f,
				HorizontalAlignment = WidgetAlignment.Center,
				VerticalAlignment = WidgetAlignment.Center
			};
			BevelledButtonWidget widget2 = obj11;
			m_rectangle = obj11;
			children12.Add(widget2);
			WidgetsList children13 = canvasWidget.Children;
			LabelWidget obj12 = new LabelWidget
			{
				HorizontalAlignment = WidgetAlignment.Center,
				VerticalAlignment = WidgetAlignment.Center,
				Font = ContentManager.Get<BitmapFont>("Fonts/Pericles")
			};
			LabelWidget widget3 = obj12;
			m_label = obj12;
			children13.Add(widget3);
			children11.Add(canvasWidget);
			children3.Add(obj3);
			obj2.Children.Add(new CanvasWidget
			{
				Size = new Vector2(0f, 1f / 0f)
			});
			WidgetsList children14 = obj2.Children;
			StackPanelWidget obj13 = new StackPanelWidget
			{
				Direction = LayoutDirection.Horizontal,
				HorizontalAlignment = WidgetAlignment.Center
			};
			WidgetsList children15 = obj13.Children;
			BevelledButtonWidget obj14 = new BevelledButtonWidget
			{
				Size = new Vector2(160f, 60f),
				Text = "OK"
			};
			ButtonWidget widget4 = obj14;
			m_okButton = obj14;
			children15.Add(widget4);
			obj13.Children.Add(new CanvasWidget
			{
				Size = new Vector2(50f, 0f)
			});
			WidgetsList children16 = obj13.Children;
			BevelledButtonWidget obj15 = new BevelledButtonWidget
			{
				Size = new Vector2(160f, 60f),
				Text = "Cancel"
			};
			widget4 = obj15;
			m_cancelButton = obj15;
			children16.Add(widget4);
			children14.Add(obj13);
			children2.Add(obj2);
			children.Add(obj);
			m_handler = handler;
			m_color = color;
			UpdateControls();
		}

		public override void Update()
		{
			if (m_rectangle.IsClicked)
			{
				DialogsManager.ShowDialog(this, new TextBoxDialog("Enter Color", GetColorString(), 20, delegate(string s)
				{
					if (s != null)
					{
						try
						{
							m_color.RGB = HumanReadableConverter.ConvertFromString<Color>(s);
						}
						catch
						{
							DialogsManager.ShowDialog(this, new MessageDialog("Invalid Color", "Use R,G,B or #HEX notation, e.g. 255,92,13 or #FF5C0D", "OK", null, null));
						}
					}
				}));
			}
			if (m_sliderR.IsSliding)
			{
				m_color.R = (byte)m_sliderR.Value;
			}
			if (m_sliderG.IsSliding)
			{
				m_color.G = (byte)m_sliderG.Value;
			}
			if (m_sliderB.IsSliding)
			{
				m_color.B = (byte)m_sliderB.Value;
			}
			if (m_okButton.IsClicked)
			{
				Dismiss(m_color);
			}
			if (base.Input.Cancel || m_cancelButton.IsClicked)
			{
				Dismiss(null);
			}
			UpdateControls();
		}

		public void UpdateControls()
		{
			m_rectangle.CenterColor = m_color;
			m_sliderR.Value = (int)m_color.R;
			m_sliderG.Value = (int)m_color.G;
			m_sliderB.Value = (int)m_color.B;
			m_label.Text = GetColorString();
		}

		public string GetColorString()
		{
			return $"#{m_color.R:X2}{m_color.G:X2}{m_color.B:X2}";
		}

		public void Dismiss(Color? result)
		{
			DialogsManager.HideDialog(this);
			m_handler?.Invoke(result);
		}
	}
}
