using Engine;

namespace Game
{
	public class FppCamera : BasePerspectiveCamera
	{
		public override bool UsesMovementControls => false;

		public override bool IsEntityControlEnabled => true;

		public FppCamera(GameWidget gameWidget)
			: base(gameWidget)
		{
		}

		public override void Activate(Camera previousCamera)
		{
			SetupPerspectiveCamera(previousCamera.ViewPosition, previousCamera.ViewDirection, previousCamera.ViewUp);
		}

		public override void Update(float dt)
		{
			if (base.GameWidget.Target != null)
			{
				if (!base.Eye.HasValue)
				{
					Matrix matrix = Matrix.CreateFromQuaternion(base.GameWidget.Target.ComponentCreatureModel.EyeRotation);
					matrix.Translation = base.GameWidget.Target.ComponentCreatureModel.EyePosition;
					SetupPerspectiveCamera(matrix.Translation, matrix.Forward, matrix.Up);
					return;
				}
				Vector3 translation = VrManager.HmdMatrix.Translation;
				Vector3 position = base.GameWidget.Target.ComponentBody.Position;
				float y = position.Y + MathUtils.Clamp(translation.Y, 0.2f, base.GameWidget.Target.ComponentBody.BoxSize.Y - 0.1f);
				Vector3 hmdMatrixYpr = VrManager.HmdMatrixYpr;
				Vector3 vector = base.GameWidget.Target.ComponentCreatureModel.EyeRotation.ToYawPitchRoll();
				float radians = vector.X - hmdMatrixYpr.X;
				Matrix identity = Matrix.Identity;
				identity.Translation = new Vector3(position.X, y, position.Z);
				identity.OrientationMatrix = VrManager.HmdMatrix * Matrix.CreateRotationY(radians);
				identity.OrientationMatrix *= Matrix.CreateFromAxisAngle(identity.OrientationMatrix.Forward, vector.Z);
				SetupPerspectiveCamera(identity.Translation, identity.Forward, identity.Up);
			}
		}
	}
}
