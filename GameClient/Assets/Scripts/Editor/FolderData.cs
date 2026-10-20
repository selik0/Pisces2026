using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameClientEditor
{
    [CreateAssetMenu(fileName = "FolderData", menuName = "ScriptableObjects/FolderData")]
    public class FolderData : ScriptableObject
    {
        public DefaultAsset folderName;
    }
}
