using System;
using System.Xml.Linq;

namespace Game
{
	public class EditBatteryDialog : Dialog
	{
		public Action<int> m_handler;

		public ButtonWidget m_okButton;

		public ButtonWidget m_cancelButton;

		public SliderWidget m_voltageSlider;

		public int m_voltageLevel;

		public EditBatteryDialog(int voltageLevel, Action<int> handler)
		{
			XElement node = ContentManager.Get<XElement>("Dialogs/EditBatteryDialog");
			LoadContents(this, node);
			m_okButton = Children.Find<ButtonWidget>("EditBatteryDialog.OK");
			m_cancelButton = Children.Find<ButtonWidget>("EditBatteryDialog.Cancel");
			m_voltageSlider = Children.Find<SliderWidget>("EditBatteryDialog.VoltageSlider");
			m_handler = handler;
			m_voltageLevel = voltageLevel;
			UpdateControls();
		}

		public override void Update()
		{
			if (m_voltageSlider.IsSliding)
			{
				m_voltageLevel = (int)m_voltageSlider.Value;
			}
			if (m_okButton.IsClicked)
			{
				Dismiss(m_voltageLevel);
			}
			if (base.Input.Cancel || m_cancelButton.IsClicked)
			{
				Dismiss(null);
			}
			UpdateControls();
		}

		public void UpdateControls()
		{
			m_voltageSlider.Text = string.Format("{0:0.0}V ({1})", 1.5f * (float)m_voltageLevel / 15f, (m_voltageLevel < 8) ? "Low" : "High");
			m_voltageSlider.Value = m_voltageLevel;
		}

		public void Dismiss(int? result)
		{
			DialogsManager.HideDialog(this);
			if (m_handler != null && result.HasValue)
			{
				m_handler(result.Value);
			}
		}
	}
}
