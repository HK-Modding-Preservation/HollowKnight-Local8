using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace KO.HollowKnight8;

internal sealed class GroupVignette : IDisposable
{
	private GameObject root;

	private Mesh mesh;

	private MeshRenderer renderer;

	private Material material;

	private Material sourceMaterial;

	private readonly MaterialPropertyBlock properties = new MaterialPropertyBlock();

	private Sprite sprite;

	private Vector2[] spriteVertices;

	private Vector2[] spriteUV;

	private ushort[] spriteTriangles;

	private readonly List<Vector3> vertices = new List<Vector3>(1024);

	private readonly List<Vector2> uvs = new List<Vector2>(1024);

	private readonly List<Color> colors = new List<Color>(1024);

	private readonly List<int> triangles = new List<int>(1536);

	private readonly LightVertex[] clipA = new LightVertex[24];

	private readonly LightVertex[] clipB = new LightVertex[24];

	private readonly Vector2[] centres = new Vector2[8];

	private readonly Vector2[] oldCentres = new Vector2[8];

	private readonly LightPoint[] blendCentres = new LightPoint[8];

	private readonly LightBlendGrid blendGrid = new LightBlendGrid();

	private readonly List<LightWeightedVertex> blendVertices = new List<LightWeightedVertex>(1024);

	private readonly LightEdgeFade edgeFade = new LightEdgeFade();

	private LightPoint[] projectedRim;
	private LightVertex[] projectedVertices;

	private Vector2 lightShift;

	private float viewWidth;

	private float viewHeight;

	private int oldCount;

	private bool built;

	private bool lastFlipX;

	private bool lastFlipY;
	private bool lastReducedDetail;
	private bool lastSmooth;

	private Matrix4x4 previousProjection;

	private Matrix4x4 previousCamera;

	private Matrix4x4 previousSource;

	private Color previousTint;

	private Vector2 previousPrimary;

	private SpriteRenderer hidden;

	private bool wasEnabled;

	private Camera renderingCamera;

	internal string Status = "unprepared";

	internal bool Render(Camera camera, Camera gameCamera, CoopSession session, SpriteRenderer source, bool light)
	{
		Restore();
		if (!source || !source.sprite || !source.sharedMaterial)
		{
			Status = "missing asset";
			return false;
		}
		if ((camera.cullingMask & (1 << source.gameObject.layer)) == 0)
		{
			Status = "other camera layer=" + source.gameObject.layer;
			return false;
		}
		if (!source.gameObject.activeInHierarchy || source.color.a < 0.001f)
		{
			Status = "inactive/transparent";
			return false;
		}
		bool value = source.enabled;
		PlayerSlot primary = session.Primary;
		if (!value && (primary.Down || primary.Hazard))
		{
			primary.RendererStates.TryGetValue(source, out value);
		}
		if (!value)
		{
			Status = "disabled";
			return false;
		}
		float num = Vector3.Dot(source.bounds.center - camera.transform.position, camera.transform.forward);
		if (num <= camera.nearClipPlane || num >= camera.farClipPlane)
		{
			Status = "outside depth=" + num + " near=" + camera.nearClipPlane;
			return false;
		}
		Prepare(source);
		int num2 = 0;
		Vector2 vector = gameCamera.WorldToViewportPoint(primary.Hero.transform.position);
		foreach (PlayerSlot player in session.Players)
		{
			if (!player.Alive || !player.Ready || TransitionVote.Holding(player) || num2 >= 8)
			{
				continue;
			}
			centres[num2++] = gameCamera.WorldToViewportPoint(player.Hero.transform.position);
		}
		if (num2 == 0)
		{
			Status = "no living players";
			return false;
		}
		Matrix4x4 projectionMatrix = camera.projectionMatrix;
		Matrix4x4 localToWorldMatrix = camera.transform.localToWorldMatrix;
		Matrix4x4 localToWorldMatrix2 = source.transform.localToWorldMatrix;
		bool reducedDetail = PerformanceTuning.Detail > 0;
		bool smooth = Plugin.Self.SmoothPlayerLights.Value;
		bool flag2 = !built || oldCount != num2 || projectionMatrix != previousProjection || localToWorldMatrix != previousCamera || localToWorldMatrix2 != previousSource || source.color != previousTint || source.flipX != lastFlipX || source.flipY != lastFlipY || vector != previousPrimary || reducedDetail != lastReducedDetail || smooth != lastSmooth;
		for (int j = 0; j < num2; j++)
		{
			if (flag2)
			{
				break;
			}
			if (centres[j] != oldCentres[j])
			{
				flag2 = true;
			}
		}
		if (flag2)
		{
			vertices.Clear();
			uvs.Clear();
			colors.Clear();
			triangles.Clear();
			float z = Vector3.Dot(primary.Hero.transform.position - gameCamera.transform.position, gameCamera.transform.forward);
			Vector3 vector3 = gameCamera.ViewportToWorldPoint(new Vector3(0f, 0f, z));
			Vector3 vector4 = gameCamera.ViewportToWorldPoint(new Vector3(1f, 1f, z));
			viewWidth = Mathf.Abs(vector4.x - vector3.x);
			viewHeight = Mathf.Abs(vector4.y - vector3.y);
			for (int k = 0; k < num2; k++)
			{
				blendCentres[k] = new LightPoint(centres[k].x * viewWidth, centres[k].y * viewHeight);
			}
			if (num2 > 1)
			{
				blendGrid.Prepare(blendCentres, num2, viewWidth, viewHeight, 3.5, reducedDetail, smooth);
			}
			if (projectedVertices == null || projectedVertices.Length != spriteVertices.Length)
				projectedVertices = new LightVertex[spriteVertices.Length];
			LightProjection.Prepare(camera, source.transform, spriteVertices, spriteUV,
				source.flipX, source.flipY, projectedVertices);
			if (num2 > 1)
			{
				for (int k = 0; k < spriteVertices.Length; k++)
				{
					LightVertex screen = projectedVertices[k];
					projectedRim[k] = new LightPoint(screen.X * viewWidth, screen.Y * viewHeight);
				}
				edgeFade.Project(projectedRim,smooth);
			}
			for (int l = 0; l < num2; l++)
			{
				Vector2 vector5 = centres[l] - vector;
				lightShift = vector5;
				for (int m = 0; m < spriteTriangles.Length; m += 3)
				{
					for (int n = 0; n < 3; n++)
					{
						int num3 = spriteTriangles[m + n];
						LightVertex vertex = projectedVertices[num3];
						clipA[n] = new LightVertex(vertex.X + vector5.x, vertex.Y + vector5.y,
						vertex.Z, vertex.U, vertex.V);
					}
					LightVertex[] a = clipA;
					LightVertex[] b = clipB;
					int num4 = 3;
					if (num4 > 0)
					{
						num4 = Clip(a, num4, b, Vector2.right, 1.01f);
						Swap(ref a, ref b);
					}
					if (num4 > 0)
					{
						num4 = Clip(a, num4, b, Vector2.left, 0.01f);
						Swap(ref a, ref b);
					}
					if (num4 > 0)
					{
						num4 = Clip(a, num4, b, Vector2.up, 1.01f);
						Swap(ref a, ref b);
					}
					if (num4 > 0)
					{
						num4 = Clip(a, num4, b, Vector2.down, 0.01f);
						Swap(ref a, ref b);
					}
					if (num4 >= 3)
					{
						for (int num5 = 1; num5 < num4 - 1; num5++)
						{
							BlendTriangle(camera, a[0], a[num5], a[num5 + 1], source.color, l, num2);
						}
					}
				}
			}
			mesh.Clear();
			mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
			mesh.SetVertices(vertices);
			mesh.SetUVs(0, uvs);
			mesh.SetColors(colors);
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateBounds();
			previousProjection = projectionMatrix;
			previousCamera = localToWorldMatrix;
			previousSource = localToWorldMatrix2;
			previousTint = source.color;
			previousPrimary = vector;
			oldCount = num2;
			lastFlipX = source.flipX;
			lastFlipY = source.flipY;
			lastReducedDetail = reducedDetail;
			lastSmooth = smooth;
			built = true;
			Array.Copy(centres, oldCentres, num2);
		}
		if (sourceMaterial != source.sharedMaterial)
		{
			CopyMaterial(source.sharedMaterial);
		}
		source.GetPropertyBlock(properties);
		properties.SetTexture("_MainTex", source.sprite.texture);
		properties.SetColor("_RendererColor", Color.white);
		properties.SetVector("_Flip", new Vector4(1f, 1f, 1f, 1f));
		renderer.SetPropertyBlock(properties);
		root.layer = source.gameObject.layer;
		renderer.sortingLayerID = source.sortingLayerID;
		renderer.sortingOrder = source.sortingOrder;
		hidden = source;
		wasEnabled = source.enabled;
		renderingCamera = camera;
		source.enabled = false;
		renderer.enabled = triangles.Count > 0;
		Status = "normalized_glows=" + num2 + " budget=1 shared_grid=" + (num2 > 1 ? blendGrid.Columns + "x" + blendGrid.Rows : "native") + " triangles=" + triangles.Count / 3;
		return true;
	}

	private void BlendTriangle(Camera camera, LightVertex a, LightVertex b, LightVertex c, Color tint, int player, int count)
	{
		float determinant = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
		if (Mathf.Abs(determinant) < 1e-12f)
		{
			return;
		}
		bool clockwise = determinant < 0f;
		Vector3 worldA = camera.ViewportToWorldPoint(new Vector3(a.X, a.Y, a.Z));
		Vector3 worldB = camera.ViewportToWorldPoint(new Vector3(b.X, b.Y, b.Z));
		Vector3 worldC = camera.ViewportToWorldPoint(new Vector3(c.X, c.Y, c.Z));
		int first = vertices.Count;
		if (count == 1)
		{
			// A single surviving player keeps the original sprite and full tint.
			vertices.Add(worldA);
			vertices.Add(worldB);
			vertices.Add(worldC);
			uvs.Add(new Vector2(a.U, a.V));
			uvs.Add(new Vector2(b.U, b.V));
			uvs.Add(new Vector2(c.U, c.V));
			colors.Add(tint);
			colors.Add(tint);
			colors.Add(tint);
			AddTriangle(first, first + 1, first + 2, clockwise);
			return;
		}

		blendVertices.Clear();
		blendGrid.Tessellate(a, b, c, player, blendVertices);
		for (int i = 0; i < blendVertices.Count; i++)
		{
			LightWeightedVertex sample = blendVertices[i];
			LightVertex v = sample.Vertex;
			float wb = ((v.X - a.X) * (c.Y - a.Y) - (v.Y - a.Y) * (c.X - a.X)) / determinant;
			float wc = ((b.X - a.X) * (v.Y - a.Y) - (b.Y - a.Y) * (v.X - a.X)) / determinant;
			// Reuse the native triangle's world plane instead of making thousands
			// of per-vertex Unity camera calls when eight players are moving.
			vertices.Add(worldA + (worldB - worldA) * wb + (worldC - worldA) * wc);
			uvs.Add(new Vector2(v.U, v.V));
			Color color = tint;
			// Keep RGB, atlas, shader and scene grading native. LinearLight uses
			// RGB as its blend colour; light ownership belongs in alpha instead.
			// A bounded taper also removes the sprite's hard polygonal outline.
			// It does not add light, fade the viewport border, or change native RGB.
			color.a *= (float)(sample.Weight * edgeFade.At(
				(v.X - lightShift.x) * viewWidth, (v.Y - lightShift.y) * viewHeight));
			colors.Add(color);
		}
		for (int i = 0; i < blendVertices.Count; i += 3)
		{
			AddTriangle(first + i, first + i + 1, first + i + 2, clockwise);
		}
	}

	private void AddTriangle(int a, int b, int c, bool clockwise)
	{
		triangles.Add(a);
		triangles.Add(clockwise ? b : c);
		triangles.Add(clockwise ? c : b);
	}

	private static void Swap(ref LightVertex[] a, ref LightVertex[] b)
	{
		LightVertex[] array = a;
		a = b;
		b = array;
	}

	private static int Clip(LightVertex[] input, int count, LightVertex[] output, Vector2 normal, float limit)
	{
		return LightGeometry.Clip(input, count, output, normal.x, normal.y, limit);
	}

	private void Prepare(SpriteRenderer source)
	{
		if (sprite != source.sprite)
		{
			sprite = source.sprite;
			spriteVertices = sprite.vertices;
			spriteUV = sprite.uv;
			spriteTriangles = sprite.triangles;
			projectedRim = new LightPoint[spriteVertices.Length];
			edgeFade.Topology(spriteTriangles);
			built = false;
		}
		if (!root)
		{
			root = new GameObject("Local8 Group Visibility");
			UnityEngine.Object.DontDestroyOnLoad(root);
			renderer = root.AddComponent<MeshRenderer>();
			renderer.enabled = false;
			renderer.shadowCastingMode = ShadowCastingMode.Off;
			renderer.receiveShadows = false;
			mesh = new Mesh
			{
				name = "Local8 blended light atlas",
				indexFormat = IndexFormat.UInt32
			};
			mesh.MarkDynamic();
			root.AddComponent<MeshFilter>().sharedMesh = mesh;
			CopyMaterial(source.sharedMaterial);
		}
	}

	private void CopyMaterial(Material source)
	{
		if ((bool)material)
		{
			UnityEngine.Object.Destroy(material);
		}
		material = new Material(source)
		{
			name = "Local8 Group Light"
		};
		sourceMaterial = source;
		renderer.sharedMaterial = material;
	}

	internal void Finish(Camera camera)
	{
		if (camera == renderingCamera)
		{
			Restore();
		}
	}

	internal void Restore()
	{
		if ((bool)hidden)
		{
			hidden.enabled = wasEnabled;
		}
		hidden = null;
		renderingCamera = null;
		if ((bool)renderer)
		{
			renderer.enabled = false;
		}
	}

	public void Dispose()
	{
		Restore();
		if ((bool)root)
		{
			UnityEngine.Object.Destroy(root);
		}
		if ((bool)mesh)
		{
			UnityEngine.Object.Destroy(mesh);
		}
		if ((bool)material)
		{
			UnityEngine.Object.Destroy(material);
		}
		root = null;
		mesh = null;
		renderer = null;
		material = null;
	}
}
