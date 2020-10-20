using Engine;
using System.Xml.Linq;

namespace Game
{
	public class SettingsControlsScreen : Screen
	{
		public ButtonWidget m_moveControlModeButton;

		public ButtonWidget m_lookControlModeButton;

		public ButtonWidget m_leftHandedLayoutButton;

		public ButtonWidget m_flipVerticalAxisButton;

		public ButtonWidget m_autoJumpButton;

		public ButtonWidget m_horizontalCreativeFlightButton;

		public ContainerWidget m_horizontalCreativeFlightPanel;

		public SliderWidget m_moveSensitivitySlider;

		public SliderWidget m_lookSensitivitySlider;

		public SliderWidget m_gamepadCursorSpeedSlider;

		public SliderWidget m_gamepadDeadZoneSlider;

		public SliderWidget m_creativeDigTimeSlider;

		public SliderWidget m_creativeReachSlider;

		public SliderWidget m_holdDurationSlider;

		public SliderWidget m_dragDistanceSlider;

		public SettingsControlsScreen()
		{
			XElement node = ContentManager.Get<XElement>("Screens/SettingsControlsScreen");
			LoadContents(this, node);
			m_moveControlModeButton = Children.Find<ButtonWidget>("MoveControlMode");
			m_lookControlModeButton = Children.Find<ButtonWidget>("LookControlMode");
			m_leftHandedLayoutButton = Children.Find<ButtonWidget>("LeftHandedLayout");
			m_flipVerticalAxisButton = Children.Find<ButtonWidget>("FlipVerticalAxis");
			m_autoJumpButton = Children.Find<ButtonWidget>("AutoJump");
			m_horizontalCreativeFlightButton = Children.Find<ButtonWidget>("HorizontalCreativeFlight");
			m_horizontalCreativeFlightPanel = Children.Find<ContainerWidget>("HorizontalCreativeFlightPanel");
			m_moveSensitivitySlider = Children.Find<SliderWidget>("MoveSensitivitySlider");
			m_lookSensitivitySlider = Children.Find<SliderWidget>("LookSensitivitySlider");
			m_gamepadCursorSpeedSlider = Children.Find<SliderWidget>("GamepadCursorSpeedSlider");
			m_gamepadDeadZoneSlider = Children.Find<SliderWidget>("GamepadDeadZoneSlider");
			m_creativeDigTimeSlider = Children.Find<SliderWidget>("CreativeDigTimeSlider");
			m_creativeReachSlider = Children.Find<SliderWidget>("CreativeReachSlider");
			m_holdDurationSlider = Children.Find<SliderWidget>("HoldDurationSlider");
			m_dragDistanceSlider = Children.Find<SliderWidget>("DragDistanceSlider");
			m_horizontalCreativeFlightPanel.IsVisible = false;
		}

		public override void Update()
		{
			if (m_moveControlModeButton.IsClicked)
			{
				SettingsManager.MoveControlMode = (MoveControlMode)((int)(SettingsManager.MoveControlMode + 1) % EnumUtils.GetEnumValues(typeof(MoveControlMode)).Count);
			}
			if (m_lookControlModeButton.IsClicked)
			{
				SettingsManager.LookControlMode = (LookControlMode)((int)(SettingsManager.LookControlMode + 1) % EnumUtils.GetEnumValues(typeof(LookControlMode)).Count);
			}
			if (m_leftHandedLayoutButton.IsClicked)
			{
				SettingsManager.LeftHandedLayout = !SettingsManager.LeftHandedLayout;
			}
			if (m_flipVerticalAxisButton.IsClicked)
			{
				SettingsManager.FlipVerticalAxis = !SettingsManager.FlipVerticalAxis;
			}
			if (m_autoJumpButton.IsClicked)
			{
				SettingsManager.AutoJump = !SettingsManager.AutoJump;
			}
			if (m_horizontalCreativeFlightButton.IsClicked)
			{
				SettingsManager.HorizontalCreativeFlight = !SettingsManager.HorizontalCreativeFlight;
			}
			if (m_moveSensitivitySlider.IsSliding)
			{
				SettingsManager.MoveSensitivity = m_moveSensitivitySlider.Value;
			}
			if (m_lookSensitivitySlider.IsSliding)
			{
				SettingsManager.LookSensitivity = m_lookSensitivitySlider.Value;
			}
			if (m_gamepadCursorSpeedSlider.IsSliding)
			{
				SettingsManager.GamepadCursorSpeed = m_gamepadCursorSpeedSlider.Value;
			}
			if (m_gamepadDeadZoneSlider.IsSliding)
			{
				SettingsManager.GamepadDeadZone = m_gamepadDeadZoneSlider.Value;
			}
			if (m_creativeDigTimeSlider.IsSliding)
			{
				SettingsManager.CreativeDigTime = m_creativeDigTimeSlider.Value;
			}
			if (m_creativeReachSlider.IsSliding)
			{
				SettingsManager.CreativeReach = m_creativeReachSlider.Value;
			}
			if (m_holdDurationSlider.IsSliding)
			{
				SettingsManager.MinimumHoldDuration = m_holdDurationSlider.Value;
			}
			if (m_dragDistanceSlider.IsSliding)
			{
				SettingsManager.MinimumDragDistance = m_dragDistanceSlider.Value;
			}
			m_moveControlModeButton.Text = StringsManager.GetString("MoveControlMode." + SettingsManager.MoveControlMode.ToString());
			m_lookControlModeButton.Text = StringsManager.GetString("LookControlMode." + SettingsManager.LookControlMode.ToString());
			m_leftHandedLayoutButton.Text = (SettingsManager.LeftHandedLayout ? "On" : "Off");
			m_flipVerticalAxisButton.Text = (SettingsManager.FlipVerticalAxis ? "On" : "Off");
			m_autoJumpButton.Text = (SettingsManager.AutoJump ? "On" : "Off");
			m_horizontalCreativeFlightButton.Text = (SettingsManager.HorizontalCreativeFlight ? "On" : "Off");
			m_moveSensitivitySlider.Value = SettingsManager.MoveSensitivity;
			m_moveSensitivitySlider.Text = MathUtils.Round(SettingsManager.MoveSensitivity * 10f).ToString();
			m_lookSensitivitySlider.Value = SettingsManager.LookSensitivity;
			m_lookSensitivitySlider.Text = MathUtils.Round(SettingsManager.LookSensitivity * 10f).ToString();
			m_gamepadCursorSpeedSlider.Value = SettingsManager.GamepadCursorSpeed;
			m_gamepadCursorSpeedSlider.Text = $"{SettingsManager.GamepadCursorSpeed:0.0}x";
			m_gamepadDeadZoneSlider.Value = SettingsManager.GamepadDeadZone;
			m_gamepadDeadZoneSlider.Text = $"{SettingsManager.GamepadDeadZone * 100f:0}%";
			m_creativeDigTimeSlider.Value = SettingsManager.CreativeDigTime;
			m_creativeDigTimeSlider.Text = $"{MathUtils.Round(1000f * SettingsManager.CreativeDigTime)}ms";
			m_creativeReachSlider.Value = SettingsManager.CreativeReach;
			m_creativeReachSlider.Text = $"{SettingsManager.CreativeReach:0.0} blocks";
			m_holdDurationSlider.Value = SettingsManager.MinimumHoldDuration;
			m_holdDurationSlider.Text = $"{MathUtils.Round(1000f * SettingsManager.MinimumHoldDuration)}ms";
			m_dragDistanceSlider.Value = SettingsManager.MinimumDragDistance;
			m_dragDistanceSlider.Text = $"{MathUtils.Round(SettingsManager.MinimumDragDistance)} pix";
			if (base.Input.Back || base.Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
			{
				ScreensManager.SwitchScreen(ScreensManager.PreviousScreen);
			}
		}
	}
}
