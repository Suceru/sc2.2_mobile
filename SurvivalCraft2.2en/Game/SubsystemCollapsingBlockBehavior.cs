using Engine;
using System.Collections.Generic;
using System.Linq;
using TemplatesDatabase;

namespace Game
{
	public class SubsystemCollapsingBlockBehavior : SubsystemBlockBehavior
	{
		public const string IdString = "CollapsingBlock";

		public SubsystemGameInfo m_subsystemGameInfo;

		public SubsystemSoundMaterials m_subsystemSoundMaterials;

		public SubsystemMovingBlocks m_subsystemMovingBlocks;

		public static int[] m_handledBlocks = new int[2]
		{
			7,
			6
		};

		public override int[] HandledBlocks => m_handledBlocks;

		public override void OnNeighborBlockChanged(int x, int y, int z, int neighborX, int neighborY, int neighborZ)
		{
			if (m_subsystemGameInfo.WorldSettings.EnvironmentBehaviorMode == EnvironmentBehaviorMode.Living)
			{
				TryCollapseColumn(new Point3(x, y, z));
			}
		}

		public override void Load(ValuesDictionary valuesDictionary)
		{
			base.Load(valuesDictionary);
			m_subsystemGameInfo = base.Project.FindSubsystem<SubsystemGameInfo>(throwOnError: true);
			m_subsystemSoundMaterials = base.Project.FindSubsystem<SubsystemSoundMaterials>(throwOnError: true);
			m_subsystemMovingBlocks = base.Project.FindSubsystem<SubsystemMovingBlocks>(throwOnError: true);
			m_subsystemMovingBlocks.Stopped += MovingBlocksStopped;
			m_subsystemMovingBlocks.CollidedWithTerrain += MovingBlocksCollidedWithTerrain;
		}

		public void MovingBlocksCollidedWithTerrain(IMovingBlockSet movingBlockSet, Point3 p)
		{
			if (movingBlockSet.Id == "CollapsingBlock")
			{
				int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(p.X, p.Y, p.Z);
				if (IsCollapseSupportBlock(cellValue))
				{
					movingBlockSet.Stop();
				}
				else if (IsCollapseDestructibleBlock(cellValue))
				{
					base.SubsystemTerrain.DestroyCell(0, p.X, p.Y, p.Z, 0, noDrop: false, noParticleSystem: false);
				}
			}
		}

		public void MovingBlocksStopped(IMovingBlockSet movingBlockSet)
		{
			if (movingBlockSet.Id == "CollapsingBlock")
			{
				Point3 p = Terrain.ToCell(MathUtils.Round(movingBlockSet.Position.X), MathUtils.Round(movingBlockSet.Position.Y), MathUtils.Round(movingBlockSet.Position.Z));
				foreach (MovingBlock block in movingBlockSet.Blocks)
				{
					Point3 point = p + block.Offset;
					base.SubsystemTerrain.DestroyCell(0, point.X, point.Y, point.Z, block.Value, noDrop: false, noParticleSystem: false);
				}
				m_subsystemMovingBlocks.RemoveMovingBlockSet(movingBlockSet);
				if (movingBlockSet.Blocks.Count > 0)
				{
					m_subsystemSoundMaterials.PlayImpactSound(movingBlockSet.Blocks[0].Value, movingBlockSet.Position, 1f);
				}
			}
		}

		public void TryCollapseColumn(Point3 p)
		{
			if (p.Y <= 0)
			{
				return;
			}
			int cellValue = base.SubsystemTerrain.Terrain.GetCellValue(p.X, p.Y - 1, p.Z);
			if (IsCollapseSupportBlock(cellValue))
			{
				return;
			}
			List<MovingBlock> list = new List<MovingBlock>();
			for (int i = p.Y; i < 256; i++)
			{
				int cellValue2 = base.SubsystemTerrain.Terrain.GetCellValue(p.X, i, p.Z);
				if (!IsCollapsibleBlock(cellValue2))
				{
					break;
				}
				list.Add(new MovingBlock
				{
					Value = cellValue2,
					Offset = new Point3(0, i - p.Y, 0)
				});
			}
			if (list.Count != 0 && m_subsystemMovingBlocks.AddMovingBlockSet(new Vector3(p), new Vector3(p.X, -list.Count - 1, p.Z), 0f, 10f, 0.7f, new Vector2(0f), list, "CollapsingBlock", null, testCollision: true) != null)
			{
				foreach (MovingBlock item in list)
				{
					Point3 point = p + item.Offset;
					base.SubsystemTerrain.ChangeCell(point.X, point.Y, point.Z, 0);
				}
			}
		}

		public static bool IsCollapsibleBlock(int value)
		{
			return m_handledBlocks.Contains(Terrain.ExtractContents(value));
		}

		public bool IsCollapseSupportBlock(int value)
		{
			int num = Terrain.ExtractContents(value);
			if (num != 0)
			{
				int data = Terrain.ExtractData(value);
				Block block = BlocksManager.Blocks[num];
				if (block is TrapdoorBlock)
				{
					if (TrapdoorBlock.GetUpsideDown(data))
					{
						return !TrapdoorBlock.GetOpen(data);
					}
					return false;
				}
				if (block.BlockIndex == 238)
				{
					return true;
				}
				if (block.IsFaceTransparent(base.SubsystemTerrain, 4, value))
				{
					return block is SoilBlock;
				}
				return true;
			}
			return false;
		}

		public static bool IsCollapseDestructibleBlock(int value)
		{
			int num = Terrain.ExtractContents(value);
			Block block = BlocksManager.Blocks[num];
			if (block is TrapdoorBlock)
			{
				int data = Terrain.ExtractData(value);
				if (TrapdoorBlock.GetUpsideDown(data) && TrapdoorBlock.GetOpen(data))
				{
					return false;
				}
			}
			else if (block is FluidBlock)
			{
				return false;
			}
			return true;
		}
	}
}
