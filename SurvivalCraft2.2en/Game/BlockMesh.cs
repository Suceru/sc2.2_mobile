using Engine;
using Engine.Graphics;
using Engine.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Game
{
	public class BlockMesh
	{
		public struct InternalVertex
		{
			public Vector3 Position;

			public Vector3 Normal;

			public Vector2 TextureCoordinate;
		}

		public DynamicArray<BlockMeshVertex> Vertices = new DynamicArray<BlockMeshVertex>();

		public DynamicArray<ushort> Indices = new DynamicArray<ushort>();

		public DynamicArray<sbyte> Sides;

		public BoundingBox CalculateBoundingBox()
		{
			return new BoundingBox(Vertices.Select((BlockMeshVertex v) => v.Position));
		}

		public BoundingBox CalculateBoundingBox(Matrix matrix)
		{
			return new BoundingBox(Vertices.Select((BlockMeshVertex v) => Vector3.Transform(v.Position, matrix)));
		}

		public static Matrix GetBoneAbsoluteTransform(ModelBone modelBone)
		{
			if (modelBone.ParentBone != null)
			{
				return GetBoneAbsoluteTransform(modelBone.ParentBone) * modelBone.Transform;
			}
			return modelBone.Transform;
		}

		public void AppendImageExtrusion(Image image, Rectangle bounds, Vector3 size, Color color)
		{
			BlockMesh blockMesh = new BlockMesh();
			DynamicArray<BlockMeshVertex> vertices = blockMesh.Vertices;
			DynamicArray<ushort> indices = blockMesh.Indices;
			BlockMeshVertex item = new BlockMeshVertex
			{
				Position = new Vector3(bounds.Left, bounds.Top, -1f),
				TextureCoordinates = new Vector2(bounds.Left, bounds.Top)
			};
			vertices.Add(item);
			item = new BlockMeshVertex
			{
				Position = new Vector3(bounds.Right, bounds.Top, -1f),
				TextureCoordinates = new Vector2(bounds.Right, bounds.Top)
			};
			vertices.Add(item);
			item = new BlockMeshVertex
			{
				Position = new Vector3(bounds.Left, bounds.Bottom, -1f),
				TextureCoordinates = new Vector2(bounds.Left, bounds.Bottom)
			};
			vertices.Add(item);
			item = new BlockMeshVertex
			{
				Position = new Vector3(bounds.Right, bounds.Bottom, -1f),
				TextureCoordinates = new Vector2(bounds.Right, bounds.Bottom)
			};
			vertices.Add(item);
			indices.Add((ushort)(vertices.Count - 4));
			indices.Add((ushort)(vertices.Count - 1));
			indices.Add((ushort)(vertices.Count - 3));
			indices.Add((ushort)(vertices.Count - 1));
			indices.Add((ushort)(vertices.Count - 4));
			indices.Add((ushort)(vertices.Count - 2));
			item = new BlockMeshVertex
			{
				Position = new Vector3(bounds.Left, bounds.Top, 1f),
				TextureCoordinates = new Vector2(bounds.Left, bounds.Top)
			};
			vertices.Add(item);
			item = new BlockMeshVertex
			{
				Position = new Vector3(bounds.Right, bounds.Top, 1f),
				TextureCoordinates = new Vector2(bounds.Right, bounds.Top)
			};
			vertices.Add(item);
			item = new BlockMeshVertex
			{
				Position = new Vector3(bounds.Left, bounds.Bottom, 1f),
				TextureCoordinates = new Vector2(bounds.Left, bounds.Bottom)
			};
			vertices.Add(item);
			item = new BlockMeshVertex
			{
				Position = new Vector3(bounds.Right, bounds.Bottom, 1f),
				TextureCoordinates = new Vector2(bounds.Right, bounds.Bottom)
			};
			vertices.Add(item);
			indices.Add((ushort)(vertices.Count - 4));
			indices.Add((ushort)(vertices.Count - 3));
			indices.Add((ushort)(vertices.Count - 1));
			indices.Add((ushort)(vertices.Count - 1));
			indices.Add((ushort)(vertices.Count - 2));
			indices.Add((ushort)(vertices.Count - 4));
			for (int i = bounds.Left - 1; i <= bounds.Right; i++)
			{
				int num = -1;
				for (int j = bounds.Top - 1; j <= bounds.Bottom; j++)
				{
					bool num2 = !bounds.Contains(new Point2(i, j)) || image.GetPixel(i, j) == Color.Transparent;
					bool flag = bounds.Contains(new Point2(i - 1, j)) && image.GetPixel(i - 1, j) != Color.Transparent;
					if (num2 & flag)
					{
						if (num < 0)
						{
							num = j;
						}
					}
					else if (num >= 0)
					{
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)i - 0.01f, (float)num - 0.01f, -1.01f),
							TextureCoordinates = new Vector2((float)(i - 1) + 0.01f, (float)num + 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)i - 0.01f, (float)num - 0.01f, 1.01f),
							TextureCoordinates = new Vector2((float)i - 0.01f, (float)num + 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)i - 0.01f, (float)j + 0.01f, -1.01f),
							TextureCoordinates = new Vector2((float)(i - 1) + 0.01f, (float)j - 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)i - 0.01f, (float)j + 0.01f, 1.01f),
							TextureCoordinates = new Vector2((float)i - 0.01f, (float)j - 0.01f)
						};
						vertices.Add(item);
						indices.Add((ushort)(vertices.Count - 4));
						indices.Add((ushort)(vertices.Count - 1));
						indices.Add((ushort)(vertices.Count - 3));
						indices.Add((ushort)(vertices.Count - 1));
						indices.Add((ushort)(vertices.Count - 4));
						indices.Add((ushort)(vertices.Count - 2));
						num = -1;
					}
				}
			}
			for (int k = bounds.Left - 1; k <= bounds.Right; k++)
			{
				int num3 = -1;
				for (int l = bounds.Top - 1; l <= bounds.Bottom; l++)
				{
					bool num4 = !bounds.Contains(new Point2(k, l)) || image.GetPixel(k, l) == Color.Transparent;
					bool flag2 = bounds.Contains(new Point2(k + 1, l)) && image.GetPixel(k + 1, l) != Color.Transparent;
					if (num4 & flag2)
					{
						if (num3 < 0)
						{
							num3 = l;
						}
					}
					else if (num3 >= 0)
					{
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)(k + 1) + 0.01f, (float)num3 - 0.01f, -1.01f),
							TextureCoordinates = new Vector2((float)(k + 1) + 0.01f, (float)num3 + 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)(k + 1) + 0.01f, (float)num3 - 0.01f, 1.01f),
							TextureCoordinates = new Vector2((float)(k + 2) - 0.01f, (float)num3 + 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)(k + 1) + 0.01f, (float)l + 0.01f, -1.01f),
							TextureCoordinates = new Vector2((float)(k + 1) + 0.01f, (float)l - 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)(k + 1) + 0.01f, (float)l + 0.01f, 1.01f),
							TextureCoordinates = new Vector2((float)(k + 2) - 0.01f, (float)l - 0.01f)
						};
						vertices.Add(item);
						indices.Add((ushort)(vertices.Count - 4));
						indices.Add((ushort)(vertices.Count - 3));
						indices.Add((ushort)(vertices.Count - 1));
						indices.Add((ushort)(vertices.Count - 1));
						indices.Add((ushort)(vertices.Count - 2));
						indices.Add((ushort)(vertices.Count - 4));
						num3 = -1;
					}
				}
			}
			for (int m = bounds.Top - 1; m <= bounds.Bottom; m++)
			{
				int num5 = -1;
				for (int n = bounds.Left - 1; n <= bounds.Right; n++)
				{
					bool num6 = !bounds.Contains(new Point2(n, m)) || image.GetPixel(n, m) == Color.Transparent;
					bool flag3 = bounds.Contains(new Point2(n, m - 1)) && image.GetPixel(n, m - 1) != Color.Transparent;
					if (num6 & flag3)
					{
						if (num5 < 0)
						{
							num5 = n;
						}
					}
					else if (num5 >= 0)
					{
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)num5 - 0.01f, (float)m - 0.01f, -1.01f),
							TextureCoordinates = new Vector2((float)num5 + 0.01f, (float)(m - 1) + 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)num5 - 0.01f, (float)m - 0.01f, 1.01f),
							TextureCoordinates = new Vector2((float)num5 + 0.01f, (float)m - 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)n + 0.01f, (float)m - 0.01f, -1.01f),
							TextureCoordinates = new Vector2((float)n - 0.01f, (float)(m - 1) + 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)n + 0.01f, (float)m - 0.01f, 1.01f),
							TextureCoordinates = new Vector2((float)n - 0.01f, (float)m - 0.01f)
						};
						vertices.Add(item);
						indices.Add((ushort)(vertices.Count - 4));
						indices.Add((ushort)(vertices.Count - 3));
						indices.Add((ushort)(vertices.Count - 1));
						indices.Add((ushort)(vertices.Count - 1));
						indices.Add((ushort)(vertices.Count - 2));
						indices.Add((ushort)(vertices.Count - 4));
						num5 = -1;
					}
				}
			}
			for (int num7 = bounds.Top - 1; num7 <= bounds.Bottom; num7++)
			{
				int num8 = -1;
				for (int num9 = bounds.Left - 1; num9 <= bounds.Right; num9++)
				{
					bool num10 = !bounds.Contains(new Point2(num9, num7)) || image.GetPixel(num9, num7) == Color.Transparent;
					bool flag4 = bounds.Contains(new Point2(num9, num7 + 1)) && image.GetPixel(num9, num7 + 1) != Color.Transparent;
					if (num10 & flag4)
					{
						if (num8 < 0)
						{
							num8 = num9;
						}
					}
					else if (num8 >= 0)
					{
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)num8 - 0.01f, (float)(num7 + 1) + 0.01f, -1.01f),
							TextureCoordinates = new Vector2((float)num8 + 0.01f, (float)(num7 + 1) + 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)num8 - 0.01f, (float)(num7 + 1) + 0.01f, 1.01f),
							TextureCoordinates = new Vector2((float)num8 + 0.01f, (float)(num7 + 2) - 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)num9 + 0.01f, (float)(num7 + 1) + 0.01f, -1.01f),
							TextureCoordinates = new Vector2((float)num9 - 0.01f, (float)(num7 + 1) + 0.01f)
						};
						vertices.Add(item);
						item = new BlockMeshVertex
						{
							Position = new Vector3((float)num9 + 0.01f, (float)(num7 + 1) + 0.01f, 1.01f),
							TextureCoordinates = new Vector2((float)num9 - 0.01f, (float)(num7 + 2) - 0.01f)
						};
						vertices.Add(item);
						indices.Add((ushort)(vertices.Count - 4));
						indices.Add((ushort)(vertices.Count - 1));
						indices.Add((ushort)(vertices.Count - 3));
						indices.Add((ushort)(vertices.Count - 1));
						indices.Add((ushort)(vertices.Count - 4));
						indices.Add((ushort)(vertices.Count - 2));
						num8 = -1;
					}
				}
			}
			for (int num11 = 0; num11 < vertices.Count; num11++)
			{
				vertices.Array[num11].Position.X -= (float)bounds.Left + (float)bounds.Width / 2f;
				vertices.Array[num11].Position.Y = (float)bounds.Bottom - vertices.Array[num11].Position.Y - (float)bounds.Height / 2f;
				vertices.Array[num11].Position.X *= size.X / (float)bounds.Width;
				vertices.Array[num11].Position.Y *= size.Y / (float)bounds.Height;
				vertices.Array[num11].Position.Z *= size.Z / 2f;
				vertices.Array[num11].TextureCoordinates.X /= image.Width;
				vertices.Array[num11].TextureCoordinates.Y /= image.Height;
				vertices.Array[num11].Color = color;
			}
			AppendBlockMesh(blockMesh);
		}

		public void AppendModelMeshPart(ModelMeshPart meshPart, Matrix matrix, bool makeEmissive, bool flipWindingOrder, bool doubleSided, bool flipNormals, Color color)
		{
			VertexBuffer vertexBuffer = meshPart.VertexBuffer;
			IndexBuffer indexBuffer = meshPart.IndexBuffer;
			ReadOnlyList<VertexElement> vertexElements = vertexBuffer.VertexDeclaration.VertexElements;
			if (vertexElements.Count != 3 || vertexElements[0].Offset != 0 || vertexElements[0].Semantic != VertexElementSemantic.Position.GetSemanticString() || vertexElements[1].Offset != 12 || vertexElements[1].Semantic != VertexElementSemantic.Normal.GetSemanticString() || vertexElements[2].Offset != 24 || vertexElements[2].Semantic != VertexElementSemantic.TextureCoordinate.GetSemanticString())
			{
				throw new InvalidOperationException("Wrong vertex format for a block mesh.");
			}
			InternalVertex[] vertexData = GetVertexData<InternalVertex>(vertexBuffer);
			ushort[] indexData = GetIndexData<ushort>(indexBuffer);
			Dictionary<ushort, ushort> dictionary = new Dictionary<ushort, ushort>();
			for (int i = meshPart.StartIndex; i < meshPart.StartIndex + meshPart.IndicesCount; i++)
			{
				ushort num = indexData[i];
				if (!dictionary.ContainsKey(num))
				{
					dictionary.Add(num, (ushort)Vertices.Count);
					BlockMeshVertex item = default(BlockMeshVertex);
					item.Position = Vector3.Transform(vertexData[num].Position, matrix);
					item.TextureCoordinates = vertexData[num].TextureCoordinate;
					Vector3 vector = Vector3.Normalize(Vector3.TransformNormal(flipNormals ? (-vertexData[num].Normal) : vertexData[num].Normal, matrix));
					if (makeEmissive)
					{
						item.IsEmissive = true;
						item.Color = color;
					}
					else
					{
						item.Color = color * LightingManager.CalculateLighting(vector);
						item.Color.A = color.A;
					}
					item.Face = (byte)CellFace.Vector3ToFace(vector);
					Vertices.Add(item);
				}
			}
			for (int j = 0; j < meshPart.IndicesCount / 3; j++)
			{
				if (doubleSided)
				{
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j]]);
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 1]]);
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 2]]);
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j]]);
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 2]]);
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 1]]);
				}
				else if (flipWindingOrder)
				{
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j]]);
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 2]]);
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 1]]);
				}
				else
				{
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j]]);
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 1]]);
					Indices.Add(dictionary[indexData[meshPart.StartIndex + 3 * j + 2]]);
				}
			}
			Trim();
		}

		public void AppendBlockMesh(BlockMesh blockMesh)
		{
			int count = Vertices.Count;
			for (int i = 0; i < blockMesh.Vertices.Count; i++)
			{
				Vertices.Add(blockMesh.Vertices.Array[i]);
			}
			for (int j = 0; j < blockMesh.Indices.Count; j++)
			{
				Indices.Add((ushort)(blockMesh.Indices.Array[j] + count));
			}
			Trim();
		}

		public void BlendBlockMesh(BlockMesh blockMesh, float factor)
		{
			if (blockMesh.Vertices.Count != Vertices.Count)
			{
				throw new InvalidOperationException("Meshes do not match.");
			}
			for (int i = 0; i < Vertices.Count; i++)
			{
				Vector3 position = Vertices.Array[i].Position;
				Vector3 position2 = blockMesh.Vertices.Array[i].Position;
				Vertices.Array[i].Position = Vector3.Lerp(position, position2, factor);
			}
		}

		public void TransformPositions(Matrix matrix, int facesMask = -1)
		{
			for (int i = 0; i < Vertices.Count; i++)
			{
				if (((1 << (int)Vertices.Array[i].Face) & facesMask) != 0)
				{
					Vertices.Array[i].Position = Vector3.Transform(Vertices.Array[i].Position, matrix);
				}
			}
		}

		public void TransformTextureCoordinates(Matrix matrix, int facesMask = -1)
		{
			for (int i = 0; i < Vertices.Count; i++)
			{
				if (((1 << (int)Vertices.Array[i].Face) & facesMask) != 0)
				{
					Vertices.Array[i].TextureCoordinates = Vector2.Transform(Vertices.Array[i].TextureCoordinates, matrix);
				}
			}
		}

		public void SetColor(Color color, int facesMask = -1)
		{
			for (int i = 0; i < Vertices.Count; i++)
			{
				if (((1 << (int)Vertices.Array[i].Face) & facesMask) != 0)
				{
					Vertices.Array[i].Color = color;
				}
			}
		}

		public void ModulateColor(Color color, int facesMask = -1)
		{
			for (int i = 0; i < Vertices.Count; i++)
			{
				if (((1 << (int)Vertices.Array[i].Face) & facesMask) != 0)
				{
					Vertices.Array[i].Color *= color;
				}
			}
		}

		public void GenerateSidesData()
		{
			Sides = new DynamicArray<sbyte>();
			Sides.Count = Indices.Count / 3;
			for (int i = 0; i < Sides.Count; i++)
			{
				int num = Indices.Array[3 * i];
				int num2 = Indices.Array[3 * i + 1];
				int num3 = Indices.Array[3 * i + 2];
				Vector3 position = Vertices.Array[num].Position;
				Vector3 position2 = Vertices.Array[num2].Position;
				Vector3 position3 = Vertices.Array[num3].Position;
				if (IsNear(position.Z, position2.Z, position3.Z, 1f))
				{
					Sides.Array[i] = 0;
				}
				else if (IsNear(position.X, position2.X, position3.X, 1f))
				{
					Sides.Array[i] = 1;
				}
				else if (IsNear(position.Z, position2.Z, position3.Z, 0f))
				{
					Sides.Array[i] = 2;
				}
				else if (IsNear(position.X, position2.X, position3.X, 0f))
				{
					Sides.Array[i] = 3;
				}
				else if (IsNear(position.Y, position2.Y, position3.Y, 1f))
				{
					Sides.Array[i] = 4;
				}
				else if (IsNear(position.Y, position2.Y, position3.Y, 0f))
				{
					Sides.Array[i] = 5;
				}
				else
				{
					Sides.Array[i] = -1;
				}
			}
		}

		public void Trim()
		{
			Vertices.Capacity = Vertices.Count;
			Indices.Capacity = Indices.Count;
			if (Sides != null)
			{
				Sides.Capacity = Sides.Count;
			}
		}

		public static T[] GetVertexData<T>(VertexBuffer vertexBuffer)
		{
			byte[] array = vertexBuffer.Tag as byte[];
			if (array == null)
			{
				throw new InvalidOperationException("VertexBuffer does not contain source data in Tag.");
			}
			if (array.Length % Utilities.SizeOf<T>() != 0)
			{
				throw new InvalidOperationException("VertexBuffer data size is not a whole multiply of target type size.");
			}
			T[] array2 = new T[array.Length / Utilities.SizeOf<T>()];
			GCHandle gCHandle = GCHandle.Alloc(array2, GCHandleType.Pinned);
			try
			{
				Marshal.Copy(array, 0, gCHandle.AddrOfPinnedObject(), Utilities.SizeOf<T>() * array2.Length);
				return array2;
			}
			finally
			{
				gCHandle.Free();
			}
		}

		public static T[] GetIndexData<T>(IndexBuffer indexBuffer)
		{
			byte[] array = indexBuffer.Tag as byte[];
			if (array == null)
			{
				throw new InvalidOperationException("IndexBuffer does not contain source data in Tag.");
			}
			if (array.Length % Utilities.SizeOf<T>() != 0)
			{
				throw new InvalidOperationException("IndexBuffer data size is not a whole multiply of target type size.");
			}
			T[] array2 = new T[array.Length / Utilities.SizeOf<T>()];
			GCHandle gCHandle = GCHandle.Alloc(array2, GCHandleType.Pinned);
			try
			{
				Marshal.Copy(array, 0, gCHandle.AddrOfPinnedObject(), Utilities.SizeOf<T>() * array2.Length);
				return array2;
			}
			finally
			{
				gCHandle.Free();
			}
		}

		public static bool IsNear(float v1, float v2, float v3, float t)
		{
			if (v1 - t >= -0.001f && v1 - t <= 0.001f && v2 - t >= -0.001f && v2 - t <= 0.001f && v3 - t >= -0.001f)
			{
				return v3 - t <= 0.001f;
			}
			return false;
		}
	}
}
