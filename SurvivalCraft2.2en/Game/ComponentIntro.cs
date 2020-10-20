using Engine;
using GameEntitySystem;
using System;
using TemplatesDatabase;

namespace Game
{
	public class ComponentIntro : Component, IUpdateable
	{
		public SubsystemTime m_subsystemTime;

		public SubsystemTerrain m_subsystemTerrain;

		public SubsystemGameInfo m_subsystemGameInfo;

		public ComponentPlayer m_componentPlayer;

		public bool m_playIntro;

		public StateMachine m_stateMachine = new StateMachine();

		public UpdateOrder UpdateOrder => UpdateOrder.Default;

		public static Vector2 FindOceanDirection(ITerrainContentsGenerator generator, Vector2 position)
		{
			float num = 3.40282347E+38f;
			Vector2 result = Vector2.Zero;
			for (int i = 0; i < 36; i++)
			{
				Vector2 vector = Vector2.CreateFromAngle((float)i / 36f * 2f * (float)Math.PI);
				Vector2 vector2 = position + 50f * vector;
				float num2 = generator.CalculateOceanShoreDistance(vector2.X, vector2.Y);
				if (num2 < num)
				{
					result = vector;
					num = num2;
				}
			}
			return result;
		}

		public void Update(float dt)
		{
			if (m_playIntro)
			{
				m_playIntro = false;
				m_stateMachine.TransitionTo("ShipView");
			}
			m_stateMachine.Update();
		}

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			m_subsystemTime = base.Project.FindSubsystem<SubsystemTime>(throwOnError: true);
			m_subsystemTerrain = base.Project.FindSubsystem<SubsystemTerrain>(throwOnError: true);
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_componentPlayer = base.Entity.FindComponent<ComponentPlayer>(throwOnError: true);
			m_playIntro = valuesDictionary.GetValue<bool>("PlayIntro");
			m_stateMachine.AddState("ShipView", ShipView_Enter, ShipView_Update, null);
		}

		public override void Save(ValuesDictionary valuesDictionary, EntityToIdMap entityToIdMap)
		{
			valuesDictionary.SetValue("PlayIntro", m_playIntro);
		}

		public void ShipView_Enter()
		{
			ComponentBody componentBody = m_componentPlayer.Entity.FindComponent<ComponentBody>(throwOnError: true);
			Vector2 vector = FindOceanDirection(m_subsystemTerrain.TerrainContentsGenerator, componentBody.Position.XZ);
			Vector2 vector2 = componentBody.Position.XZ + 25f * vector;
			bool isPlayerMounted = m_componentPlayer.ComponentRider.Mount != null;
			Vector2 vector3 = vector2;
			float num = -3.40282347E+38f;
			for (int i = Terrain.ToCell(vector2.Y) - 15; i < Terrain.ToCell(vector2.Y) + 15; i++)
			{
				for (int j = Terrain.ToCell(vector2.X) - 15; j < Terrain.ToCell(vector2.X) + 15; j++)
				{
					float num2 = ScoreShipPosition(componentBody.Position.XZ, j, i);
					if (num2 > num)
					{
						num = num2;
						vector3 = new Vector2(j, i);
					}
				}
			}
			DatabaseObject databaseObject = base.Project.GameDatabase.Database.FindDatabaseObject("IntroShip", base.Project.GameDatabase.EntityTemplateType, throwIfNotFound: true);
			ValuesDictionary valuesDictionary = new ValuesDictionary();
			valuesDictionary.PopulateFromDatabaseObject(databaseObject);
			Entity entity = base.Project.CreateEntity(valuesDictionary);
			Vector3 vector4 = new Vector3(vector3.X, (float)m_subsystemTerrain.TerrainContentsGenerator.OceanLevel + 0.5f, vector3.Y);
			entity.FindComponent<ComponentFrame>(throwOnError: true).Position = vector4;
			entity.FindComponent<ComponentIntroShip>(throwOnError: true).Heading = Vector2.Angle(vector, -Vector2.UnitY);
			base.Project.AddEntity(entity);
			m_subsystemTime.QueueGameTimeDelayedExecution(2.0, delegate
			{
				m_componentPlayer.ComponentGui.DisplayLargeMessage(null, "Cast away, boys.", 5f, 0f);
			});
			m_subsystemTime.QueueGameTimeDelayedExecution(7.0, delegate
			{
				if (isPlayerMounted)
				{
					m_componentPlayer.ComponentGui.DisplayLargeMessage(null, "Hey you, in the boat!", 5f, 0f);
				}
				else
				{
					m_componentPlayer.ComponentGui.DisplayLargeMessage(null, "Hey you, on the shore!", 5f, 0f);
				}
			});
			m_subsystemTime.QueueGameTimeDelayedExecution(12.0, delegate
			{
				m_componentPlayer.ComponentGui.DisplayLargeMessage(null, "We're leaving you here for good!", 5f, 0f);
			});
			IntroCamera introCamera = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
			m_componentPlayer.GameWidget.ActiveCamera = introCamera;
			introCamera.CameraPosition = vector4 + new Vector3(12f * vector.X, 8f, 12f * vector.Y) + new Vector3(-5f * vector.Y, 0f, 5f * vector.X);
			introCamera.TargetPosition = m_componentPlayer.ComponentCreatureModel.EyePosition + 2.5f * new Vector3(vector.X, 0f, vector.Y);
			introCamera.Speed = 0f;
			introCamera.TargetCameraPosition = m_componentPlayer.ComponentCreatureModel.EyePosition;
		}

		public void ShipView_Update()
		{
			IntroCamera introCamera = m_componentPlayer.GameWidget.FindCamera<IntroCamera>();
			introCamera.Speed = MathUtils.Lerp(0f, 8f, MathUtils.Saturate(((float)m_subsystemGameInfo.TotalElapsedGameTime - 6f) / 3f));
			if (Vector3.Distance(introCamera.TargetCameraPosition, introCamera.CameraPosition) < 0.3f)
			{
				m_componentPlayer.GameWidget.ActiveCamera = m_componentPlayer.GameWidget.FindCamera<FppCamera>();
				m_stateMachine.TransitionTo(null);
			}
		}

		public float ScoreShipPosition(Vector2 playerPosition, int x, int z)
		{
			float num = 0f;
			float num2 = m_subsystemTerrain.TerrainContentsGenerator.CalculateOceanShoreDistance(x, z);
			if (num2 > -8f)
			{
				num -= 100f;
			}
			num -= 0.25f * num2;
			float num3 = Vector2.Distance(playerPosition, new Vector2(x, z));
			num -= MathUtils.Abs(num3 - 20f);
			int num4 = 0;
			TerrainChunk chunkAtCell = m_subsystemTerrain.Terrain.GetChunkAtCell(x, z);
			if (chunkAtCell != null && chunkAtCell.State >= TerrainChunkState.InvalidLight)
			{
				int oceanLevel = m_subsystemTerrain.TerrainContentsGenerator.OceanLevel;
				int num5 = oceanLevel;
				while (num5 >= oceanLevel - 5 && num5 >= 0)
				{
					int cellContentsFast = chunkAtCell.GetCellContentsFast(x & 0xF, num5, z & 0xF);
					if (cellContentsFast != 18 && cellContentsFast != 92)
					{
						break;
					}
					num5--;
					num4++;
				}
			}
			else
			{
				num4 = 2;
			}
			if (num4 < 2)
			{
				num -= 100f;
			}
			return num + 2f * (float)num4;
		}
	}
}
