using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR;

public class Skeleton : MonoBehaviour
{
	// Start is called before the first frame update
	public List<int> parentIndices = new List<int>();
	public List<GameObject> bones = new List<GameObject>();
	public List<(Vector3 s, Quaternion r, Vector3 p)> referencePose = new List<(Vector3 s, Quaternion r, Vector3 p)>();
	public new SCA.TransformTrack[] animation = new SCA.TransformTrack[0];
	public int animationFrames = 0;
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		
	}

	public void ApplyReferencePose()
	{
		for(int i = 0; i < bones.Count; i++)
		{
			bones[i].transform.localScale = referencePose[i].s;
			bones[i].transform.localRotation = referencePose[i].r;
			bones[i].transform.localPosition = referencePose[i].p;
		}
	}

	public void ApplyAnimationPose(int frame, bool withDegree = true)
	{
		if (animation.Length == 0) return;
		for (int i = 0; i < bones.Count; i++)
		{
			//Vector3 FinScale = referencePose[i].s;
			Vector3 FinScale = Vector3.one;
			if (animation[i].SplineScale != null)
			{
				FinScale.x *= animation[i].SplineScale.GetValueX((float)frame, withDegree) ?? 1;
				FinScale.y *= animation[i].SplineScale.GetValueY((float)frame, withDegree) ?? 1;
				FinScale.z *= animation[i].SplineScale.GetValueZ((float)frame, withDegree) ?? 1;
			} else
			{
				if (animation[i].Mask.ScaleTypes.Contains(SCA.FlagOffset.StaticX))
					FinScale.x *= animation[i].StaticScale.x;
				if (animation[i].Mask.ScaleTypes.Contains(SCA.FlagOffset.StaticY))
					FinScale.y *= animation[i].StaticScale.y;
				if (animation[i].Mask.ScaleTypes.Contains(SCA.FlagOffset.StaticZ))
					FinScale.z *= animation[i].StaticScale.z;
			}

			Quaternion FinRot = Quaternion.identity;
			if (animation[i].SplineRotation != null)//track.HasSplineRotation)
			{
				FinRot = animation[i].SplineRotation.GetValue(frame, withDegree).ToQ();
			}
			else if (animation[i].HasStaticRotation)
			{
				// We actually need static rotation or Gael hands become unbent among others
				FinRot = animation[i].StaticRotation.ToQ();
			}
			//FinRot = new Quaternion(
			//	FinRot.x * referencePose[i].r.x,
			//	FinRot.y * referencePose[i].r.y,
			//	FinRot.z * referencePose[i].r.z,
			//	FinRot.w * referencePose[i].r.w
			//);

			//Vector3 FinPos = referencePose[i].p;
			Vector3 FinPos = Vector3.zero;
			if (animation[i].SplinePosition != null)
			{
				FinPos.x += animation[i].SplinePosition.GetValueX((float)frame, withDegree) ?? 0;
				FinPos.y += animation[i].SplinePosition.GetValueY((float)frame, withDegree) ?? 0;
				FinPos.z += animation[i].SplinePosition.GetValueZ((float)frame, withDegree) ?? 0;
			}
			else
			{
				if (animation[i].Mask.PositionTypes.Contains(SCA.FlagOffset.StaticX))
					FinPos.x += animation[i].StaticPosition.x;
				if (animation[i].Mask.PositionTypes.Contains(SCA.FlagOffset.StaticY))
					FinPos.y += animation[i].StaticPosition.y;
				if (animation[i].Mask.PositionTypes.Contains(SCA.FlagOffset.StaticZ))
					FinPos.z += animation[i].StaticPosition.z;
			}


			//if (i == 0)
			//{
			//	FinPos = new Vector3(-FinPos.z, FinPos.y, FinPos.x);
			//}
			//if (i == 7)
			//{
			//	var e = FinRot.eulerAngles;
			//	FinRot = Quaternion.Euler(e.x, e.y - 90, e.z);
			//}
			//if (i == 8)
			//{
			//	var e = FinRot.eulerAngles;
			//	FinRot = Quaternion.Euler(e.x, e.y + 90, e.z);
			//}

			bones[i].transform.localScale = FinScale;
			bones[i].transform.localRotation = FinRot;
			bones[i].transform.localPosition = FinPos;
		}
	}
}

public static class MatrixExtensions
{
	public static Quaternion ExtractRotation(this Matrix4x4 m)
	{
		Quaternion q = new Quaternion();
		q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
		q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
		q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
		q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
		q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
		q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
		q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
		return q;
	}

	public static Vector3 ExtractPosition(this Matrix4x4 matrix)
	{
		Vector3 position;
		position.x = matrix.m03;
		position.y = matrix.m13;
		position.z = matrix.m23;
		return position;
	}

	public static Vector3 ExtractScale(this Matrix4x4 matrix)
	{
		Vector3 scale;
		scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
		scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
		scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
		return scale;
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(Skeleton))]
public class SkeletonEditor : Editor
{
	float anim_value;
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		Skeleton myScript = (Skeleton)target;
		if (GUILayout.Button("Apply Reference Pose"))
		{
			myScript.ApplyReferencePose();
		}
		GUILayout.Label("Animation slider");
		float new_val = GUILayout.HorizontalSlider(anim_value, 0, myScript.animationFrames, null);
		if (new_val != anim_value)
		{
			anim_value = new_val;
			myScript.ApplyAnimationPose((int)anim_value);
		}
		GUILayout.Space(20);
		GUILayout.Label(myScript.bones[0].transform.localRotation.eulerAngles.ToString());
		GUILayout.Label(myScript.bones[7].transform.localRotation.eulerAngles.ToString());
		GUILayout.Label(myScript.bones[8].transform.localRotation.eulerAngles.ToString());
		GUILayout.Label(myScript.bones[10].transform.localPosition.ToString());
	}
}
#endif