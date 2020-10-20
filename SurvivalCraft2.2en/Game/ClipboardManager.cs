using Android.Text;
using Engine;

namespace Game
{
	public static class ClipboardManager
	{
		public static string ClipboardString
		{
			get
			{
				return ((Android.Text.ClipboardManager)Window.Activity.GetSystemService("clipboard")).Text;
			}
			set
			{
				((Android.Text.ClipboardManager)Window.Activity.GetSystemService("clipboard")).Text = value;
			}
		}
	}
}
