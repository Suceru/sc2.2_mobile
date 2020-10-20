using System;

namespace Game
{
	[Flags]
	public enum WidgetInputDevice
	{
		None = 0x0,
		Keyboard = 0x1,
		Mouse = 0x2,
		Touch = 0x4,
		GamePad1 = 0x8,
		GamePad2 = 0x10,
		GamePad3 = 0x20,
		GamePad4 = 0x40,
		Gamepads = 0x78,
		VrControllers = 0x80,
		All = 0xFF
	}
}
