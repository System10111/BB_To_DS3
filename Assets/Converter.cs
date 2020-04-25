using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using UnityEngine.XR;


public struct DQuaternion
{
	public double x;
	public double y;
	public double z;
	public double w;

	public DQuaternion(double x, double y, double z, double w)
	{
		this.x = x;
		this.y = y;
		this.z = z;
		this.w = w;
	}

	public Quaternion ToQ()
	{
		return new Quaternion((float)x, (float)y, (float)z, (float)w);
	}

	public static DQuaternion FromQ(Quaternion q)
	{
		return new DQuaternion(q.x, q.y, q.z, q.w);
	}

	public static DQuaternion operator *(DQuaternion q1, DQuaternion q2)
	{
		return new DQuaternion(
			q1.x * q2.w + q1.y * q2.z - q1.z * q2.y + q1.w * q2.x,
			-q1.x * q2.z + q1.y * q2.w + q1.z * q2.x + q1.w * q2.y,
			q1.x * q2.y - q1.y * q2.x + q1.z * q2.w + q1.w * q2.z,
			-q1.x * q2.x - q1.y * q2.y - q1.z * q2.z + q1.w * q2.w
		);
	}

	public DQuaternion Inverse()
	{
		return FromQ(Quaternion.Inverse(ToQ()));
	}

	public static DQuaternion identity = FromQ(Quaternion.identity);

	internal static DQuaternion Euler(Vector3 v)
	{
		double yaw = v.y * Mathf.Deg2Rad;
		double pitch = v.x * Mathf.Deg2Rad;
		double roll = v.z * Mathf.Deg2Rad;

		double rollOver2 = roll * 0.5f;
		double sinRollOver2 = (double)Math.Sin((double)rollOver2);
		double cosRollOver2 = (double)Math.Cos((double)rollOver2);
		double pitchOver2 = pitch * 0.5f;
		double sinPitchOver2 = (double)Math.Sin((double)pitchOver2);
		double cosPitchOver2 = (double)Math.Cos((double)pitchOver2);
		double yawOver2 = yaw * 0.5f;
		double sinYawOver2 = (double)Math.Sin((double)yawOver2);
		double cosYawOver2 = (double)Math.Cos((double)yawOver2);
		DQuaternion result;
		result.w = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2;
		result.x = cosYawOver2 * sinPitchOver2 * cosRollOver2 + sinYawOver2 * cosPitchOver2 * sinRollOver2;
		result.y = sinYawOver2 * cosPitchOver2 * cosRollOver2 - cosYawOver2 * sinPitchOver2 * sinRollOver2;
		result.z = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2;

		return result;
	}
}

public class Converter : MonoBehaviour
{
	public int[] bone_relations = {
	/*000*/0,
	/*001*/1,
	/*002*/2,
	/*003*/3,
	/*004*/4,
	/*005*/5,
	/*006*/6,
	/*007*/7,
	/*008*/8,
	/*009*/38,
	/*010*/10,
	/*011*/11,
	/*012*/17,
	/*013*/16,
	/*014*/17,
	/*015*/12,
	/*016*/14,
	/*017*/13,
	/*018*/18,
	/*019*/20,
	/*020*/19,
	/*021*/19,
	/*022*/21,
	/*023*/22,
	/*024*/40,
	/*025*/24,
	/*026*/25,
	/*027*/31,
	/*028*/30,
	/*029*/31,
	/*030*/26,
	/*031*/28,
	/*032*/27,
	/*033*/32,
	/*034*/34,
	/*035*/33,
	/*036*/33,
	/*037*/35,
	/*038*/36,
	/*039*/42,
	/*040*/43,
	/*041*/44,
	/*042*/46,
	/*043*/48,
	/*044*/47,
	/*045*/47,
	/*046*/47,
	/*047*/77,
	/*048*/50,
	/*049*/76,
	/*050*/51,
	/*051*/52,
	/*052*/52,
	/*053*/71,
	/*054*/72,
	/*055*/53,
	/*056*/54,
	/*057*/55,
	/*058*/56,
	/*059*/57,
	/*060*/58,
	/*061*/59,
	/*062*/60,
	/*063*/61,
	/*064*/62,
	/*065*/63,
	/*066*/64,
	/*067*/65,
	/*068*/66,
	/*069*/67,
	/*070*/68,
	/*071*/69,
	/*072*/69,
	/*073*/73,
	/*074*/74,
	/*075*/50,
	/*076*/106,
	/*077*/77,
	/*078*/78,
	/*079*/79,
	/*080*/105,
	/*081*/80,
	/*082*/81,
	/*083*/81,
	/*084*/100,
	/*085*/101,
	/*086*/82,
	/*087*/83,
	/*088*/84,
	/*089*/85,
	/*090*/86,
	/*091*/87,
	/*092*/88,
	/*093*/89,
	/*094*/90,
	/*095*/91,
	/*096*/92,
	/*097*/93,
	/*098*/94,
	/*099*/95,
	/*100*/96,
	/*101*/97,
	/*102*/98,
	/*103*/98,
	/*104*/102,
	/*105*/103,
	/*106*/79,
	/*107*/107,
	/*108*/48,
	/*109*/48,
	};
	public string outputPath = "";
	// Start is called before the first frame update
	public TextAsset bb_skel;
	public float scale_factor = 0.98f;
	public TextAsset ds3_skel;
	public GameObject bone_pf;
	public TextAsset bb_anim;
	public TextAsset ds3_anim;
	public Skeleton bb_skeleton;
	public Skeleton ds3_skeleton;
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		
	}

	public Skeleton SpawnSkeleton(TextAsset xml_file, Color col)
	{
		GameObject skel = new GameObject("Unnamed_Skeleton");
		var skelcomp = skel.AddComponent<Skeleton>();
		XElement xml = XElement.Parse(xml_file.text);
		XElement data = (from item in xml.Elements() where item.Attribute("name").Value == "__data__" select item).First();
		XElement xSkel = (from item in data.Elements() where item.Attribute("class").Value == "hkaSkeleton" select item).First();

		//get the parent indices
		string[] inds = (from item in xSkel.Elements() where item.Attribute("name").Value == "parentIndices" select item).First().Value.Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries);
		skelcomp.parentIndices = (from ind in inds select int.Parse(ind)).ToList();

		//create the bones and set their parents
		{
			int index = 0;
			foreach (var xb in
				(from item in xSkel.Elements() where item.Attribute("name").Value == "bones" select item).First().Elements()
			)
			{
				GameObject b = Instantiate(bone_pf);
				b.GetComponent<MeshRenderer>().material.color = col;
				skelcomp.bones.Add(b);

				b.name = (from item in xb.Elements() where item.Attribute("name").Value == "name" select item).First().Value + "(" + index + ")";

				int parIndex = skelcomp.parentIndices[index];
				if (parIndex == -1)
				{
					b.transform.SetParent(skel.transform);
				}
				else
				{
					b.transform.SetParent(skelcomp.bones[parIndex].transform);
				}

				index++;
			}
		}

		//create the reference pose matrices
		{
			int inRow = 0;
			string poss = (from item in xSkel.Elements() where item.Attribute("name").Value == "referencePose" select item).First().Value;
			var matches = Regex.Matches(poss, @"\(([^)]*)\)");
			(Vector3 s, Quaternion r, Vector3 p) curPose = (Vector3.one, Quaternion.identity, Vector3.zero);
			foreach (var m in matches)
			{
				float[] vals = (from s in m.ToString().Split('(', ')')[1].Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries) select float.Parse(s)).ToArray();
				switch (inRow)
				{
					case 0:
						{
							curPose.p = new Vector3(vals[0], vals[1], vals[2]);
						}
						break;
					case 1:
						{
							curPose.r = new Quaternion(vals[0], vals[1], vals[2], vals[3]);
						}
						break;
					case 2:
						{
							curPose.s = new Vector3(vals[0], vals[1], vals[2]);
							skelcomp.referencePose.Add(curPose);
						}
						break;
				}
				inRow = (inRow + 1) % 3;
			}
		}

		skelcomp.ApplyReferencePose();

		return skelcomp;
	}

	public void SpawnBBSkeleton()
	{
		bb_skeleton = SpawnSkeleton(bb_skel, Color.blue);
		bb_skeleton.gameObject.name = "BB_Skeleton";
		bb_skeleton.transform.localScale = new Vector3(scale_factor,scale_factor,scale_factor);
	}
	public void SpawnDS3Skeleton()
	{
		ds3_skeleton = SpawnSkeleton(ds3_skel, Color.red);
		ds3_skeleton.gameObject.name = "DS3_Skeleton";
	}

	public void SetBBAnimation(string xmlFile, List<(float x, float y, float z, float w)> referenceFrame = null)
	{
		XElement xml;
		if(xmlFile == "")
		{
			xml = XElement.Parse(bb_anim.text);
		} else
		{
			xml = XElement.Parse(xmlFile);
		}
		XElement data = (from item in xml.Elements() where item.Attribute("name").Value == "__data__" select item).First();
		XElement xSCA = (from item in data.Elements() where item.Attribute("class").Value == "hkaSplineCompressedAnimation" select item).First();


		int numBlocks = int.Parse((from item in xSCA.Elements() where item.Attribute("name").Value == "numBlocks" select item).First().Value);
		int numTransTracks = int.Parse((from item in xSCA.Elements() where item.Attribute("name").Value == "numberOfTransformTracks" select item).First().Value);
		int frames = int.Parse((from item in xSCA.Elements() where item.Attribute("name").Value == "numFrames" select item).First().Value);

		if(referenceFrame != null)
		{
			float addx = 0;
			float addy = 0;
			float addz = 0;
			float addw = 0;
			if(referenceFrame.Count > 0)
			{
				addx = referenceFrame.Last().x;
				addy = referenceFrame.Last().y;
				addz = referenceFrame.Last().z;
				addw = referenceFrame.Last().w;
			}
			var els = (from item in data.Elements() where item.Attribute("class").Value == "hkaDefaultAnimatedReferenceFrame" select item);
			if(els.Count() == 0)
			{
				for(int i = 0; i < frames; i++)
				{
					referenceFrame.Add((0.0f + addx, 0.0f + addy, 0.0f + addz, 0.0f + addw));
				}
			} else
			{
				XElement xDARF = els.First();
				string[] refFrame = (from item in xDARF.Elements() where item.Attribute("name").Value == "referenceFrameSamples" select item).First().Value.Split('(');
				refFrame = (from s in refFrame where s.Contains(')') select s.Split(')')[0]).ToArray();
				foreach (var s in refFrame)
				{
					float[] vals = (from f in s.Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries) select float.Parse(f)).ToArray();
					referenceFrame.Add((vals[0] + addx, vals[1] + addy, vals[2] + addz, vals[3] + addw));
				}
			}
		}

		byte[] dataVals = 
			(from v in 
				(from item in xSCA.Elements() where item.Attribute("name").Value == "data" select item)
			.First().Value.Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries) select byte.Parse(v)).ToArray();

		bb_skeleton.animation_blocks = SCA.ReadSplineCompressedAnimByteBlock(false, dataVals, numTransTracks, numBlocks);
		bb_skeleton.animationFrames = frames;

		bb_skeleton.ApplyAnimationPose(0);
	}

	public void FitDS3Skeleton()
	{
		ds3_skeleton.ApplyReferencePose();
		ds3_skeleton.transform.position = bb_skeleton.transform.position;
		ds3_skeleton.animationFrames = bb_skeleton.animationFrames;
		//ds3_skeleton.animation_blocks = new SCA.TransformTrack[ds3_skeleton.bones.Count];
		List<int> feetTargets = new List<int>() { 1, 2, 3, 4, 5, 6 };
		ds3_skeleton.animation_blocks = new List<SCA.TransformTrack[]>(bb_skeleton.animation_blocks.Count);
		for (int j = 0; j < bb_skeleton.animation_blocks.Count; j++)
		{
			ds3_skeleton.animation_blocks.Add(new SCA.TransformTrack[ds3_skeleton.bones.Count]);
			//List<int> feetTargets = new List<int>() { };

			for (int i = 0; i < ds3_skeleton.bones.Count; i++)
			{
				//we want to handle feet targets ourselves
				if (feetTargets.Contains(i)) continue;
				//copy most of bloodborne's bones depending on the relation
				ds3_skeleton.animation_blocks[j][i] = bb_skeleton.animation_blocks[j][bone_relations[i]].Clone();

				//bloodborne character is bigger
				//but trying to fix it messes up the animation
			}
		}

		for (int j = 0; j < ds3_skeleton.animation_blocks.Count; j++)
		foreach (var it in feetTargets)
		{
			ds3_skeleton.animation_blocks[j][it] = new SCA.TransformTrack();
			SCA.TransformTrack tr = ds3_skeleton.animation_blocks[j][it];
			tr.Mask = new SCA.TransformMask();

			//top targets get moved
			if (it == 1 || it == 4)
			{
				var bbt = bb_skeleton.animation_blocks[j][bone_relations[it]];
				tr.HasSplinePosition = bbt.HasSplinePosition;
				tr.Mask.PositionTypes = bbt.Mask.PositionTypes;
				tr.Mask.PositionQuantizationType = bbt.Mask.PositionQuantizationType;
				tr.StaticPosition = bbt.StaticPosition;
				tr.SplinePosition = bbt.SplinePosition;

				tr.HasSplineRotation = false;
				tr.HasStaticRotation = true;
				tr.HasSplineScale = false;
				tr.Mask.RotationTypes.Clear();
				tr.Mask.RotationTypes.Add(SCA.FlagOffset.StaticX);
				tr.Mask.RotationTypes.Add(SCA.FlagOffset.StaticY);
				tr.Mask.RotationTypes.Add(SCA.FlagOffset.StaticZ);
				tr.Mask.RotationTypes.Add(SCA.FlagOffset.StaticW);
				tr.StaticRotation = DQuaternion.FromQ(ds3_skeleton.bones[it].transform.localRotation); //rotation from reference pose
				tr.Mask.ScaleTypes.Clear();
				tr.Mask.ScaleTypes.Add(SCA.FlagOffset.StaticX);
				tr.Mask.ScaleTypes.Add(SCA.FlagOffset.StaticY);
				tr.Mask.ScaleTypes.Add(SCA.FlagOffset.StaticZ);
				tr.StaticScale = ds3_skeleton.bones[it].transform.localScale; //scale from reference pose
			}
			//bottom targets get bb's rotation
			else if (it == 3 || it == 6)
			{
				var bbt = bb_skeleton.animation_blocks[j][bone_relations[it]];
				tr.HasSplineRotation = bbt.HasSplineRotation;
				tr.HasStaticRotation = bbt.HasStaticRotation;
				tr.Mask.RotationTypes = bbt.Mask.RotationTypes;
				tr.Mask.RotationQuantizationType = bbt.Mask.RotationQuantizationType;
				tr.StaticRotation = bbt.StaticRotation;
				tr.SplineRotation = bbt.SplineRotation;
				DQuaternion FeetRot = DQuaternion.Euler(new Vector3(0, 10, 0));
				if(tr.SplineRotation != null)
				{
					tr.SplineRotation.Channel.Values = (from q in tr.SplineRotation.Channel.Values select q * FeetRot).ToList();
				} else
				{
					tr.StaticRotation *= FeetRot;
				}

				tr.HasSplinePosition = false;
				tr.HasSplineScale = false;
				tr.Mask.PositionTypes.Clear();
				tr.Mask.PositionTypes.Add(SCA.FlagOffset.StaticX);
				tr.Mask.PositionTypes.Add(SCA.FlagOffset.StaticY);
				tr.Mask.PositionTypes.Add(SCA.FlagOffset.StaticZ);
				tr.StaticPosition = ds3_skeleton.bones[it].transform.localPosition; //position from reference pose
				tr.Mask.ScaleTypes.Clear();
				tr.Mask.ScaleTypes.Add(SCA.FlagOffset.StaticX);
				tr.Mask.ScaleTypes.Add(SCA.FlagOffset.StaticY);
				tr.Mask.ScaleTypes.Add(SCA.FlagOffset.StaticZ);
				tr.StaticScale = ds3_skeleton.bones[it].transform.localScale; //scale from reference pose
			}
			//other targets are dumb and just become reference pose
			else
			{
				tr.HasSplineRotation = false;
				tr.HasStaticRotation = true;
				tr.Mask.RotationTypes.Clear();
				tr.Mask.RotationTypes.Add(SCA.FlagOffset.StaticX);
				tr.Mask.RotationTypes.Add(SCA.FlagOffset.StaticY);
				tr.Mask.RotationTypes.Add(SCA.FlagOffset.StaticZ);
				tr.Mask.RotationTypes.Add(SCA.FlagOffset.StaticW);
				tr.StaticRotation = DQuaternion.FromQ(ds3_skeleton.bones[it].transform.localRotation); //rotation from reference pose
				tr.HasSplinePosition = false;
				tr.HasSplineScale = false;
				tr.Mask.PositionTypes.Clear();
				tr.Mask.PositionTypes.Add(SCA.FlagOffset.StaticX);
				tr.Mask.PositionTypes.Add(SCA.FlagOffset.StaticY);
				tr.Mask.PositionTypes.Add(SCA.FlagOffset.StaticZ);
				tr.StaticPosition = ds3_skeleton.bones[it].transform.localPosition + new Vector3(0.05f,0,0); //position from reference pose - 0.1 to account for differences
				tr.Mask.ScaleTypes.Clear();
				tr.Mask.ScaleTypes.Add(SCA.FlagOffset.StaticX);
				tr.Mask.ScaleTypes.Add(SCA.FlagOffset.StaticY);
				tr.Mask.ScaleTypes.Add(SCA.FlagOffset.StaticZ);
				tr.StaticScale = ds3_skeleton.bones[it].transform.localScale; //scale from reference pose
			}
		}

		//DS3BoneRotSameAsReference(ds3_skeleton, 20); // Skirt is messed up
		//DS3BoneRotSameAsReference(ds3_skeleton, 35); // 
		//ds3_skeleton.animation[20].StaticRotation = DQuaternion.Euler(new Vector3(0,0,180)); 
		////for some reason this is a bit bugged and needs to be set to 180 to be 0...
		//ds3_skeleton.animation[35].StaticRotation = DQuaternion.Euler(new Vector3(0,0,180));
		//DS3BonePosSameAsReference(ds3_skeleton, 20); // Skirt is messed up
		DS3BonePosSameAsReference(ds3_skeleton, 35); // 
		DS3BoneFlipZRot(71); //L weapons are backwards
		DS3BoneFlipZRot(72); //L weapons are backwards
		DS3BoneFlipZRot(102); //R weapons are backwards
		DS3BoneFlipZRot(103); //R weapons are backwards
		float weaponZRot = 17.0f;
		DS3BoneRotQ(71, DQuaternion.Euler(new Vector3(3, 0, weaponZRot)));
		DS3BoneRotQ(72, DQuaternion.Euler(new Vector3(3, 0, weaponZRot)));
		DS3BoneRotQ(102, DQuaternion.Euler(new Vector3(3, 0, weaponZRot)));
		DS3BoneRotQ(103, DQuaternion.Euler(new Vector3(3, 0, weaponZRot)));

		
		DS3BonePosSameAsReference(ds3_skeleton, 54); // There is a weird bone that on the L forearm which is a child of a different object
		DS3BoneRotSameAsReference(ds3_skeleton, 54);
		DS3BonePosSameAsReference(ds3_skeleton, 85); // There is a weird bone that on the R forearm which is a child of a different object
		DS3BoneRotSameAsReference(ds3_skeleton, 85);
		DS3BoneRotSameAsReference(ds3_skeleton, 76); // the L and R Po are messed up
		DS3BoneRotSameAsReference(ds3_skeleton, 107);
		
		for (int j = 0; j < ds3_skeleton.animation_blocks.Count; j++)
		{
			
			//try to move master down
			SCA.TransformTrack master = ds3_skeleton.animation_blocks[j][0];
			SCA.TransformTrack rootpos = ds3_skeleton.animation_blocks[j][7];

			DS3ChangeBone(0, (SCA.TransformTrack tr) =>
			{
				rootpos.HasSplinePosition = master.HasSplinePosition;
				rootpos.Mask.PositionQuantizationType = master.Mask.PositionQuantizationType;
				rootpos.Mask.PositionTypes = master.Mask.PositionTypes;
				rootpos.SplinePosition = master.SplinePosition;
				rootpos.StaticPosition = master.StaticPosition;
				master.HasSplinePosition = false;
				master.StaticPosition = new Vector3(0, 0, 0);
				master.SplinePosition = null;
				master.Mask.PositionTypes = new List<SCA.FlagOffset>() { SCA.FlagOffset.StaticX, SCA.FlagOffset.StaticY, SCA.FlagOffset.StaticZ };
			}, null, j);

			ds3_skeleton.ApplyReferencePose();

			//the pectorals are messed up
			foreach (var i in new List<int>() { 75, 106 })
			{
				var tr = new SCA.TransformTrack();
				SCA.TransformTrack otr = ds3_skeleton.animation_blocks[j][i];
				tr.HasSplinePosition = otr.HasSplinePosition;
				tr.Mask = new SCA.TransformMask();
				tr.Mask.PositionTypes = otr.Mask.PositionTypes;
				tr.Mask.PositionQuantizationType = otr.Mask.PositionQuantizationType;
				tr.StaticPosition = otr.StaticPosition;
				tr.SplinePosition = otr.SplinePosition;
				tr.HasSplineRotation = otr.HasSplineRotation;
				tr.HasStaticRotation = otr.HasStaticRotation;
				tr.Mask.RotationTypes = otr.Mask.RotationTypes;
				tr.Mask.RotationQuantizationType = otr.Mask.RotationQuantizationType;
				tr.StaticRotation = otr.StaticRotation;
				tr.SplineRotation = otr.SplineRotation;
				tr.HasSplineScale = otr.HasSplineScale;
				tr.Mask.ScaleTypes = otr.Mask.ScaleTypes;
				tr.Mask.ScaleQuantizationType = otr.Mask.ScaleQuantizationType;
				tr.StaticScale = otr.StaticScale;
				tr.SplineScale = otr.SplineScale;

				ds3_skeleton.animation_blocks[j][i] = tr;
				if (tr.SplinePosition != null)
				{
					tr.SplinePosition = new SCA.SplineTrackVector3(
						(from x in tr.SplinePosition.ChannelX.Values select x - 0.02f).ToList(),
						tr.SplinePosition.ChannelY.Values,
						tr.SplinePosition.ChannelZ.Values,
						tr.SplinePosition.Knots,
						tr.SplinePosition.Degree
					);
				}
				else
				{
					tr.StaticPosition.x -= 0.02f;
				}
			}
			
			// R_Pectoral is rotated
			DQuaternion R_PecRot = DQuaternion.Euler(new Vector3(180, 0, 0));
			{
				SCA.TransformTrack tr = ds3_skeleton.animation_blocks[j][106];
				if (tr.SplineRotation != null)
				{
					tr.SplineRotation = new SCA.SplineTrackQuaternion(
						(from q in tr.SplineRotation.Channel.Values select q * R_PecRot).ToList(),
						tr.SplineRotation.Knots,
						tr.SplineRotation.Degree
					);
				}
				else
				{
					tr.StaticRotation = tr.StaticRotation * R_PecRot;
				}
			}
			//the spine armor is messed up
			foreach (var i in new List<int>() { 108, 109 })
			{
				var tr = new SCA.TransformTrack();
				SCA.TransformTrack otr = ds3_skeleton.animation_blocks[j][i];
				tr.HasSplinePosition = otr.HasSplinePosition;
				tr.Mask = new SCA.TransformMask();
				tr.Mask.PositionTypes = otr.Mask.PositionTypes;
				tr.Mask.PositionQuantizationType = otr.Mask.PositionQuantizationType;
				tr.StaticPosition = otr.StaticPosition;
				tr.SplinePosition = otr.SplinePosition;
				tr.HasSplineRotation = otr.HasSplineRotation;
				tr.HasStaticRotation = otr.HasStaticRotation;
				tr.Mask.RotationTypes = otr.Mask.RotationTypes;
				tr.Mask.RotationQuantizationType = otr.Mask.RotationQuantizationType;
				tr.StaticRotation = otr.StaticRotation;
				tr.SplineRotation = otr.SplineRotation;
				tr.HasSplineScale = otr.HasSplineScale;
				tr.Mask.ScaleTypes = otr.Mask.ScaleTypes;
				tr.Mask.ScaleQuantizationType = otr.Mask.ScaleQuantizationType;
				tr.StaticScale = otr.StaticScale;
				tr.SplineScale = otr.SplineScale;

				if (tr.SplinePosition != null && tr.SplinePosition.ChannelX?.Values != null)
				{
					tr.HasSplinePosition = true;
					tr.Mask.PositionTypes = new List<SCA.FlagOffset>(tr.Mask.PositionTypes);
					tr.Mask.PositionTypes.Add(SCA.FlagOffset.SplineX);
					var xs = (from x in tr.SplinePosition.ChannelX.Values select x + 0.13f).ToList();
					var ys = tr.SplinePosition.ChannelY?.Values;
					var zs = tr.SplinePosition.ChannelZ?.Values;
					tr.SplinePosition = new SCA.SplineTrackVector3(
						xs,
						ys,
						zs,
						tr.SplinePosition.Knots,
						tr.SplinePosition.Degree
					);
					tr.SplinePosition.BoundsXMax = xs.Max();
					tr.SplinePosition.BoundsXMin = xs.Min();
				}
				else
				{
					tr.StaticPosition.x += 0.13f;
				}
				ds3_skeleton.animation_blocks[j][i] = tr;
			}

			//feet are stubby
			foreach (var i in new List<int>() { 16, 31 })
			{
				SCA.TransformTrack tr = ds3_skeleton.animation_blocks[j][i];

				if (tr.SplinePosition != null)
				{
					tr.SplinePosition.ChannelX.Values = (from x in tr.SplinePosition.ChannelX.Values select x + 0.02f).ToList();
					tr.SplinePosition.ChannelZ.Values = (from z in tr.SplinePosition.ChannelZ.Values select z - 0.04f).ToList();
					tr.SplinePosition.BoundsXMax = tr.SplinePosition.ChannelX.Values.Max();
					tr.SplinePosition.BoundsXMin = tr.SplinePosition.ChannelX.Values.Min();
					tr.SplinePosition.BoundsZMax = tr.SplinePosition.ChannelZ.Values.Max();
					tr.SplinePosition.BoundsZMin = tr.SplinePosition.ChannelZ.Values.Min();
				}
				else
				{
					tr.StaticPosition.z -= 0.04f;
					tr.StaticPosition.x += 0.02f;
				}
				DQuaternion rotQuat = DQuaternion.Euler(new Vector3(0, -8, 0));
				if (tr.SplineRotation != null)
				{
					tr.SplineRotation = new SCA.SplineTrackQuaternion(
						(from q in tr.SplineRotation.Channel.Values select q * rotQuat).ToList(),
						tr.SplineRotation.Knots,
						tr.SplineRotation.Degree
					);
				}
				else
				{
					tr.StaticRotation = tr.StaticRotation * rotQuat;
				}
			}

			//the head is a bit forward
			float head_degree = 8.3f;
			foreach (var i in new List<int>() { 77, 78 })
			{
				SCA.TransformTrack tr = ds3_skeleton.animation_blocks[j][i];
				if (tr.SplineRotation != null)
				{
					tr.SplineRotation = new SCA.SplineTrackQuaternion(
						(from q in tr.SplineRotation.Channel.Values select q * DQuaternion.Euler(new Vector3(0, head_degree, 0))).ToList(),
						tr.SplineRotation.Knots,
						tr.SplineRotation.Degree
					);
				}
				else
				{
					tr.StaticRotation = tr.StaticRotation * DQuaternion.Euler(new Vector3(0, head_degree, 0));
				}
				head_degree *= -1;
			}

			var neck_t = ds3_skeleton.animation_blocks[j][77];
			if (neck_t.SplinePosition != null)
			{
				neck_t.SplinePosition.ChannelZ.Values = (from z in neck_t.SplinePosition.ChannelZ.Values select z - 0.03f).ToList();
				neck_t.SplinePosition.BoundsZMax = neck_t.SplinePosition.ChannelZ.Values.Max();
				neck_t.SplinePosition.BoundsZMin = neck_t.SplinePosition.ChannelZ.Values.Min();
			}
			else
			{
				neck_t.StaticPosition.z = neck_t.StaticPosition.z - 0.03f;
			}

			//collar can be same as neck
			ds3_skeleton.animation_blocks[j][47] = ds3_skeleton.animation_blocks[j][77].Clone();
		}
		//Specific Rotations
		DS3BoneRotQ(7, new DQuaternion(0, -1, 0, 1)); // rotate rootpos
		DS3BoneRotQ(8, DQuaternion.Euler(new Vector3(0,0,180)));// rotate pelvis
		DS3BoneRotQ(39, DQuaternion.Euler(new Vector3(-90, -90, 0))); // rotate rootRotY
		DS3BoneRotQ(40, DQuaternion.Euler(new Vector3(-90,-90,0)));// rotate rootRotXZ
		DS3BoneRotQ(48, DQuaternion.Euler(new Vector3(0, 20, 0))); // rotate L clavicle
		DS3BoneRotQ(79, DQuaternion.Euler(new Vector3(0, 20, 0)));// rotate R Clavicle
		DS3BoneMov(48, new Vector3(0, 0, -0.085f)); // move L clavicle
		DS3BoneMov(79, new Vector3(0, 0, -0.085f)); // move R Clavicle
		DS3BoneMov(41, new Vector3(0, 0.093f, 0)); // move spine1

		ds3_skeleton.ApplyAnimationPose(0);
	}
	public void DS3BonePosSameAsReference(Skeleton s, int boneId)
	{
		for (int j = 0; j < ds3_skeleton.animation_blocks.Count; j++)
		{
			var track = s.animation_blocks[j][boneId];
			if (track.SplinePosition != null)
			{
				if (track.SplinePosition.ChannelX != null)
					track.SplinePosition.ChannelX.Values = (from x in track.SplinePosition.ChannelX.Values select s.referencePose[boneId].p.x).ToList();
				if (track.SplinePosition.ChannelY != null)
					track.SplinePosition.ChannelY.Values = (from y in track.SplinePosition.ChannelY.Values select s.referencePose[boneId].p.y).ToList();
				if (track.SplinePosition.ChannelZ != null)
					track.SplinePosition.ChannelZ.Values = (from z in track.SplinePosition.ChannelZ.Values select s.referencePose[boneId].p.z).ToList();
			}
			else
			{
				track.StaticPosition = s.referencePose[boneId].p;
			}
		}
	}

	public void DS3BoneRotSameAsReference(Skeleton s, int boneId)
	{
		for (int j = 0; j < ds3_skeleton.animation_blocks.Count; j++)
		{
			var track = s.animation_blocks[j][boneId];
			track.SplineRotation = null;
			track.HasSplineRotation = false;
			track.HasStaticRotation = true;
			track.Mask.RotationTypes.Clear();
			track.Mask.RotationTypes.Add(SCA.FlagOffset.StaticX);
			track.Mask.RotationTypes.Add(SCA.FlagOffset.StaticY);
			track.Mask.RotationTypes.Add(SCA.FlagOffset.StaticZ);
			track.Mask.RotationTypes.Add(SCA.FlagOffset.StaticW);
			track.StaticRotation = DQuaternion.FromQ(ds3_skeleton.referencePose[boneId].r); //rotation from reference pose
		}
	}

	public void DS3BoneFlipZRot(int boneId)
	{
		for (int j = 0; j < ds3_skeleton.animation_blocks.Count; j++)
		{
			var track = ds3_skeleton.animation_blocks[j][boneId];
			if (track.SplineRotation != null)
			{
				track.SplineRotation.Channel.Values = (from q in track.SplineRotation.Channel.Values select new DQuaternion(0, 0, 1, 0) * q).ToList();
			}
			else
			{
				track.StaticRotation = new DQuaternion(0, 0, 1, 0) * track.StaticRotation;
			}
		}
	}

	public void DS3ChangeBone(int boneId, Action<SCA.TransformTrack> act, List<int> ignoredChildren = null, int blockid = -1)
	{

		var bone = ds3_skeleton.bones[boneId];
		List<int> children = new List<int>();
		List<List<List<Vector3>>> cpos = new List<List<List<Vector3>>>();
		List<List<List<Quaternion>>> crot = new List<List<List<Quaternion>>>();
		for (int j = 0; j < ds3_skeleton.animation_blocks.Count; j++)
		{
			cpos.Add(new List<List<Vector3>>());
			crot.Add(new List<List<Quaternion>>());
		}
		foreach (Transform b in bone.transform)
		{
			int id = int.Parse(b.name.Split('(', ')')[1]);
			if (ignoredChildren != null && ignoredChildren.Contains(id)) continue;
			children.Add(id);
			for (int j = 0; j < ds3_skeleton.animation_blocks.Count; j++)
			{
				cpos[j].Add(new List<Vector3>());
				crot[j].Add(new List<Quaternion>());
			}
		}

		int start_frame = blockid == -1 ? 0 : (blockid * 256);
		int duration = blockid == -1 ? ds3_skeleton.animationFrames : Math.Min(ds3_skeleton.animationFrames - start_frame, 256);

		for (int i = start_frame; i < start_frame + duration; i++)
		{
			ds3_skeleton.ApplyAnimationPose(i);
			for (int c = 0; c < children.Count; c++)
			{
				var cb = ds3_skeleton.bones[children[c]].transform;
				cpos[i / 256][c].Add(cb.position);
				crot[i / 256][c].Add(cb.rotation);
			}
		}

		for (int j = 0; j < ds3_skeleton.animation_blocks.Count; j++)
		{
			if (blockid != -1 && j != blockid) continue;
			var track = ds3_skeleton.animation_blocks[j][boneId];
			act(track);
		}

		for (int i = start_frame; i < start_frame + duration; i++)
		{
			ds3_skeleton.ApplyAnimationPose(i);
			for (int c = 0; c < children.Count; c++)
			{
				var cb = ds3_skeleton.bones[children[c]].transform;
				cb.position = cpos[i / 256][c][i % 256];
				cb.rotation = crot[i / 256][c][i % 256];
				cpos[i / 256][c][i % 256] = cb.localPosition;
				crot[i / 256][c][i % 256] = cb.localRotation;
			}
		}

		for (int c = 0; c < children.Count; c++)
		{
			for (int j = 0; j < ds3_skeleton.animation_blocks.Count; j++)
			{
				if (blockid != -1 && j != blockid) continue;
				int block_start = j * 256;
				int block_duration = Math.Min(ds3_skeleton.animationFrames - block_start, 256);
				var ct = ds3_skeleton.animation_blocks[j][children[c]];
				ct.HasSplinePosition = true;
				ct.HasSplineRotation = true;
				ct.Mask.PositionTypes.Clear();
				ct.Mask.PositionTypes.Add(SCA.FlagOffset.SplineX);
				ct.Mask.PositionTypes.Add(SCA.FlagOffset.SplineY);
				ct.Mask.PositionTypes.Add(SCA.FlagOffset.SplineZ);
				ct.Mask.RotationTypes.Clear();
				ct.Mask.RotationTypes.Add(SCA.FlagOffset.SplineX);
				ct.Mask.RotationTypes.Add(SCA.FlagOffset.SplineY);
				ct.Mask.RotationTypes.Add(SCA.FlagOffset.SplineZ);
				ct.Mask.RotationTypes.Add(SCA.FlagOffset.SplineW);

				var knotList = (from k in Enumerable.Range(-1, block_duration + 1) select (byte)Math.Max(k, 0)).ToList();

				var xs = (from v in cpos[j][c] select v.x).ToList();
				var ys = (from v in cpos[j][c] select v.y).ToList();
				var zs = (from v in cpos[j][c] select v.z).ToList();
				ct.SplinePosition = new SCA.SplineTrackVector3(
					xs,
					ys,
					zs,
					knotList,
					1
				);
				ct.SplinePosition.BoundsXMax = xs.Max();
				ct.SplinePosition.BoundsXMin = xs.Min();
				ct.SplinePosition.BoundsYMax = ys.Max();
				ct.SplinePosition.BoundsYMin = ys.Min();
				ct.SplinePosition.BoundsZMax = zs.Max();
				ct.SplinePosition.BoundsZMin = zs.Min();

				ct.SplineRotation = new SCA.SplineTrackQuaternion(
					(from q in crot[j][c] select DQuaternion.FromQ(q)).ToList(),
					knotList,
					1
				);


				//if(ct.SplinePosition != null)
				//{
				//	List<Vector3> ps = new List<Vector3>();
				//	for(int i = 0; i < ct.SplinePosition.ChannelX.Values.Count; i++)
				//	{
				//		ps.Add(quat.ToQ() * new Vector3(
				//			ct.SplinePosition.ChannelX.Values[i],
				//			ct.SplinePosition.ChannelY.Values[i],
				//			ct.SplinePosition.ChannelZ.Values[i]
				//		));
				//	}
				//	ct.SplinePosition.ChannelX.Values = (from v in ps select v.x).ToList();
				//	ct.SplinePosition.ChannelY.Values = (from v in ps select v.y).ToList();
				//	ct.SplinePosition.ChannelZ.Values = (from v in ps select v.z).ToList();
				//} else
				//{
				//	ct.StaticPosition = quat.ToQ() * ct.StaticPosition;
				//}

			}
		}
	}

	public void DS3BoneRotQ(int boneId, DQuaternion quat)
	{
		DS3ChangeBone(boneId, (SCA.TransformTrack track) =>
		{
			//TODO: try to make it a full list if it bugs out
			if (track.SplineRotation != null)
			{
				track.SplineRotation.Channel.Values = (from q in track.SplineRotation.Channel.Values select q * quat).ToList();
			}
			else
			{
				track.StaticRotation = track.StaticRotation * quat;
			}
		});
	}

	public void DS3BoneMov(int boneId, Vector3 v)
	{
		DS3ChangeBone(boneId, (SCA.TransformTrack track) =>
		{
			//TODO: try to make it a full list if it bugs out
			if (track.SplinePosition != null)
			{
				track.SplinePosition.ChannelX.Values = (from x in track.SplinePosition.ChannelX.Values select x + v.x).ToList();
				track.SplinePosition.ChannelY.Values = (from y in track.SplinePosition.ChannelY.Values select y + v.y).ToList();
				track.SplinePosition.ChannelZ.Values = (from z in track.SplinePosition.ChannelZ.Values select z + v.z).ToList();
				track.SplinePosition.BoundsXMax = track.SplinePosition.ChannelX.Values.Max();
				track.SplinePosition.BoundsXMin = track.SplinePosition.ChannelX.Values.Min();
				track.SplinePosition.BoundsYMax = track.SplinePosition.ChannelY.Values.Max();
				track.SplinePosition.BoundsYMin = track.SplinePosition.ChannelY.Values.Min();
				track.SplinePosition.BoundsZMax = track.SplinePosition.ChannelZ.Values.Max();
				track.SplinePosition.BoundsZMin = track.SplinePosition.ChannelZ.Values.Min();
			}
			else
			{
				track.StaticPosition = track.StaticPosition + v;
			}
		});
	}

	public void SetDS3Animation()
	{
		XElement xml = XElement.Parse(ds3_anim.text);
		XElement data = (from item in xml.Elements() where item.Attribute("name").Value == "__data__" select item).First();
		XElement xSCA = (from item in data.Elements() where item.Attribute("class").Value == "hkaSplineCompressedAnimation" select item).First();

		int numTransTracks = int.Parse((from item in xSCA.Elements() where item.Attribute("name").Value == "numberOfTransformTracks" select item).First().Value);
		int frames = int.Parse((from item in xSCA.Elements() where item.Attribute("name").Value == "numFrames" select item).First().Value);

		byte[] dataVals =
			(from v in
				(from item in xSCA.Elements() where item.Attribute("name").Value == "data" select item)
			.First().Value.Split(new char[0], System.StringSplitOptions.RemoveEmptyEntries)
			 select byte.Parse(v)).ToArray();

		ds3_skeleton.animation_blocks = SCA.ReadSplineCompressedAnimByteBlock(false, dataVals, numTransTracks, 1);
		ds3_skeleton.animationFrames = frames;

		ds3_skeleton.ApplyAnimationPose(0);
	}

	public void SaveSkeletonAnimationToXML(Skeleton s, string inputFile, string fileName, List<(float x, float y, float z, float w)> referenceFrame = null)
	{
		XElement xml;
		if(inputFile == "")
		{
			xml = XElement.Parse(bb_anim.text);
		}else
		{
			xml = XElement.Parse(inputFile);
		}

		XElement data = (from item in xml.Elements() where item.Attribute("name").Value == "__data__" select item).First();

		XElement xSCA = (from item in data.Elements() where item.Attribute("class").Value == "hkaSplineCompressedAnimation" select item).First();
		(from item in xSCA.Elements() where item.Attribute("name").Value == "numberOfTransformTracks" select item).First().Value = s.bones.Count.ToString();
		(from item in xSCA.Elements() where item.Attribute("name").Value == "maskAndQuantizationSize" select item).First().Value = (s.bones.Count * 4).ToString();
		XElement a_tracks = (from item in xSCA.Elements() where item.Attribute("name").Value == "annotationTracks" select item).First();
		a_tracks.RemoveNodes();
		a_tracks.Attribute("numelements").Value = s.bones.Count.ToString();
		foreach (var b in s.bones)
		{
			a_tracks.Add(
				new XElement("hkobject",
					new XElement("hkparam", new XAttribute("name", "trackName"), b.name.Split('(')[0]),
					new XElement("hkparam", new XAttribute("name", "annotations"), new XAttribute("numelements", "0"))
				)
			);
		}

		List<byte> dat = SCA.CompressAnimation(false, s.animation_blocks, out List<int> blockBounderies);

		if (referenceFrame != null)
		{
			var els = (from item in data.Elements() where item.Attribute("class").Value == "hkaDefaultAnimatedReferenceFrame" select item);
			if (els.Count() == 0)
			{
				//We should do some funky stuff to add the missing element, but I'm lazy now and I can't be bothered to do test the edge cases
				//Most animations do have a reference frame anyway
			}
			else
			{
				XElement xDARF = els.First();
				StringBuilder xRFval = new StringBuilder("\n");
				for (int i = 0; i < referenceFrame.Count; i++)
				{
					xRFval.Append("(" + referenceFrame[i].x + " " + referenceFrame[i].y + " " + referenceFrame[i].z + " " + referenceFrame[i].w + ")\n");
				}
				(from item in xDARF.Elements() where item.Attribute("name").Value == "duration" select item).First().Value = (s.animationFrames * 0.033333f).ToString();
				(from item in xDARF.Elements() where item.Attribute("name").Value == "referenceFrameSamples" select item).First().Value = xRFval.ToString();
				(from item in xDARF.Elements() where item.Attribute("name").Value == "referenceFrameSamples" select item).First().Attribute("numelements").Value = referenceFrame.Count.ToString();
			}
		}

		List<int> floatBlockOffsets = new List<int>();
		for(int i = 1; i <= blockBounderies.Count; i++)
		{
			int cur = i != blockBounderies.Count ? blockBounderies[i] : dat.Count;
			int prev = blockBounderies[i - 1];
			floatBlockOffsets.Add(cur - prev);
		}
		(from item in xSCA.Elements() where item.Attribute("name").Value == "duration" select item).First().Value = (s.animationFrames * 0.033333f).ToString();
		(from item in xSCA.Elements() where item.Attribute("name").Value == "numBlocks" select item).First().Value = blockBounderies.Count.ToString();
		(from item in xSCA.Elements() where item.Attribute("name").Value == "numFrames" select item).First().Value = s.animationFrames.ToString();
		(from item in xSCA.Elements() where item.Attribute("name").Value == "blockOffsets" select item).First().Value = string.Join(" ", blockBounderies);
		(from item in xSCA.Elements() where item.Attribute("name").Value == "blockOffsets" select item).First().Attribute("numelements").Value = blockBounderies.Count.ToString();
		(from item in xSCA.Elements() where item.Attribute("name").Value == "floatBlockOffsets" select item).First().Value = string.Join(" ", floatBlockOffsets);
		(from item in xSCA.Elements() where item.Attribute("name").Value == "floatBlockOffsets" select item).First().Attribute("numelements").Value = floatBlockOffsets.Count.ToString();
		XElement xcData = (from item in xSCA.Elements() where item.Attribute("name").Value == "data" select item).First();
		xcData.Attribute("numelements").Value = dat.Count.ToString();
		StringBuilder xcVal = new StringBuilder("\n");
		for(int i = 0; i < dat.Count; i++)
		{
			xcVal.Append(dat[i].ToString());
			if(i % 16 == 15)
			{
				xcVal.Append("\n");
			} else
			{
				xcVal.Append(" ");
			}
		}
		xcData.Value = xcVal.ToString();

		XElement xAB = (from item in data.Elements() where item.Attribute("class").Value == "hkaAnimationBinding" select item).First();
		XElement tttbi = (from item in xAB.Elements() where item.Attribute("name").Value == "transformTrackToBoneIndices" select item).First();
		tttbi.Attribute("numelements").Value = s.bones.Count.ToString();
		tttbi.Value = "\n";
		for (int i = 0; i < s.bones.Count; i++)
		{
			tttbi.Value += i.ToString();
			if (i % 16 == 15)
			{
				tttbi.Value += "\n";
			}
			else
			{
				tttbi.Value += " ";
			}
		}


		XDocument doc = new XDocument(
			new XDeclaration("1.0","ascii","yes"),
			xml
		);
		if(fileName == "")
		{
			doc.Save("Assets/output.xml");
		} else
		{
			doc.Save(fileName);
		}
	}

	public void SaveDS3AnimationToXML(string inputFile,string fileName, List<(float x, float y, float z, float w)> referenceFrame = null)
	{
		SaveSkeletonAnimationToXML(ds3_skeleton, inputFile, fileName, referenceFrame);
	}

	public void SaveBBAnimationToXML(string fileName)
	{
		SaveSkeletonAnimationToXML(bb_skeleton, "", fileName);
	}

	public void CleanSkeletons()
	{
		foreach (var s in FindObjectsOfType<Skeleton>())
		{
			DestroyImmediate(s.gameObject);
		}
	}

	public void MassConvert()
	{
		var files = new DirectoryInfo("Assets/mass_input").GetFiles("*.xml");
		Debug.Log("Starting Mass Conversion");
		for(int i = 0; i < files.Length; i++)
		{
			var f = files[i];
			Debug.Log(string.Format("Converting {0}/{1} - {2}", i + 1, files.Length, f.Name));
			CleanSkeletons();
			SpawnBBSkeleton();
			SetBBAnimation(f.OpenText().ReadToEnd());
			SpawnDS3Skeleton();
			FitDS3Skeleton();
			SaveDS3AnimationToXML("","Assets/mass_input/output/" + f.Name);
		}
		Debug.Log("Done Converting :)");
	}

	public void DebugTime(Action act)
	{
		var watch = System.Diagnostics.Stopwatch.StartNew();
		act();
		watch.Stop();
		Debug.Log((float)watch.ElapsedMilliseconds / 1000.0f);
	}
}

public class StichData
{
	public int totalAnimations = 0;
	public int frames = 0;
	public List<List<(Vector3 pos, Quaternion rot, Vector3 scale)>> frameAnim = new List<List<(Vector3 pos, Quaternion rot, Vector3 scale)>>();
	public List<(float x, float y, float z, float w)> referenceFrame = new List<(float x, float y, float z, float w)>();
}

#if UNITY_EDITOR
[CustomEditor(typeof(Converter))]
public class ConverterEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		Converter myScript = (Converter)target;
		if (GUILayout.Button("Spawn BB Skeleton"))
		{
			myScript.SpawnBBSkeleton();
		}

		if (GUILayout.Button("Set BB Animation"))
		{
			myScript.SetBBAnimation("");
		}

		if (GUILayout.Button("Spawn DS3 Skeleton"))
		{
			myScript.SpawnDS3Skeleton();
		}
		if (GUILayout.Button("Fit DS3 Skeleton to BB"))
		{
			myScript.FitDS3Skeleton();
		}
		if (GUILayout.Button("Save DS3 Animation to XML"))
		{
			myScript.SaveDS3AnimationToXML("",myScript.outputPath);
		}
		if (GUILayout.Button("Save BB Animation to XML(for testing purposes)"))
		{
			myScript.SaveBBAnimationToXML(myScript.outputPath);
		}
		if (GUILayout.Button("Set DS3 Animation(for testing purposes)"))
		{
			myScript.SetDS3Animation();
		}
		if (GUILayout.Button("Clean Skeletons"))
		{
			myScript.CleanSkeletons();
		}
		if (GUILayout.Button("Mass convert"))
		{
			myScript.MassConvert();
		}
	}
}
#endif
public class SCA
{
	public class BinaryReaderEx
	{
		int pos = 0;
		bool isBigEndian;
		byte[] data;
		public BinaryReaderEx(bool bigEndian, byte[] input)
		{
			isBigEndian = bigEndian;
			data = input;
		}

		public byte ReadByte()
		{
			pos++;
			return data[pos - 1];
		}
		public byte[] ReadBytes(int num)
		{
			byte[] res = new byte[num];
			for (int it = 0; it < num; it++)
			{
				res[it] = ReadByte();
			}
			if(isBigEndian) Array.Reverse(res); //uncomment if big endian
			return res;
		}

		public void Pad(int align)
		{
			if (pos % align > 0)
				pos += align - (pos % align);
		}

		public short ReadInt16()
		{
			return BitConverter.ToInt16(ReadBytes(2), 0);
		}

		public ushort ReadUInt16()
		{
			return BitConverter.ToUInt16(ReadBytes(2), 0);
		}

		public uint ReadUInt32()
		{
			return BitConverter.ToUInt32(ReadBytes(4), 0);
		}

		public float ReadSingle()
		{
			return BitConverter.ToSingle(ReadBytes(4), 0);
		}
	}

	public class BinaryWriterEx
	{
		bool isBigEndian;
		List<byte> data;
		public BinaryWriterEx(bool bigEndian, List<byte> output)
		{
			isBigEndian = bigEndian;
			data = output;
		}
		public void WriteByte(byte val)
		{
			data.Add(val);
		}

		public void WriteBytes(byte[] vals)
		{
			if (isBigEndian) Array.Reverse(vals);
			for (int i = 0; i < vals.Length; i++)
			{
				WriteByte(vals[i]);
			}
		}

		public void Pad(int align)
		{
			while (data.Count % align > 0)
				data.Add(0);
		}

		public void WriteInt16(short val)
		{
			WriteBytes(BitConverter.GetBytes(val));
		}

		public void WriteUInt16(ushort val)
		{
			WriteBytes(BitConverter.GetBytes(val));
		}

		public void WriteSingle(float val)
		{
			WriteBytes(BitConverter.GetBytes(val));
		}

		public void WriteUInt32(uint val)
		{
			WriteBytes(BitConverter.GetBytes(val));
		}
	}

	[Flags]
	public enum FlagOffset : byte
	{
		StaticX = 0b00000001,
		StaticY = 0b00000010,
		StaticZ = 0b00000100,
		StaticW = 0b00001000,
		SplineX = 0b00010000,
		SplineY = 0b00100000,
		SplineZ = 0b01000000,
		SplineW = 0b10000000
	};

	public enum ScalarQuantizationType
	{
		BITS8 = 0,
		BITS16 = 1,
	};

	public enum RotationQuantizationType
	{
		POLAR32 = 0, //4 bytes long
		THREECOMP40 = 1, //5 bytes long
		THREECOMP48 = 2, //6 bytes long
		THREECOMP24 = 3, //3 bytes long
		STRAIGHT16 = 4, //2 bytes long
		UNCOMPRESSED = 5, //16 bytes long
	}

	static int GetRotationAlign(RotationQuantizationType qt)
	{
		switch (qt)
		{
			case RotationQuantizationType.POLAR32: return 4;
			case RotationQuantizationType.THREECOMP40: return 1;
			case RotationQuantizationType.THREECOMP48: return 2;
			case RotationQuantizationType.THREECOMP24: return 1;
			case RotationQuantizationType.STRAIGHT16: return 2;
			case RotationQuantizationType.UNCOMPRESSED: return 4;
			default: throw new NotImplementedException();
		}
	}

	static int GetRotationByteCount(RotationQuantizationType qt)
	{
		switch (qt)
		{
			case RotationQuantizationType.POLAR32: return 4;
			case RotationQuantizationType.THREECOMP40: return 5;
			case RotationQuantizationType.THREECOMP48: return 6;
			case RotationQuantizationType.THREECOMP24: return 3;
			case RotationQuantizationType.STRAIGHT16: return 2;
			case RotationQuantizationType.UNCOMPRESSED: return 16;
			default: throw new NotImplementedException();
		}
	}

	static float ReadQuantizedFloat(BinaryReaderEx bin, float min, float max, ScalarQuantizationType type)
	{
		float ratio = -1;
		switch (type)
		{
			case ScalarQuantizationType.BITS8: ratio = bin.ReadByte() / 255.0f; break;
			case ScalarQuantizationType.BITS16: ratio = bin.ReadUInt16() / 65535.0f; break;
			default: throw new NotImplementedException();
		}
		return min + ((max - min) * ratio);
	}

	static void WriteQuantizedFloat(float val, BinaryWriterEx bw, float min, float max, ScalarQuantizationType type)
	{
		float ratio = (val - min) / (max - min);
		switch (type)
		{
			case ScalarQuantizationType.BITS8: bw.WriteByte((byte)(ratio * 255.0f)); break;
			case ScalarQuantizationType.BITS16: bw.WriteUInt16((ushort)(ratio * 65535.0f)); break;
			default: throw new NotImplementedException();
		}
	}
	// Because C# can't static cast an int to a float natively
	static float CastToFloat(uint src)
	{
		var floatbytes = BitConverter.GetBytes(src);
		return BitConverter.ToSingle(floatbytes, 0);
	}

	static Quaternion ReadQuatPOLAR32(BinaryReaderEx br)
	{
		const ulong rMask = (1 << 10) - 1;
		const float rFrac = 1.0f / rMask;
		const float fPI = 3.14159265f;
		const float fPI2 = 0.5f * fPI;
		const float fPI4 = 0.5f * fPI2;
		const float phiFrac = fPI2 / 511.0f;

		uint cVal = br.ReadUInt32();

		float R = CastToFloat((cVal >> 18) & (uint)(rMask & 0xFFFFFFFF)) * rFrac;
		R = 1.0f - (R * R);

		float phiTheta = (float)((cVal & 0x3FFFF));

		float phi = (float)Math.Floor(Math.Sqrt(phiTheta));
		float theta = 0;

		if (phi > 0.0f)
		{
			theta = fPI4 * (phiTheta - (phi * phi)) / phi;
			phi = phiFrac * phi;
		}

		float magnitude = (float)Math.Sqrt(1.0f - R * R);

		Quaternion retVal;
		retVal.x = (float)(Math.Sin(phi) * Math.Cos(theta) * magnitude);
		retVal.y = (float)(Math.Sin(phi) * Math.Sin(theta) * magnitude);
		retVal.z = (float)(Math.Cos(phi) * magnitude);
		retVal.w = R;

		if ((cVal & 0x10000000) > 0)
			retVal.x *= -1;

		if ((cVal & 0x20000000) > 0)
			retVal.y *= -1;

		if ((cVal & 0x40000000) > 0)
			retVal.z *= -1;

		if ((cVal & 0x80000000) > 0)
			retVal.w *= -1;

		return retVal;
	}

	static Quaternion ReadQuatTHREECOMP48(BinaryReaderEx br)
	{
		const ulong mask = (1 << 15) - 1;
		const float fractal = 0.000043161f;

		short x = br.ReadInt16();
		short y = br.ReadInt16();
		short z = br.ReadInt16();

		char resultShift = (char)(((y >> 14) & 2) | ((x >> 15) & 1));
		bool rSign = (z >> 15) != 0;

		x &= (short)mask;
		x -= (short)(mask >> 1);
		y &= (short)mask;
		y -= (short)(mask >> 1);
		z &= (short)mask;
		z -= (short)(mask >> 1);

		float[] tempValF = new float[3];
		tempValF[0] = (float)x * fractal;
		tempValF[1] = (float)y * fractal;
		tempValF[2] = (float)z * fractal;

		float[] retval = new float[4];

		for (int i = 0; i < 4; i++)
		{
			if (i < resultShift)
				retval[i] = tempValF[i];
			else if (i > resultShift)
				retval[i] = tempValF[i - 1];
		}

		retval[resultShift] = 1.0f - tempValF[0] * tempValF[0] - tempValF[1] * tempValF[1] - tempValF[2] * tempValF[2];

		if (retval[resultShift] <= 0.0f)
			retval[resultShift] = 0.0f;
		else
			retval[resultShift] = (float)Math.Sqrt(retval[resultShift]);

		if (rSign)
			retval[resultShift] *= -1;

		return new Quaternion(retval[0], retval[1], retval[2], retval[3]);
	}

	static ulong Read40BitValue(BinaryReaderEx br)
	{
		byte[] bytes = br.ReadBytes(5);
		Array.Resize(ref bytes, 8);
		return BitConverter.ToUInt64(bytes, 0);
	}

	static Quaternion ReadQuatTHREECOMP40(BinaryReaderEx br)
	{
		const ulong mask = (1 << 12) - 1;
		const ulong positiveMask = mask >> 1;
		const float fractal = 0.000345436f;
		// Read only the 5 bytes needed to prevent EndOfStreamException :fatcat:
		ulong cVal = Read40BitValue(br);

		int x = (int)(cVal & mask);
		int y = (int)((cVal >> 12) & mask);
		int z = (int)((cVal >> 24) & mask);

		int resultShift = (int)((cVal >> 36) & 3);

		x -= (int)positiveMask;
		y -= (int)positiveMask;
		z -= (int)positiveMask;

		float[] tempValF = new float[3];
		tempValF[0] = (float)x * fractal;
		tempValF[1] = (float)y * fractal;
		tempValF[2] = (float)z * fractal;

		float[] retval = new float[4];

		for (int i = 0; i < 4; i++)
		{
			if (i < resultShift)
				retval[i] = tempValF[i];
			else if (i > resultShift)
				retval[i] = tempValF[i - 1];
		}

		retval[resultShift] = 1.0f - tempValF[0] * tempValF[0] - tempValF[1] * tempValF[1] - tempValF[2] * tempValF[2];

		if (retval[resultShift] <= 0.0f)
			retval[resultShift] = 0.0f;
		else
			retval[resultShift] = (float)Math.Sqrt(retval[resultShift]);

		if (((cVal >> 38) & 1) > 0)
			retval[resultShift] *= -1;

		var finalQuat = new Quaternion(retval[0], retval[1], retval[2], retval[3]);

		return finalQuat;

	}

	static Quaternion ReadQuantizedQuaternion(BinaryReaderEx br, RotationQuantizationType type)
	{
		switch (type)
		{
			case RotationQuantizationType.POLAR32:
				return ReadQuatPOLAR32(br);
			case RotationQuantizationType.THREECOMP40:
				return ReadQuatTHREECOMP40(br);
			case RotationQuantizationType.THREECOMP48:
				return ReadQuatTHREECOMP48(br);
			case RotationQuantizationType.THREECOMP24:
			case RotationQuantizationType.STRAIGHT16:
				throw new NotImplementedException();
			case RotationQuantizationType.UNCOMPRESSED:
				return new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
			default:
				return Quaternion.identity;
		}
	}

	static void WriteQuantizedQuaternion(Quaternion val, BinaryWriterEx bw, RotationQuantizationType type)
	{
		//switch (type)
		//{
		//	case RotationQuantizationType.POLAR32:
		//		WriteQuatPOLAR32(bw); break;
		//	case RotationQuantizationType.THREECOMP40:
		//		WriteQuatTHREECOMP40(bw); break;
		//	case RotationQuantizationType.THREECOMP48:
		//		WriteQuatTHREECOMP48(bw); break;
		//	case RotationQuantizationType.THREECOMP24:
		//	case RotationQuantizationType.STRAIGHT16:
		//		throw new NotImplementedException();
		//	case RotationQuantizationType.UNCOMPRESSED:

		// Write every quaternion uncompressed, because I can't be bothered
		bw.WriteSingle(val.x);
		bw.WriteSingle(val.y);
		bw.WriteSingle(val.z);
		bw.WriteSingle(val.w);

		//	break;
		//}
	}
	// Algorithm A2.1 The NURBS Book 2nd edition, page 68
	static int FindKnotSpan(int degree, float value, int cPointsSize, List<byte> knots)
	{
		if (value >= knots[cPointsSize])
			return cPointsSize - 1;

		int low = degree;
		int high = cPointsSize;
		int mid = (low + high) / 2;

		while (value < knots[mid] || value >= knots[mid + 1])
		{
			if (value < knots[mid])
				high = mid;
			else
				low = mid;

			mid = (low + high) / 2;
		}

		return mid;
	}

	//Basis_ITS1, GetPoint_NR1, TIME-EFFICIENT NURBS CURVE EVALUATION ALGORITHMS, pages 64 & 65
	static float GetSinglePoint(int knotSpanIndex, int degree, float frame, List<byte> knots, List<float> cPoints)
	{
		float[] N = { 1, 0, 0, 0, 0 };

		for (int i = 1; i <= degree; i++)
			for (int j = i - 1; j >= 0; j--)
			{
				//i is 1, j is 0
				float A = (frame - knots[knotSpanIndex - j]) / (knots[knotSpanIndex + i - j] - knots[knotSpanIndex - j]);
				// without multiplying A, model jitters slightly
				float tmp = N[j] * A;
				// without subtracting tmp, model flies away then resets to origin every few frames
				N[j + 1] += N[j] - tmp;
				// without setting to tmp, model either is moved from origin or grows very long limbs
				// depending on the animation
				N[j] = tmp;
			}

		float retVal = 0.0f;

		for (int i = 0; i <= degree; i++)
			retVal += cPoints[knotSpanIndex - i] * N[i];

		return retVal;
	}

	//Basis_ITS1, GetPoint_NR1, TIME-EFFICIENT NURBS CURVE EVALUATION ALGORITHMS, pages 64 & 65
	static DQuaternion GetSinglePoint(int knotSpanIndex, int degree, float frame, List<byte> knots, List<DQuaternion> cPoints)
	{
		float[] N = { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f };

		for (int i = 1; i <= degree; i++)
			for (int j = i - 1; j >= 0; j--)
			{
				float A = (frame - knots[knotSpanIndex - j]) / (knots[knotSpanIndex + i - j] - knots[knotSpanIndex - j]);
				float tmp = N[j] * A;
				N[j + 1] += N[j] - tmp;
				N[j] = tmp;
			}

		DQuaternion retVal = new DQuaternion(0.0f, 0.0f, 0.0f, 0.0f);

		for (int i = 0; i <= degree; i++)
			retVal = new DQuaternion(
				retVal.x + cPoints[knotSpanIndex - i].x * N[i],
				retVal.y + cPoints[knotSpanIndex - i].y * N[i],
				retVal.z + cPoints[knotSpanIndex - i].z * N[i],
				retVal.w + cPoints[knotSpanIndex - i].w * N[i]
			);

		return retVal;
	}

	static void MulSinglePoint(DQuaternion q,List<DQuaternion> editedPoints, int knotSpanIndex, int degree, float frame, List<byte> knots, List<DQuaternion> cPoints)
	{
		float[] N = { 1.0f, 0.0f, 0.0f, 0.0f, 0.0f };

		for (int i = 1; i <= degree; i++)
			for (int j = i - 1; j >= 0; j--)
			{
				float A = (frame - knots[knotSpanIndex - j]) / (knots[knotSpanIndex + i - j] - knots[knotSpanIndex - j]);
				float tmp = N[j] * A;
				N[j + 1] += N[j] - tmp;
				N[j] = tmp;
			}

		DQuaternion val = GetSinglePoint(knotSpanIndex, degree, frame, knots, cPoints);
		DQuaternion change = new DQuaternion(
			(val * q).x - val.x,
			(val * q).y - val.y,
			(val * q).z - val.z,
			(val * q).w - val.w
		);
		float sumN = N.Sum();

		for (int i = 0; i <= degree; i++)
		{
			DQuaternion propChange = new DQuaternion(
				change.x * (N[i] / sumN),
				change.y * (N[i] / sumN),
				change.z * (N[i] / sumN),
				change.w * (N[i] / sumN)
			);
			editedPoints[knotSpanIndex - i] = new DQuaternion(
				editedPoints[knotSpanIndex - i].x + propChange.x,
				editedPoints[knotSpanIndex - i].y + propChange.y,
				editedPoints[knotSpanIndex - i].z + propChange.z,
				editedPoints[knotSpanIndex - i].w + propChange.w
			);
		}
	}

	public class SplineChannel<T>
	{
		public bool IsDynamic = true;
		public List<T> Values = new List<T>();
		public SplineChannel()
		{

		}
		public SplineChannel(List<T> v)
		{
			Values = v;
		}

		public SplineChannel<T> Clone()
		{
			SplineChannel<T> c = (SplineChannel<T>)MemberwiseClone();
			c.Values = new List<T>(Values);
			return c;
		}
	}

	public class SplineTrackQuaternion
	{
		public SplineChannel<DQuaternion> Channel;
		public List<byte> Knots = new List<byte>();
		public byte Degree;

		public SplineTrackQuaternion(List<DQuaternion> c, List<byte> k, byte d)
		{
			Channel = new SplineChannel<DQuaternion>();
			Channel.Values = c;
			Knots = k;
			Degree = d;
		}

		internal SplineTrackQuaternion(BinaryReaderEx br, RotationQuantizationType quantizationType)
		{
			//long debug_StartOfThisSplineTrack = br.Position;

			short numItems = br.ReadInt16();
			Degree = br.ReadByte();
			int knotCount = numItems + Degree + 2;
			for (int i = 0; i < knotCount; i++)
			{
				Knots.Add(br.ReadByte());
			}

			br.Pad(GetRotationAlign(quantizationType));

			Channel = new SplineChannel<DQuaternion>();

			for (int i = 0; i <= numItems; i++)
			{
				Channel.Values.Add(DQuaternion.FromQ(ReadQuantizedQuaternion(br, quantizationType)));

				//try
				//{

				//}
				//catch (System.IO.EndOfStreamException)
				//{
				//    // TEST
				//    Channel.Values.Add(DQuaternion.Identity);
				//}
			}
		}

		public void Compress(BinaryWriterEx bw, RotationQuantizationType quantizationType)
		{
			int knotCount = Knots.Count;
			short numItems = (short)(knotCount - Degree - 2);
			bw.WriteInt16(numItems);
			bw.WriteByte(Degree);
			foreach(var b in Knots)
			{
				bw.WriteByte(b);
			}

			bw.Pad(GetRotationAlign(quantizationType));

			for (int i = 0; i <= numItems; i++)
			{
				WriteQuantizedQuaternion(Channel.Values[i].ToQ(), bw, quantizationType);
			}
		}

		public DQuaternion GetValue(float frame)
		{
			int knotspan = FindKnotSpan(Degree, frame, Channel.Values.Count, Knots);
			return GetSinglePoint(knotspan, Degree, frame, Knots, Channel.Values);

		}

		internal SplineTrackQuaternion Clone()
		{
			SplineTrackQuaternion c = (SplineTrackQuaternion)MemberwiseClone();
			c.Channel = Channel?.Clone();
			c.Knots = new List<byte>(Knots);
			return c;
		}
	}

	public class SplineTrackVector3
	{
		public SplineChannel<float> ChannelX;
		public SplineChannel<float> ChannelY;
		public SplineChannel<float> ChannelZ;
		public List<byte> Knots = new List<byte>();
		public byte Degree;

		public float BoundsXMin;
		public float BoundsXMax;
		public float BoundsYMin;
		public float BoundsYMax;
		public float BoundsZMin;
		public float BoundsZMax;


		public SplineTrackVector3(List<float> cx, List<float> cy, List<float> cz, List<byte> knots, byte deg)
		{
			if(cx != null)
				ChannelX = new SplineChannel<float>(cx);
			if(cy != null)
				ChannelY = new SplineChannel<float>(cy);
			if(cz != null)
				ChannelZ = new SplineChannel<float>(cz);
			Knots = knots;
			Degree = deg;
		}
		internal SplineTrackVector3(BinaryReaderEx br, List<FlagOffset> channelTypes, ScalarQuantizationType quantizationType, bool isPosition)
		{
			//long debug_StartOfThisSplineTrack = br.Position;

			short numItems = br.ReadInt16();
			Degree = br.ReadByte();
			int knotCount = numItems + Degree + 2;
			for (int i = 0; i < knotCount; i++)
			{
				Knots.Add(br.ReadByte());
			}

			br.Pad(4);

			BoundsXMin = 0;
			BoundsXMax = 0;
			BoundsYMin = 0;
			BoundsYMax = 0;
			BoundsZMin = 0;
			BoundsZMax = 0;

			ChannelX = new SplineChannel<float>();
			ChannelY = new SplineChannel<float>();
			ChannelZ = new SplineChannel<float>();

			if (channelTypes.Contains(FlagOffset.SplineX))
			{
				BoundsXMin = br.ReadSingle();
				BoundsXMax = br.ReadSingle();
			}
			else if (channelTypes.Contains(FlagOffset.StaticX))
			{
				ChannelX.Values = new List<float> { br.ReadSingle() };
				ChannelX.IsDynamic = false;
			}
			else
			{
				ChannelX = null;
			}

			if (channelTypes.Contains(FlagOffset.SplineY))
			{
				BoundsYMin = br.ReadSingle();
				BoundsYMax = br.ReadSingle();
			}
			else if (channelTypes.Contains(FlagOffset.StaticY))
			{
				ChannelY.Values = new List<float> { br.ReadSingle() };
				ChannelY.IsDynamic = false;
			}
			else
			{
				ChannelY = null;
			}

			if (channelTypes.Contains(FlagOffset.SplineZ))
			{
				BoundsZMin = br.ReadSingle();
				BoundsZMax = br.ReadSingle();
			}
			else if (channelTypes.Contains(FlagOffset.StaticZ))
			{
				ChannelZ.Values = new List<float> { br.ReadSingle() };
				ChannelZ.IsDynamic = false;
			}
			else
			{
				ChannelZ = null;
			}

			for (int i = 0; i <= numItems; i++)
			{
				if (channelTypes.Contains(FlagOffset.SplineX))
				{
					ChannelX.Values.Add(ReadQuantizedFloat(br, BoundsXMin, BoundsXMax, quantizationType));
				}

				if (channelTypes.Contains(FlagOffset.SplineY))
				{
					ChannelY.Values.Add(ReadQuantizedFloat(br, BoundsYMin, BoundsYMax, quantizationType));
				}

				if (channelTypes.Contains(FlagOffset.SplineZ))
				{
					ChannelZ.Values.Add(ReadQuantizedFloat(br, BoundsZMin, BoundsZMax, quantizationType));
				}
			}
		}

		public void Compress(BinaryWriterEx bw, List<FlagOffset> channelTypes, ScalarQuantizationType quantizationType, bool isPosition)
		{
			int knotCount = Knots.Count;
			short numItems = (short)(knotCount - Degree - 2);
			bw.WriteInt16(numItems);
			bw.WriteByte(Degree);
			foreach(var b in Knots)
			{
				bw.WriteByte(b);
			}

			bw.Pad(4);

			//--GET THE FLOATS FIRST--//

			if (channelTypes.Contains(FlagOffset.SplineX))
			{
				bw.WriteSingle(BoundsXMin);
				bw.WriteSingle(BoundsXMax);
			}
			else if (channelTypes.Contains(FlagOffset.StaticX))
			{
				bw.WriteSingle(ChannelX.Values[0]);
			}

			if (channelTypes.Contains(FlagOffset.SplineY))
			{
				bw.WriteSingle(BoundsYMin);
				bw.WriteSingle(BoundsYMax);
			}
			else if (channelTypes.Contains(FlagOffset.StaticY))
			{
				bw.WriteSingle(ChannelY.Values[0]);
			}

			if (channelTypes.Contains(FlagOffset.SplineZ))
			{
				bw.WriteSingle(BoundsZMin);
				bw.WriteSingle(BoundsZMax);
			}
			else if (channelTypes.Contains(FlagOffset.StaticZ))
			{
				bw.WriteSingle(ChannelZ.Values[0]);
			}

			for (int i = 0; i <= numItems; i++)
			{
				if (channelTypes.Contains(FlagOffset.SplineX))
				{
					WriteQuantizedFloat(ChannelX.Values[i], bw, BoundsXMin, BoundsXMax, quantizationType);
				}

				if (channelTypes.Contains(FlagOffset.SplineY))
				{
					WriteQuantizedFloat(ChannelY.Values[i], bw, BoundsYMin, BoundsYMax, quantizationType);
				}

				if (channelTypes.Contains(FlagOffset.SplineZ))
				{
					WriteQuantizedFloat(ChannelZ.Values[i], bw, BoundsZMin, BoundsZMax, quantizationType);
				}
			}
		}

		public float? GetValueX(float frame)
		{
			if (ChannelX == null)
				return null;

			if (ChannelX.Values.Count == 1)
				return ChannelX.Values[0];
			int knotspan = FindKnotSpan(Degree, frame, ChannelX.Values.Count, Knots);
			return GetSinglePoint(knotspan, Degree, frame, Knots, ChannelX.Values);

		}

		public float? GetValueY(float frame)
		{
			if (ChannelY == null)
				return null;

			if (ChannelY.Values.Count == 1)
				return ChannelY.Values[0];
			int knotspan = FindKnotSpan(Degree, frame, ChannelY.Values.Count, Knots);
			return GetSinglePoint(knotspan, Degree, frame, Knots, ChannelY.Values);
		}

		public float? GetValueZ(float frame)
		{
			if (ChannelZ == null)
				return null;

			if (ChannelZ.Values.Count == 1)
				return ChannelZ.Values[0];
			int knotspan = FindKnotSpan(Degree, frame, ChannelZ.Values.Count, Knots);
			return GetSinglePoint(knotspan, Degree, frame, Knots, ChannelZ.Values);

		}

		public SplineTrackVector3 Clone()
		{
			SplineTrackVector3 c = (SplineTrackVector3)MemberwiseClone();
			c.ChannelX = ChannelX?.Clone();
			c.ChannelY = ChannelY?.Clone();
			c.ChannelZ = ChannelZ?.Clone();
			c.Knots = new List<byte>(Knots);
			return c;
		}
	}

	public class TransformMask
	{
		public ScalarQuantizationType PositionQuantizationType;
		public RotationQuantizationType RotationQuantizationType;
		public ScalarQuantizationType ScaleQuantizationType;
		public List<FlagOffset> PositionTypes;
		public List<FlagOffset> RotationTypes;
		public List<FlagOffset> ScaleTypes;

		public TransformMask()
		{
			PositionTypes = new List<FlagOffset>();
			RotationTypes = new List<FlagOffset>();
			ScaleTypes = new List<FlagOffset>();
		}

		internal TransformMask(BinaryReaderEx br)
		{
			PositionTypes = new List<FlagOffset>();
			RotationTypes = new List<FlagOffset>();
			ScaleTypes = new List<FlagOffset>();

			var byteQuantizationTypes = br.ReadByte();
			var bytePositionTypes = (FlagOffset)br.ReadByte();
			var byteRotationTypes = (FlagOffset)br.ReadByte();
			var byteScaleTypes = (FlagOffset)br.ReadByte();

			PositionQuantizationType = (ScalarQuantizationType)(byteQuantizationTypes & 0b11);
			RotationQuantizationType = (RotationQuantizationType)((byteQuantizationTypes >> 2) & 0xF);
			ScaleQuantizationType = (ScalarQuantizationType)((byteQuantizationTypes >> 6) & 3);

			foreach (var flagOffset in (FlagOffset[])Enum.GetValues(typeof(FlagOffset)))
			{
				if ((bytePositionTypes & flagOffset) != 0)
					PositionTypes.Add(flagOffset);

				if ((byteRotationTypes & flagOffset) != 0)
					RotationTypes.Add(flagOffset);

				if ((byteScaleTypes & flagOffset) != 0)
					ScaleTypes.Add(flagOffset);
			}
		}

		public void Compress(BinaryWriterEx bw)
		{
			byte byteQuantizationTypes = (byte)((int)PositionQuantizationType | ((int)RotationQuantizationType << 2) | ((int)ScaleQuantizationType << 6));
			byte bytePositionTypes = 0;
			byte byteRotationTypes = 0;
			byte byteScaleTypes = 0;

			foreach (var flagOffset in (FlagOffset[])Enum.GetValues(typeof(FlagOffset)))
			{
				if (PositionTypes.Contains(flagOffset))
					bytePositionTypes |= (byte)flagOffset;

				if (RotationTypes.Contains(flagOffset))
					byteRotationTypes |= (byte)flagOffset;

				if (ScaleTypes.Contains(flagOffset))
					byteScaleTypes |= (byte)flagOffset;
			}

			bw.WriteByte(byteQuantizationTypes);
			bw.WriteByte(bytePositionTypes);
			bw.WriteByte(byteRotationTypes);
			bw.WriteByte(byteScaleTypes);
		}
	}

	public class TransformTrack
	{
		public TransformMask Mask;

		public bool HasSplinePosition;
		public bool HasSplineRotation;
		public bool HasSplineScale;

		public bool HasStaticRotation;

		public Vector3 StaticPosition = Vector3.zero;
		public DQuaternion StaticRotation = DQuaternion.identity;
		public Vector3 StaticScale = Vector3.one;
		public SplineTrackVector3 SplinePosition = null;
		public SplineTrackQuaternion SplineRotation = null;
		public SplineTrackVector3 SplineScale = null;

		public TransformTrack Clone()
		{
			TransformTrack c = (TransformTrack)MemberwiseClone();
			c.SplinePosition = c.SplinePosition?.Clone();
			c.SplineRotation = c.SplineRotation?.Clone();
			c.SplineScale = c.SplineScale?.Clone();
			return c;
		}
	}

	public static List<TransformTrack[]> ReadSplineCompressedAnimByteBlock(
		bool isBigEndian, byte[] animationData, int numTransformTracks, int numBlocks)
	{
		List<TransformTrack[]> blocks = new List<TransformTrack[]>();

		var br = new BinaryReaderEx(isBigEndian, animationData);

		for (int blockIndex = 0; blockIndex < numBlocks; blockIndex++)
		{
			var TransformTracks = new TransformTrack[numTransformTracks];

			for (int i = 0; i < numTransformTracks; i++)
			{
				TransformTracks[i] = new TransformTrack();
			}

			for (int i = 0; i < numTransformTracks; i++)
			{
				TransformTracks[i].Mask = new TransformMask(br);
			}

			br.Pad(4);

			for (int i = 0; i < numTransformTracks; i++)
			{
				var m = TransformTracks[i].Mask;
				var track = TransformTracks[i];

				track.HasSplinePosition = m.PositionTypes.Contains(FlagOffset.SplineX)
					|| m.PositionTypes.Contains(FlagOffset.SplineY)
					|| m.PositionTypes.Contains(FlagOffset.SplineZ);

				track.HasSplineRotation = m.RotationTypes.Contains(FlagOffset.SplineX)
					|| m.RotationTypes.Contains(FlagOffset.SplineY)
					|| m.RotationTypes.Contains(FlagOffset.SplineZ)
					|| m.RotationTypes.Contains(FlagOffset.SplineW);

				track.HasStaticRotation = m.RotationTypes.Contains(FlagOffset.StaticX)
					|| m.RotationTypes.Contains(FlagOffset.StaticY)
					|| m.RotationTypes.Contains(FlagOffset.StaticZ)
					|| m.RotationTypes.Contains(FlagOffset.StaticW);

				track.HasSplineScale = m.ScaleTypes.Contains(FlagOffset.SplineX)
					|| m.ScaleTypes.Contains(FlagOffset.SplineY)
					|| m.ScaleTypes.Contains(FlagOffset.SplineZ);

				if (track.HasSplinePosition)
				{
					track.SplinePosition = new SplineTrackVector3(br, m.PositionTypes, m.PositionQuantizationType, isPosition: true);
				}
				else
				{
					if (m.PositionTypes.Contains(FlagOffset.StaticX))
					{
						track.StaticPosition.x = br.ReadSingle();
					}

					if (m.PositionTypes.Contains(FlagOffset.StaticY))
					{
						track.StaticPosition.y = br.ReadSingle();
					}

					if (m.PositionTypes.Contains(FlagOffset.StaticZ))
					{
						track.StaticPosition.z = br.ReadSingle();
					}
				}

				br.Pad(4);



				if (track.HasSplineRotation)
				{
					track.SplineRotation = new SplineTrackQuaternion(br, m.RotationQuantizationType);
				}
				else
				{
					if (track.HasStaticRotation)
					{
						br.Pad(GetRotationAlign(m.RotationQuantizationType));
						track.StaticRotation = DQuaternion.FromQ(ReadQuantizedQuaternion(br, m.RotationQuantizationType)); //br.ReadBytes(GetRotationByteCount(m.RotationQuantizationType));
					}
				}

				br.Pad(4);

				if (track.HasSplineScale)
				{
					track.SplineScale = new SplineTrackVector3(br, m.ScaleTypes, m.ScaleQuantizationType, isPosition: false);
				}
				else
				{
					if (m.ScaleTypes.Contains(FlagOffset.StaticX))
					{
						track.StaticScale.x = br.ReadSingle();
					}

					if (m.ScaleTypes.Contains(FlagOffset.StaticY))
					{
						track.StaticScale.y = br.ReadSingle();
					}

					if (m.ScaleTypes.Contains(FlagOffset.StaticZ))
					{
						track.StaticScale.z = br.ReadSingle();
					}
				}

				br.Pad(4);
			}

			br.Pad(16);

			blocks.Add(TransformTracks);
		}

		return blocks;
	}

	public static List<byte> CompressAnimation(bool isBigEndian, List<TransformTrack[]> blocks, out List<int> blockBounderies)
	{
		List<byte> data = new List<byte>();
		blockBounderies = new List<int>();

		var bw = new BinaryWriterEx(isBigEndian, data);

		for(int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
		{
			blockBounderies.Add(data.Count);
			var TransformTracks = blocks[blockIndex];

			for (int i = 0; i < TransformTracks.Length; i++)
			{
				//write every quaaternion uncompressed, because I can't be bothered
				TransformTracks[i].Mask.RotationQuantizationType = RotationQuantizationType.UNCOMPRESSED;
				TransformTracks[i].Mask.Compress(bw);
			}

			bw.Pad(4);

			for (int i = 0; i < TransformTracks.Length; i++)
			{
				var m = TransformTracks[i].Mask;
				var track = TransformTracks[i];

				if(track.HasSplinePosition)
				{
					track.SplinePosition.Compress(bw,m.PositionTypes,m.PositionQuantizationType,isPosition: true);
				} else
				{
					if (m.PositionTypes.Contains(FlagOffset.StaticX))
					{
						bw.WriteSingle(track.StaticPosition.x);
					}

					if (m.PositionTypes.Contains(FlagOffset.StaticY))
					{
						bw.WriteSingle(track.StaticPosition.y);
					}

					if (m.PositionTypes.Contains(FlagOffset.StaticZ))
					{
						bw.WriteSingle(track.StaticPosition.z);
					}
				}

				bw.Pad(4);

				
				if (track.HasSplineRotation)
				{
					track.SplineRotation.Compress(bw, m.RotationQuantizationType);
				}
				else
				{
					if (track.HasStaticRotation)
					{
						bw.Pad(GetRotationAlign(m.RotationQuantizationType));
						WriteQuantizedQuaternion(track.StaticRotation.ToQ(),bw, m.RotationQuantizationType);
					}
				}

				bw.Pad(4);

				if (track.HasSplineScale)
				{
					track.SplineScale.Compress(bw, m.ScaleTypes, m.ScaleQuantizationType, isPosition: false);
				}
				else
				{
					if (m.ScaleTypes.Contains(FlagOffset.StaticX))
					{
						 bw.WriteSingle(track.StaticScale.x);
					}

					if (m.ScaleTypes.Contains(FlagOffset.StaticY))
					{
						 bw.WriteSingle(track.StaticScale.y);
					}

					if (m.ScaleTypes.Contains(FlagOffset.StaticZ))
					{
						 bw.WriteSingle(track.StaticScale.z);
					}
				}

				bw.Pad(4);
			}

			bw.Pad(16);
		}

		return data;
	}
}