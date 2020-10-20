using Engine;
using GameEntitySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Game
{
	public class GameWidget : CanvasWidget
	{
		public List<Camera> m_cameras = new List<Camera>();

		public Camera m_activeCamera;

		public ViewWidget ViewWidget
		{
			get;
			set;
		}

		public ContainerWidget GuiWidget
		{
			get;
			set;
		}

		public int GameWidgetIndex
		{
			get;
			set;
		}

		public SubsystemGameWidgets SubsystemGameWidgets
		{
			get;
			set;
		}

		public PlayerData PlayerData
		{
			get;
			set;
		}

		public ReadOnlyList<Camera> Cameras => new ReadOnlyList<Camera>(m_cameras);

		public Camera ActiveCamera
		{
			get
			{
				return m_activeCamera;
			}
			set
			{
				if (value == null || value.GameWidget != this)
				{
					throw new InvalidOperationException("Invalid camera.");
				}
				if (!IsCameraAllowed(value))
				{
					value = FindCamera<FppCamera>();
				}
				if (value != m_activeCamera)
				{
					Camera activeCamera = m_activeCamera;
					m_activeCamera = value;
					m_activeCamera.Activate(activeCamera);
				}
			}
		}

		public ComponentCreature Target
		{
			get;
			set;
		}

		public GameWidget(PlayerData playerData, int gameViewIndex)
		{
			PlayerData = playerData;
			GameWidgetIndex = gameViewIndex;
			SubsystemGameWidgets = playerData.SubsystemGameWidgets;
			LoadContents(this, ContentManager.Get<XElement>("Widgets/GameWidget"));
			ViewWidget = Children.Find<ViewWidget>("View");
			GuiWidget = Children.Find<ContainerWidget>("Gui");
			m_cameras.Add(new FppCamera(this));
			m_cameras.Add(new DeathCamera(this));
			m_cameras.Add(new IntroCamera(this));
			m_cameras.Add(new TppCamera(this));
			m_cameras.Add(new OrbitCamera(this));
			m_cameras.Add(new FixedCamera(this));
			m_cameras.Add(new LoadingCamera(this));
			m_activeCamera = FindCamera<LoadingCamera>();
		}

		public T FindCamera<T>(bool throwOnError = true) where T : Camera
		{
			T val = (T)m_cameras.FirstOrDefault((Camera c) => c is T);
			if (val != null || !throwOnError)
			{
				return val;
			}
			throw new InvalidOperationException($"Camera with type \"{typeof(T).Name}\" not found.");
		}

		public bool IsEntityTarget(Entity entity)
		{
			if (Target != null)
			{
				return Target.Entity == entity;
			}
			return false;
		}

		public bool IsEntityFirstPersonTarget(Entity entity)
		{
			if (IsEntityTarget(entity))
			{
				return ActiveCamera is FppCamera;
			}
			return false;
		}

		public override void Update()
		{
			WidgetInputDevice widgetInputDevice = DetermineInputDevices();
			if (base.WidgetsHierarchyInput == null || base.WidgetsHierarchyInput.Devices != widgetInputDevice)
			{
				base.WidgetsHierarchyInput = new WidgetInput(widgetInputDevice);
			}
			if (GuiWidget.ParentWidget == null)
			{
				Widget.UpdateWidgetsHierarchy(GuiWidget);
			}
		}

		public WidgetInputDevice DetermineInputDevices()
		{
			if (PlayerData.SubsystemPlayers.PlayersData.Count > 0 && PlayerData == PlayerData.SubsystemPlayers.PlayersData[0])
			{
				WidgetInputDevice widgetInputDevice = WidgetInputDevice.None;
				foreach (PlayerData playersDatum in PlayerData.SubsystemPlayers.PlayersData)
				{
					if (playersDatum != PlayerData)
					{
						widgetInputDevice |= playersDatum.InputDevice;
					}
				}
				return (WidgetInputDevice.All & ~widgetInputDevice) | WidgetInputDevice.Touch | PlayerData.InputDevice;
			}
			WidgetInputDevice widgetInputDevice2 = WidgetInputDevice.None;
			foreach (PlayerData playersDatum2 in PlayerData.SubsystemPlayers.PlayersData)
			{
				if (playersDatum2 == PlayerData)
				{
					break;
				}
				widgetInputDevice2 |= playersDatum2.InputDevice;
			}
			return (PlayerData.InputDevice & ~widgetInputDevice2) | WidgetInputDevice.Touch;
		}

		public bool IsCameraAllowed(Camera camera)
		{
			if (PlayerData.ComponentPlayer?.ComponentInput.IsControlledByVr ?? false)
			{
				if (!(camera is FppCamera) && !(camera is LoadingCamera))
				{
					return camera is DeathCamera;
				}
				return true;
			}
			return true;
		}
	}
}
