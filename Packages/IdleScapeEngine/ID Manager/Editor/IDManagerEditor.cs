﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(IDManager))]
public class IDManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        IDManager targetObject = (IDManager)target;
        DrawDefaultInspector();

        var extentions = targetObject.transform.GetComponentsInChildren<IIDExtention>(true).ToList();
        extentions.Reverse();

        foreach (var item in extentions)
        {
            if (item.GetID() == null || item.GetID() == "")
            {
                GenerateID(item);
            }
            else
            {
                var list = new List<IIDExtention>(extentions);
                list.Remove(item);
                var v = list.Where(a => a.GetID() != "0" && a.GetID() == item.GetID());
                if (v.Any()) GenerateID(item);
            }
        }
    }

    private void GenerateID(IIDExtention item)
    {
        var id = GUID.Generate().ToString();
        item.SetID(id);
        Debug.Log("Applyed ID: " + item.GetGameObject().name + " " + id);
        PrefabUtility.RecordPrefabInstancePropertyModifications(item.GetObject());
        EditorUtility.SetDirty(item.GetGameObject());
    }
}
