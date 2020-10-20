using Engine;
using Engine.Graphics;
using Engine.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Game
{
	public static class BlocksManager
	{
		public static Block[] m_blocks;

		public static FluidBlock[] m_fluidBlocks;

		public static List<string> m_categories = new List<string>();

		public static DrawBlockEnvironmentData m_defaultEnvironmentData = new DrawBlockEnvironmentData();

		public static Vector4[] m_slotTexCoords = new Vector4[256];

		public static Block[] Blocks => m_blocks;

		public static FluidBlock[] FluidBlocks => m_fluidBlocks;
		public static Action Initialize1;
		public static ReadOnlyList<string> Categories => new ReadOnlyList<string>(m_categories);
		public static Func<IEnumerable<TypeInfo>> GetBlockTypes1;
		public static IEnumerable<TypeInfo> GetBlockTypes()
		{
			Func<IEnumerable<TypeInfo>> getBlockTypes = GetBlockTypes1;
			if (getBlockTypes != null)
			{
				return getBlockTypes();
			}
			List<TypeInfo> list = new List<TypeInfo>();
			list.AddRange(typeof(BlocksManager).Assembly.DefinedTypes);
			ReadOnlyList<Assembly>.Enumerator enumerator = TypeCache.LoadedAssemblies.GetEnumerator();
			while (enumerator.MoveNext())
			{
				list.AddRange(enumerator.Current.DefinedTypes);
			}
			return list;
		}
		public sealed class c
		{
			public static readonly c _ = new c();

			public static Func<FieldInfo, bool> __11_0;

			public bool Initialize_b__11_0(FieldInfo fi)
			{
				if (fi.Name == "Index" && fi.IsPublic)
				{
					return fi.IsStatic;
				}
				return false;
			}
		}


		public static void Initialize()
		{
			if (Initialize1 != null)
			{
				Initialize1();
				return;
			}
			CalculateSlotTexCoordTables();
			int num = 0;
			Dictionary<int, Block> dictionary = new Dictionary<int, Block>();
			IEnumerable<TypeInfo> jl = GetBlockTypes();
			foreach (TypeInfo blockType in jl)
			{
				if (blockType.IsSubclassOf(typeof(Block)) && !blockType.IsAbstract)
				{
					FieldInfo fieldInfo = blockType.AsType().GetRuntimeFields().FirstOrDefault(c._.Initialize_b__11_0);
					if (!(fieldInfo != null) || !(fieldInfo.FieldType == typeof(int)))
					{
						throw new InvalidOperationException($"Block type \"{blockType.FullName}\" does not have static field Index of type int.");
					}
					int num2 = (int)fieldInfo.GetValue(null);
					Block block = (Block)Activator.CreateInstance(blockType.AsType());
					dictionary[block.BlockIndex = num2] = block;
					if (num2 > num)
					{
						num = num2;
					}
				}
			}
			m_blocks = new Block[num + 1];
			m_fluidBlocks = new FluidBlock[num + 1];

			foreach (KeyValuePair<int, Block> current2 in dictionary) {
				m_blocks[current2.Key] = current2.Value;//添加普通方块
				m_fluidBlocks[current2.Key] = (current2.Value as FluidBlock);//添加流体方块
			}

			for (num = 0; num < m_blocks.Length; num++)
			{
				if (m_blocks[num] == null)
				{
					m_blocks[num] = Blocks[0];
				}
			}
			string data = ContentManager.Get<string>("BlocksData");
			ContentManager.Dispose("BlocksData");
			LoadBlocksData(data);
			List<FileEntry> blocklist= ModsManager.GetEntries(".csv");
			foreach (FileEntry file in blocklist) {
				file.Stream.Position = 0L;
				StreamReader reader = new StreamReader(file.Stream);
				string text = reader.ReadToEnd();
				LoadBlocksData(text);
			}
			Block[] blocks = Blocks;
			for (int j = 0; j < blocks.Length; j++)
			{
				blocks[j].Initialize();
			}
			m_categories.Add("Terrain");
			m_categories.Add("Plants");
			m_categories.Add("Construction");
			m_categories.Add("Items");
			m_categories.Add("Tools");
			m_categories.Add("Weapons");
			m_categories.Add("Clothes");
			m_categories.Add("Electrics");
			m_categories.Add("Food");
			m_categories.Add("Spawner_Eggs");
			m_categories.Add("Painted");
			m_categories.Add("Dyed");
			m_categories.Add("Fireworks");
			blocks = Blocks;
			foreach (Block block2 in blocks)
			{
				foreach (int creativeValue in block2.GetCreativeValues())
				{
					string category = block2.GetCategory(creativeValue);
					if (!m_categories.Contains(category))
					{
						m_categories.Add(category);
					}
				}
			}
		}

		public static Block FindBlockByTypeName(string typeName, bool throwIfNotFound)
		{
			Block block = Blocks.FirstOrDefault((Block b) => b.GetType().Name == typeName);
			if (block == null && throwIfNotFound)
			{
				throw new InvalidOperationException(string.Format("block type {0} not found",typeName));
			}
			return block;
		}

		public static Block[] FindBlocksByCraftingId(string craftingId)
		{
			return Blocks.Where((Block b) => b.CraftingId == craftingId).ToArray();
		}

		public static void DrawCubeBlock(PrimitivesRenderer3D primitivesRenderer, int value, Vector3 size, ref Matrix matrix, Color color, Color topColor, DrawBlockEnvironmentData environmentData)
		{
			environmentData = (environmentData ?? m_defaultEnvironmentData);
			Texture2D texture = (environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture;
			TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(texture, useAlphaTest: true, 0, null, RasterizerState.CullCounterClockwiseScissor, null, SamplerState.PointClamp);
			float s = LightingManager.LightIntensityByLightValue[environmentData.Light];
			color = Color.MultiplyColorOnly(color, s);
			topColor = Color.MultiplyColorOnly(topColor, s);
			Vector3 translation = matrix.Translation;
			Vector3 vector = matrix.Right * size.X;
			Vector3 v = matrix.Up * size.Y;
			Vector3 v2 = matrix.Forward * size.Z;
			Vector3 v3 = translation + 0.5f * (-vector - v - v2);
			Vector3 v4 = translation + 0.5f * (vector - v - v2);
			Vector3 v5 = translation + 0.5f * (-vector + v - v2);
			Vector3 v6 = translation + 0.5f * (vector + v - v2);
			Vector3 v7 = translation + 0.5f * (-vector - v + v2);
			Vector3 v8 = translation + 0.5f * (vector - v + v2);
			Vector3 v9 = translation + 0.5f * (-vector + v + v2);
			Vector3 v10 = translation + 0.5f * (vector + v + v2);
			if (environmentData.ViewProjectionMatrix.HasValue)
			{
				Matrix m = environmentData.ViewProjectionMatrix.Value;
				Vector3.Transform(ref v3, ref m, out v3);
				Vector3.Transform(ref v4, ref m, out v4);
				Vector3.Transform(ref v5, ref m, out v5);
				Vector3.Transform(ref v6, ref m, out v6);
				Vector3.Transform(ref v7, ref m, out v7);
				Vector3.Transform(ref v8, ref m, out v8);
				Vector3.Transform(ref v9, ref m, out v9);
				Vector3.Transform(ref v10, ref m, out v10);
			}
			int num = Terrain.ExtractContents(value);
			Block block = Blocks[num];
			Vector4 vector2 = m_slotTexCoords[block.GetFaceTextureSlot(0, value)];
			texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(color, LightingManager.CalculateLighting(-matrix.Forward)), p1: v3, p2: v5, p3: v6, p4: v4, texCoord1: new Vector2(vector2.X, vector2.W), texCoord2: new Vector2(vector2.X, vector2.Y), texCoord3: new Vector2(vector2.Z, vector2.Y), texCoord4: new Vector2(vector2.Z, vector2.W));
			vector2 = m_slotTexCoords[block.GetFaceTextureSlot(2, value)];
			texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(color, LightingManager.CalculateLighting(matrix.Forward)), p1: v7, p2: v8, p3: v10, p4: v9, texCoord1: new Vector2(vector2.Z, vector2.W), texCoord2: new Vector2(vector2.X, vector2.W), texCoord3: new Vector2(vector2.X, vector2.Y), texCoord4: new Vector2(vector2.Z, vector2.Y));
			vector2 = m_slotTexCoords[block.GetFaceTextureSlot(5, value)];
			texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(color, LightingManager.CalculateLighting(-matrix.Up)), p1: v3, p2: v4, p3: v8, p4: v7, texCoord1: new Vector2(vector2.X, vector2.Y), texCoord2: new Vector2(vector2.Z, vector2.Y), texCoord3: new Vector2(vector2.Z, vector2.W), texCoord4: new Vector2(vector2.X, vector2.W));
			vector2 = m_slotTexCoords[block.GetFaceTextureSlot(4, value)];
			texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(topColor, LightingManager.CalculateLighting(matrix.Up)), p1: v5, p2: v9, p3: v10, p4: v6, texCoord1: new Vector2(vector2.X, vector2.W), texCoord2: new Vector2(vector2.X, vector2.Y), texCoord3: new Vector2(vector2.Z, vector2.Y), texCoord4: new Vector2(vector2.Z, vector2.W));
			vector2 = m_slotTexCoords[block.GetFaceTextureSlot(1, value)];
			texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(color, LightingManager.CalculateLighting(-matrix.Right)), p1: v3, p2: v7, p3: v9, p4: v5, texCoord1: new Vector2(vector2.Z, vector2.W), texCoord2: new Vector2(vector2.X, vector2.W), texCoord3: new Vector2(vector2.X, vector2.Y), texCoord4: new Vector2(vector2.Z, vector2.Y));
			vector2 = m_slotTexCoords[block.GetFaceTextureSlot(3, value)];
			texturedBatch3D.QueueQuad(color: Color.MultiplyColorOnly(color, LightingManager.CalculateLighting(matrix.Right)), p1: v4, p2: v6, p3: v10, p4: v8, texCoord1: new Vector2(vector2.X, vector2.W), texCoord2: new Vector2(vector2.X, vector2.Y), texCoord3: new Vector2(vector2.Z, vector2.Y), texCoord4: new Vector2(vector2.Z, vector2.W));
		}

		public static void DrawFlatBlock(PrimitivesRenderer3D primitivesRenderer, int value, float size, ref Matrix matrix, Texture2D texture, Color color, bool isEmissive, DrawBlockEnvironmentData environmentData)
		{
			environmentData = (environmentData ?? m_defaultEnvironmentData);
			if (!isEmissive)
			{
				float s = LightingManager.LightIntensityByLightValue[environmentData.Light];
				color = Color.MultiplyColorOnly(color, s);
			}
			Vector3 translation = matrix.Translation;
			Vector3 vector;
			Vector3 v;
			if (environmentData.BillboardDirection.HasValue)
			{
				vector = Vector3.Normalize(Vector3.Cross(environmentData.BillboardDirection.Value, Vector3.UnitY));
				v = -Vector3.Normalize(Vector3.Cross(environmentData.BillboardDirection.Value, vector));
			}
			else
			{
				vector = matrix.Right;
				v = matrix.Up;
			}
			Vector3 v2 = translation + 0.85f * size * (-vector - v);
			Vector3 v3 = translation + 0.85f * size * (vector - v);
			Vector3 v4 = translation + 0.85f * size * (-vector + v);
			Vector3 v5 = translation + 0.85f * size * (vector + v);
			if (environmentData.ViewProjectionMatrix.HasValue)
			{
				Matrix m = environmentData.ViewProjectionMatrix.Value;
				Vector3.Transform(ref v2, ref m, out v2);
				Vector3.Transform(ref v3, ref m, out v3);
				Vector3.Transform(ref v4, ref m, out v4);
				Vector3.Transform(ref v5, ref m, out v5);
			}
			int num = Terrain.ExtractContents(value);
			Block block = Blocks[num];
			Vector4 vector2;
			if (texture != null)
			{
				vector2 = new Vector4(0f, 0f, 1f, 1f);
			}
			else
			{
				texture = ((environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture);
				vector2 = m_slotTexCoords[block.GetFaceTextureSlot(-1, value)];
			}
			TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(texture, useAlphaTest: true, 0, null, RasterizerState.CullCounterClockwiseScissor, null, SamplerState.PointClamp);
			texturedBatch3D.QueueQuad(v2, v4, v5, v3, new Vector2(vector2.X, vector2.W), new Vector2(vector2.X, vector2.Y), new Vector2(vector2.Z, vector2.Y), new Vector2(vector2.Z, vector2.W), color);
			if (!environmentData.BillboardDirection.HasValue)
			{
				texturedBatch3D.QueueQuad(v2, v3, v5, v4, new Vector2(vector2.X, vector2.W), new Vector2(vector2.Z, vector2.W), new Vector2(vector2.Z, vector2.Y), new Vector2(vector2.X, vector2.Y), color);
			}
		}

		public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer, BlockMesh blockMesh, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			environmentData = (environmentData ?? m_defaultEnvironmentData);
			Texture2D texture = (environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture;
			DrawMeshBlock(primitivesRenderer, blockMesh, texture, Color.White, size, ref matrix, environmentData);
		}

		public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer, BlockMesh blockMesh, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			environmentData = (environmentData ?? m_defaultEnvironmentData);
			Texture2D texture = (environmentData.SubsystemTerrain != null) ? environmentData.SubsystemTerrain.SubsystemAnimatedTextures.AnimatedBlocksTexture : BlocksTexturesManager.DefaultBlocksTexture;
			DrawMeshBlock(primitivesRenderer, blockMesh, texture, color, size, ref matrix, environmentData);
		}

		public static void DrawMeshBlock(PrimitivesRenderer3D primitivesRenderer, BlockMesh blockMesh, Texture2D texture, Color color, float size, ref Matrix matrix, DrawBlockEnvironmentData environmentData)
		{
			environmentData = (environmentData ?? m_defaultEnvironmentData);
			float num = LightingManager.LightIntensityByLightValue[environmentData.Light];
			Vector4 v = new Vector4(color);
			v.X *= num;
			v.Y *= num;
			v.Z *= num;
			bool flag = v == Vector4.One;
			TexturedBatch3D texturedBatch3D = primitivesRenderer.TexturedBatch(texture, useAlphaTest: true, 0, null, RasterizerState.CullCounterClockwiseScissor, null, SamplerState.PointClamp);
			bool flag2 = false;
			Matrix m = (!environmentData.ViewProjectionMatrix.HasValue) ? matrix : (matrix * environmentData.ViewProjectionMatrix.Value);
			if (size != 1f)
			{
				m = Matrix.CreateScale(size) * m;
			}
			if (m.M14 != 0f || m.M24 != 0f || m.M34 != 0f || m.M44 != 1f)
			{
				flag2 = true;
			}
			int count = blockMesh.Vertices.Count;
			BlockMeshVertex[] array = blockMesh.Vertices.Array;
			int count2 = blockMesh.Indices.Count;
			ushort[] array2 = blockMesh.Indices.Array;
			DynamicArray<VertexPositionColorTexture> triangleVertices = texturedBatch3D.TriangleVertices;
			int count3 = triangleVertices.Count;
			int count4 = triangleVertices.Count;
			triangleVertices.Count += count;
			for (int i = 0; i < count; i++)
			{
				BlockMeshVertex blockMeshVertex = array[i];
				if (flag2)
				{
					Vector4 v2 = new Vector4(blockMeshVertex.Position, 1f);
					Vector4.Transform(ref v2, ref m, out v2);
					float num2 = 1f / v2.W;
					blockMeshVertex.Position = new Vector3(v2.X * num2, v2.Y * num2, v2.Z * num2);
				}
				else
				{
					Vector3.Transform(ref blockMeshVertex.Position, ref m, out blockMeshVertex.Position);
				}
				if (flag || blockMeshVertex.IsEmissive)
				{
					triangleVertices.Array[count4++] = new VertexPositionColorTexture(blockMeshVertex.Position, blockMeshVertex.Color, blockMeshVertex.TextureCoordinates);
					continue;
				}
				Color color2 = new Color((byte)((float)(int)blockMeshVertex.Color.R * v.X), (byte)((float)(int)blockMeshVertex.Color.G * v.Y), (byte)((float)(int)blockMeshVertex.Color.B * v.Z), (byte)((float)(int)blockMeshVertex.Color.A * v.W));
				triangleVertices.Array[count4++] = new VertexPositionColorTexture(blockMeshVertex.Position, color2, blockMeshVertex.TextureCoordinates);
			}
			DynamicArray<ushort> triangleIndices = texturedBatch3D.TriangleIndices;
			int count5 = triangleIndices.Count;
			triangleIndices.Count += count2;
			for (int j = 0; j < count2; j++)
			{
				triangleIndices.Array[count5++] = (ushort)(count3 + array2[j]);
			}
		}

		public static int DamageItem(int value, int damageCount)
		{
			int num = Terrain.ExtractContents(value);
			Block block = Blocks[num];
			if (block.Durability >= 0)
			{
				int num2 = block.GetDamage(value) + damageCount;
				if (num2 <= block.Durability)
				{
					return block.SetDamage(value, num2);
				}
				return block.GetDamageDestructionValue(value);
			}
			return value;
		}

		public static void LoadBlocksData(string data)
		{
			Dictionary<Block, bool> dictionary = new Dictionary<Block, bool>();
			data = data.Replace("\r", string.Empty);
			string[] array = data.Split(new char[1]
			{
				'\n'
			}, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = null;
			for (int i = 0; i < array.Length; i++)
			{
				string[] array3 = array[i].Split(';');
				if (i == 0)
				{
					array2 = new string[array3.Length - 1];
					Array.Copy(array3, 1, array2, 0, array3.Length - 1);
					continue;
				}
				if (array3.Length != array2.Length + 1)
				{
					throw new InvalidOperationException(string.Format("Not enough field values for block \"{0}\".", (array3.Length != 0) ? array3[0] : "unknown"));
				}
				string typeName = array3[0];
				if (string.IsNullOrEmpty(typeName))
				{
					continue;
				}
				Block block = m_blocks.FirstOrDefault((Block v) => v.GetType().Name == typeName);
				if (block == null)
				{
					throw new InvalidOperationException($"Block \"{typeName}\" not found when loading block data.");
				}
				if (dictionary.ContainsKey(block))
				{
					throw new InvalidOperationException($"Data for block \"{typeName}\" specified more than once when loading block data.");
				}
				dictionary.Add(block, value: true);
				Dictionary<string, FieldInfo> dictionary2 = new Dictionary<string, FieldInfo>();
				foreach (FieldInfo runtimeField in block.GetType().GetRuntimeFields())
				{
					if (runtimeField.IsPublic && !runtimeField.IsStatic)
					{
						dictionary2.Add(runtimeField.Name, runtimeField);
					}
				}
				for (int j = 1; j < array3.Length; j++)
				{
					string text = array2[j - 1];
					string text2 = array3[j];
					if (!string.IsNullOrEmpty(text2))
					{
						if (!dictionary2.TryGetValue(text, out FieldInfo value))
						{
							throw new InvalidOperationException($"Field \"{text}\" not found or not accessible when loading block data.");
						}
						object obj = null;
						if (text2.StartsWith("#"))
						{
							string refTypeName = text2.Substring(1);
							obj = ((!string.IsNullOrEmpty(refTypeName)) ? ((object)(m_blocks.FirstOrDefault((Block v) => v.GetType().Name == refTypeName) ?? throw new InvalidOperationException($"Reference block \"{refTypeName}\" not found when loading block data.")).BlockIndex) : ((object)block.BlockIndex));
						}
						else
						{
							obj = HumanReadableConverter.ConvertFromString(value.FieldType, text2);
						}
						value.SetValue(block, obj);
					}
				}
			}
			/*//不在字典的block抛出错误?
			using (IEnumerator<Block> enumerator2 = m_blocks.Except(dictionary.Keys).GetEnumerator())
			{
				if (enumerator2.MoveNext())
				{
					Block current2 = enumerator2.Current;
					throw new InvalidOperationException($"Data for block \"{current2.GetType().Name}\" not found when loading blocks data.");
				}
			}
			*/
		}

		public static void CalculateSlotTexCoordTables()
		{
			for (int i = 0; i < 256; i++)
			{
				m_slotTexCoords[i] = TextureSlotToTextureCoords(i);
			}
		}

		public static Vector4 TextureSlotToTextureCoords(int slot)
		{
			int num = slot % 16;
			int num2 = slot / 16;
			float x = ((float)num + 0.001f) / 16f;
			float y = ((float)num2 + 0.001f) / 16f;
			float z = ((float)(num + 1) - 0.001f) / 16f;
			float w = ((float)(num2 + 1) - 0.001f) / 16f;
			return new Vector4(x, y, z, w);
		}
	}
}
