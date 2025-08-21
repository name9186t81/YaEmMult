using System.Runtime.CompilerServices;

using UnityEngine;

namespace YaEm
{
	public static class MathUtils
	{
		public enum LerpType
		{
			Linear,
			Quad,
			QuadInverse,
			QuadSmooth
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Cross(this Vector2 a, in Vector2 b)
		{
			return a.x * b.y - b.x * a.y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Polar2Vector(float angle, float radius)
		{
			return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float AngleFromVector(this Vector2 vector)
		{
			return Mathf.Atan2(vector.y, vector.x);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Area(this AnimationCurve curve, float end = 1f, bool ignoreNegative = false)
		{
			if (curve == null) return 0f;

			const float step = 0.001f;

			float preValue = curve.Evaluate(step);
			float area = 0f;
			for (float st = step; st < end; st += step)
			{
				float current = curve.Evaluate(st);

				if (ignoreNegative && current < 0) continue;

				area += (current + preValue) * step * 0.5f;
				preValue = current;
			}

			//area += (curve.Evaluate(end) + preValue) * step * 0.5f;
			return area;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 RandomDirection()
		{
			float angle = Random.value * Mathf.PI * 2;
			return Polar2Vector(angle, 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LerpRad(float a, float b, float t)
		{
			float num = Mathf.Repeat(b - a, Mathf.PI * 2);
			if (num > Mathf.PI)
				num -= Mathf.PI * 2;
			return a + num * Mathf.Clamp01(t);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool DistanceLess(this Vector2 v1, in Vector2 v2, float distance)
		{
			return (v1 - v2).sqrMagnitude < distance * distance;
		}

		/// <summary>
		/// Angle in radians.
		/// </summary>
		/// <param name="vector"></param>
		/// <param name="angle"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 Rotate(this in Vector2 vector, float angle)
		{
			float sin = Mathf.Sin(angle);
			float cos = Mathf.Cos(angle);

			return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float ArcLength(float start, float end, float radius)
		{
			start = NormalizeAngle(start);
			end = NormalizeAngle(end);

			float abs = Mathf.Abs(start - end);
			return Mathf.Min(Mathf.PI * 2 - abs, abs) * radius;
		}

		/// <summary>
		/// Both gradients must have the same amount of keys.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <param name="t"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Gradient Lerp(this Gradient a, Gradient b, float t)
		{
			if (a.colorKeys.Length != b.colorKeys.Length) throw new System.ArgumentException();

			Gradient result = new Gradient();
			GradientColorKey[] keys = new GradientColorKey[a.colorKeys.Length];
			for(int i = 0; i < keys.Length; ++i)
			{
				keys[i] = new GradientColorKey(Color.Lerp(a.colorKeys[i].color, b.colorKeys[i].color, t), Mathf.Lerp(a.colorKeys[i].time, b.colorKeys[i].time, t));
			}
			result.colorKeys = keys;
			result.alphaKeys = a.alphaKeys;
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float NormalizeAngle(float angle)
		{
			float normal = angle % (Mathf.PI * 2);
			while(angle < 0)
			{
				normal += Mathf.PI * 2;
			}

			return normal;
		}

		public static float UniversalLerp(float start, float end, float t, LerpType type, bool clamp = true)
		{
			switch (type)
			{
				case LerpType.Linear:
				{
					if (clamp)
					{
						return Mathf.Lerp(start, end, t);
					}
					else
					{
						return Mathf.LerpUnclamped(start, end, t);
					}
				}
				case LerpType.Quad:
				{
					if (clamp)
					{
						return LerpQuad(start, end, t);
					}
					else
					{
						return LerpQuadUnclamped(start, end, t);
					}
				}
				case LerpType.QuadInverse:
				{
					if (clamp)
					{
						return LerpQuad(start, end, t);
					}
					else
					{
						return LerpInvQuadUnclamped(start, end, t);
					}
				}
				case LerpType.QuadSmooth:
				{
					if (clamp)
					{
						return LerpSmoothQuad(start, end, t);
					}
					else
					{
						return LerpSmoothQuadUnclamped(start, end, t);
					}
				}
			}
			throw new System.NotImplementedException();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LerpQuad(float start, float end, float t)
		{
			t = Mathf.Clamp01(t);
			return Mathf.Lerp(start, end, t * t);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LerpInvQuad(float start, float end, float t)
		{
			t = Mathf.Clamp01(t);
			float factor = 1 - t;
			return Mathf.Lerp(start, end, 1 - factor * factor);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LerpSmoothQuad(float start, float end, float t)
		{
			t = Mathf.Clamp01(t);
			float factor = 1 - t;
			return t > 0.5f ? Mathf.Lerp(start, end, 1 - 2 * factor * factor) : Mathf.Lerp(start, end, 2 * t * t);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LerpQuadUnclamped(float start, float end, float t)
		{
			return Mathf.Lerp(start, end, t * t);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LerpInvQuadUnclamped(float start, float end, float t)
		{
			float factor = 1 - t;
			return Mathf.Lerp(start, end, 1 - factor * factor);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float LerpSmoothQuadUnclamped(float start, float end, float t)
		{
			float factor = 1 - t;
			return t > 0.5f ? Mathf.Lerp(start, end, 1 - 2 * factor * factor) : Mathf.Lerp(start, end, 2 * t * t);
		}
	}
}
