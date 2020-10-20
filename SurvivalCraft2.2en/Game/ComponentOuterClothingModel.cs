using Engine.Graphics;
using GameEntitySystem;
using System;
using TemplatesDatabase;

namespace Game
{
	public class ComponentOuterClothingModel : ComponentModel
	{
		public SubsystemSky m_subsystemSky;

		public ComponentHumanModel m_componentHumanModel;

		public ComponentCreature m_componentCreature;

		public override void Load(ValuesDictionary valuesDictionary, IdToEntityMap idToEntityMap)
		{
			base.Load(valuesDictionary, idToEntityMap);
			m_subsystemSky = base.Project.FindSubsystem<SubsystemSky>(throwOnError: true);
			m_componentHumanModel = base.Entity.FindComponent<ComponentHumanModel>(throwOnError: true);
			m_componentCreature = base.Entity.FindComponent<ComponentCreature>(throwOnError: true);
		}

		public override void Animate()
		{
			base.Opacity = m_componentHumanModel.Opacity;
			foreach (ModelBone bone in base.Model.Bones)
			{
				ModelBone modelBone = m_componentHumanModel.Model.FindBone(bone.Name);
				SetBoneTransform(bone.Index, m_componentHumanModel.GetBoneTransform(modelBone.Index));
			}
			if (base.Opacity.HasValue && base.Opacity.Value < 1f)
			{
				bool num = m_componentCreature.ComponentBody.ImmersionFactor >= 1f;
				bool flag = m_subsystemSky.ViewUnderWaterDepth > 0f;
				if (num == flag)
				{
					RenderingMode = ModelRenderingMode.TransparentAfterWater;
				}
				else
				{
					RenderingMode = ModelRenderingMode.TransparentBeforeWater;
				}
			}
			else
			{
				RenderingMode = ModelRenderingMode.AlphaThreshold;
			}
			base.Animate();
		}

		protected override void SetModel(Model model)
		{
			base.SetModel(model);
			if (base.MeshDrawOrders.Length != 4)
			{
				throw new InvalidOperationException("Invalid number of meshes in OuterClothing model.");
			}
			base.MeshDrawOrders[0] = model.Meshes.IndexOf(model.FindMesh("Leg1"));
			base.MeshDrawOrders[1] = model.Meshes.IndexOf(model.FindMesh("Leg2"));
			base.MeshDrawOrders[2] = model.Meshes.IndexOf(model.FindMesh("Body"));
			base.MeshDrawOrders[3] = model.Meshes.IndexOf(model.FindMesh("Head"));
		}
	}
}
