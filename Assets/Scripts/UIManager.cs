using System.Collections;
using System.Collections.Generic;

using System.IO;
using UnityEngine;
using SimpleFileBrowser;

public class UIManager : MonoBehaviour
{
    [HideInInspector]
    public string _rootFolder;      //root folder

    [HideInInspector]
    public string _cpbrtFilePath;       //cpbrtfile

    private Parser _parser;

    public GameManager gm;

    private void Start()
    {
        _parser = new Parser();
    }

    public void OpenFileBrowser()
    {
        FileBrowser.AddQuickLink("Desktop", "C:\\Users\\Desktop", null);
        StartCoroutine(ShowLoadDialogCorotine());
    }

    IEnumerator ShowLoadDialogCorotine()
    {
        yield return FileBrowser.WaitForLoadDialog(false, null, "Select Folder", "Select");
        _cpbrtFilePath = FileBrowser.Result;
        if(_cpbrtFilePath != null)
        {
            _rootFolder = Directory.GetParent(_cpbrtFilePath).FullName;
            _parser.Parse(_rootFolder, _cpbrtFilePath);

            gm.ClearScene();
            gm.GenerateScene(_parser.rayTracingInfo, _parser.outputInfo);
        }
    }

    public void StartRender()
    {
        gm.StartRender(_parser.rayTracingInfo, _parser.outputInfo);
    }
}
