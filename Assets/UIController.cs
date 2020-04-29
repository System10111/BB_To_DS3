using SFB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    // Start is called before the first frame update
    public TextAsset bb_skel;
    public TextAsset ds3_skel;
    public GameObject bone_pf;
    Converter mainConv;
    string input_path = "";
    string stich_path = "";
    string output_path = "";
    List<string> mass_files = new List<string>();
    List<string> mass_output = new List<string>();
    bool massing = false;
    public StichData stichData = new StichData();

    public GameObject advanced_menu;
    void Start()
    {
        GameObject converterobj = new GameObject("Converter");
        mainConv = converterobj.AddComponent<Converter>();
        mainConv.bb_skel = bb_skel;
        mainConv.ds3_skel = ds3_skel;
        mainConv.bone_pf = bone_pf;
    }

    // Update is called once per frame
    void Update()
    {
        if(massing && mass_files.Count > 0)
        {
            input_path = mass_files[0];
            mass_files.RemoveAt(0);
            output_path = mass_output[0];
            mass_output.RemoveAt(0);
            ConvertSingle();
        } else
        {
            massing = false;
        }
    }

    public void SelectInputFile()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Select BB animation", "", "xml", false);
        if (paths.Length == 0) return;
        var path = paths[0];
        var displayPath = path.Length > 40 ? "..." + new string(path.Skip(path.Length - 37).ToArray()) : path; 
        GameObject.Find("Selected_Input").GetComponent<InputField>().text = displayPath;
        input_path = path;
    }

    public void SelectOutputFile()
    {
        var path = StandaloneFileBrowser.SaveFilePanel("Select Output Location", "", "", "xml");
        var displayPath = path.Length > 40 ? "..." + new string(path.Skip(path.Length - 37).ToArray()) : path;
        GameObject.Find("Selected_Output").GetComponent<InputField>().text = displayPath;
        output_path = path;
    }

    public void SpawnBBSkeleton()
    {
        mainConv.SpawnBBSkeleton();
    }

    public void LoadBBAnimation()
    {
        if (input_path == "") return;
        if (stichData.totalAnimations <= 0) stichData = new StichData();
        mainConv.SetBBAnimation(File.ReadAllText(input_path), stichData.referenceFrame);
        GameObject.Find("BB_Slider").GetComponent<Slider>().maxValue = mainConv.bb_skeleton.animationFrames;
    }

    public void SetBBFrame(float val)
    {
        mainConv.bb_skeleton.ApplyAnimationPose((int)val);
    }

    public void SpawnDS3Skeleton()
    {
        mainConv.SpawnDS3Skeleton();
    }

    public void FitDS3Skeleton()
    {
        mainConv.FitDS3Skeleton();
        GameObject.Find("DS_Slider").GetComponent<Slider>().maxValue = mainConv.ds3_skeleton.animationFrames;
    }

    public void SetDS3Frame(float val)
    {
        mainConv.ds3_skeleton.ApplyAnimationPose((int)val);
    }

    public void SaveDS3ToXML()
    {
        if (output_path == "") return;
        mainConv.SaveDS3AnimationToXML(File.ReadAllText(input_path), output_path, stichData.totalAnimations > 0 ? stichData.referenceFrame : null);
    }

    public void CleanSkeletons()
    {
        mainConv.CleanSkeletons();
    }

    public void ConvertSingle()
    {
        if (input_path == "") return;
        if (output_path == "") return;
        CleanSkeletons();
        SpawnBBSkeleton();
        LoadBBAnimation();
        SpawnDS3Skeleton();
        FitDS3Skeleton();
        SaveDS3ToXML();
    }

    public void SelectMassFolder()
    {
        mass_files.Clear();
        mass_output.Clear();
        var paths = StandaloneFileBrowser.OpenFolderPanel("Select Mass Convertion Input Folder", "", false);
        if (paths.Length == 0) return;
        string path = paths[0];
        var displayPath = path.Length > 40 ? "..." + new string(path.Skip(path.Length - 37).ToArray()) : path;
        GameObject.Find("Selected_MassInput").GetComponent<InputField>().text = displayPath;
        output_path = path;
        var files = new DirectoryInfo(path).GetFiles("*.xml");
        foreach(var f in files)
        {
            mass_files.Add(f.FullName);
            mass_output.Add(f.DirectoryName + "/output/" + f.Name);
        }
    }

    public void MassConvert()
    {
        if (mass_files.Count == 0 || mass_output.Count == 0) return;
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(mass_output[0]));
        massing = true;
    }

    public void ToggleShowAdvanced()
    {
        advanced_menu.SetActive(!advanced_menu.activeSelf);
    }

    public void SelectStichInputFile()
    {
        var paths = StandaloneFileBrowser.OpenFilePanel("Select BB animation", "", "xml", false);
        if (paths.Length == 0) return;
        var path = paths[0];
        var displayPath = path.Length > 30 ? "..." + new string(path.Skip(path.Length - 27).ToArray()) : path;
        GameObject.Find("Selected_Stich").GetComponent<InputField>().text = displayPath;
        stich_path = path;
    }

    public void AddStich()
    {
        if (mainConv.ds3_skeleton == null) return;
        if(stichData.frameAnim.Count == 0)
        {
            for(int i = 0; i < mainConv.ds3_skeleton.bones.Count; i++)
            {
                stichData.frameAnim.Add(new List<(Vector3 pos, Quaternion rot, Vector3 Scale)>());
            }
        }
        List<List<(Vector3 pos, Quaternion rot, Vector3 Scale)>> frameAnim = mainConv.ds3_skeleton.GetFrameAnimation();
        for(int i = 0; i < frameAnim.Count; i++)
        {
            stichData.frameAnim[i].AddRange(frameAnim[i]);
        }
        stichData.frames += frameAnim[0].Count;
        stichData.totalAnimations++;
        GameObject.Find("NumAnim").GetComponent<Text>().text = "Number of animations: " + stichData.totalAnimations;
    }

    public void SetStichToDS3Skel()
    {
        if (stichData.totalAnimations <= 0) return;
        CleanSkeletons();
        SpawnDS3Skeleton();
        var dsk = mainConv.ds3_skeleton;
        int blockId = -1;
        SCA.TransformTrack[] block = new SCA.TransformTrack[0];
        List<SCA.TransformTrack[]> anim = new List<SCA.TransformTrack[]>();
        dsk.animationFrames = stichData.frames;
        for(int i = 0; i < stichData.frames; i++)
        {
            if(i/256 != blockId)
            {
                blockId = i / 256;
                block = new SCA.TransformTrack[dsk.bones.Count];
                anim.Add(block);
                for (int j = 0; j < dsk.bones.Count; j++)
                {
                    block[j] = new SCA.TransformTrack();

                    block[j].HasSplinePosition = true;
                    block[j].HasSplineRotation = true;
                    block[j].HasSplineScale = true;
                    block[j].HasStaticRotation = false;

                    block[j].Mask = new SCA.TransformMask();
                    block[j].Mask.PositionTypes = new List<SCA.FlagOffset>();
				    block[j].Mask.PositionTypes.Add(SCA.FlagOffset.SplineX);
				    block[j].Mask.PositionTypes.Add(SCA.FlagOffset.SplineY);
				    block[j].Mask.PositionTypes.Add(SCA.FlagOffset.SplineZ);

				    block[j].Mask.RotationTypes = new List<SCA.FlagOffset>();
				    block[j].Mask.RotationTypes.Add(SCA.FlagOffset.SplineX);
				    block[j].Mask.RotationTypes.Add(SCA.FlagOffset.SplineY);
				    block[j].Mask.RotationTypes.Add(SCA.FlagOffset.SplineZ);
                    block[j].Mask.RotationTypes.Add(SCA.FlagOffset.SplineW);

                    block[j].Mask.ScaleTypes = new List<SCA.FlagOffset>();
                    block[j].Mask.ScaleTypes.Add(SCA.FlagOffset.SplineX);
                    block[j].Mask.ScaleTypes.Add(SCA.FlagOffset.SplineY);
                    block[j].Mask.ScaleTypes.Add(SCA.FlagOffset.SplineZ);

                    block[j].Mask.PositionQuantizationType = SCA.ScalarQuantizationType.BITS16;
                    block[j].Mask.RotationQuantizationType = SCA.RotationQuantizationType.UNCOMPRESSED;
                    block[j].Mask.ScaleQuantizationType = SCA.ScalarQuantizationType.BITS8;

                    block[j].SplinePosition = new SCA.SplineTrackVector3(
                        new List<float>(),
                        new List<float>(),
                        new List<float>(),
                        new List<byte>() { 0 },
                        1
                    );

                    block[j].SplineRotation = new SCA.SplineTrackQuaternion(
                        new List<DQuaternion>(),
                        new List<byte>() { 0 },
                        1
                    );

                    block[j].SplineScale = new SCA.SplineTrackVector3(
                        new List<float>(),
                        new List<float>(),
                        new List<float>(),
                        new List<byte>() { 0 },
                        1
                    );
                }
            }
            for(int j = 0; j < dsk.bones.Count; j++)
            {
                block[j].SplinePosition.Knots.Add((byte)(i % 256));
                block[j].SplinePosition.ChannelX.Values.Add(stichData.frameAnim[j][i].pos.x);
                block[j].SplinePosition.ChannelY.Values.Add(stichData.frameAnim[j][i].pos.y);
                block[j].SplinePosition.ChannelZ.Values.Add(stichData.frameAnim[j][i].pos.z);
                block[j].SplinePosition.BoundsXMax = block[j].SplinePosition.ChannelX.Values.Max();
                block[j].SplinePosition.BoundsXMin = block[j].SplinePosition.ChannelX.Values.Min();
                block[j].SplinePosition.BoundsYMax = block[j].SplinePosition.ChannelY.Values.Max();
                block[j].SplinePosition.BoundsYMin = block[j].SplinePosition.ChannelY.Values.Min();
                block[j].SplinePosition.BoundsZMax = block[j].SplinePosition.ChannelZ.Values.Max();
                block[j].SplinePosition.BoundsZMin = block[j].SplinePosition.ChannelZ.Values.Min();

                block[j].SplineRotation.Knots.Add((byte)(i % 256));
                block[j].SplineRotation.Channel.Values.Add(DQuaternion.FromQ(stichData.frameAnim[j][i].rot));

                block[j].SplineScale.Knots.Add((byte)(i % 256));
                block[j].SplineScale.ChannelX.Values.Add(stichData.frameAnim[j][i].scale.x);
                block[j].SplineScale.ChannelY.Values.Add(stichData.frameAnim[j][i].scale.y);
                block[j].SplineScale.ChannelZ.Values.Add(stichData.frameAnim[j][i].scale.z);
                block[j].SplineScale.BoundsXMax = block[j].SplineScale.ChannelX.Values.Max();
                block[j].SplineScale.BoundsXMin = block[j].SplineScale.ChannelX.Values.Min();
                block[j].SplineScale.BoundsYMax = block[j].SplineScale.ChannelY.Values.Max();
                block[j].SplineScale.BoundsYMin = block[j].SplineScale.ChannelY.Values.Min();
                block[j].SplineScale.BoundsZMax = block[j].SplineScale.ChannelZ.Values.Max();
                block[j].SplineScale.BoundsZMin = block[j].SplineScale.ChannelZ.Values.Min();
            }
        }

        dsk.animation_blocks = anim;
        dsk.ApplyAnimationPose(0);
        GameObject.Find("DS_Slider").GetComponent<Slider>().maxValue = mainConv.ds3_skeleton.animationFrames;
    }

    public void SaveStiched()
    {
        if (output_path == "") return;
        mainConv.SaveSkeletonAnimationToXML(mainConv.ds3_skeleton, File.ReadAllText(input_path), output_path, stichData.referenceFrame);
    }

    public void ClearStich()
    {
        stichData = new StichData();
        GameObject.Find("NumAnim").GetComponent<Text>().text = "Number of animations: " + stichData.totalAnimations;
    }
}
