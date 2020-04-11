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
    string output_path = "";
    List<string> mass_files = new List<string>();
    List<string> mass_output = new List<string>();
    bool massing = false;
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
        mainConv.SetBBAnimation(File.ReadAllText(input_path));
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
        mainConv.SaveDS3AnimationToXML(File.ReadAllText(input_path), output_path);
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
}
