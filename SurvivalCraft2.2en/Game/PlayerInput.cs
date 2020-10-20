using Engine;

namespace Game
{
	public struct PlayerInput
	{
		public Vector2 Look;

		public Vector3 Move;

		public Vector3 SneakMove;

		public Vector3? VrMove;

		public Vector2? VrLook;

		public Vector2 CameraLook;

		public Vector3 CameraMove;

		public Vector3 CameraSneakMove;

		public bool ToggleCreativeFly;

		public bool ToggleSneak;

		public bool ToggleMount;

		public bool EditItem;

		public bool Jump;

		public int ScrollInventory;

		public bool ToggleInventory;

		public bool ToggleClothing;

		public bool TakeScreenshot;

		public bool SwitchCameraMode;

		public bool TimeOfDay;

		public bool Lighting;

		public bool KeyboardHelp;

		public bool GamepadHelp;

		public Ray3? Dig;

		public Ray3? Hit;

		public Ray3? Aim;

		public Ray3? Interact;

		public Ray3? PickBlockType;

		public bool Drop;

		public int? SelectInventorySlot;
	}
}
