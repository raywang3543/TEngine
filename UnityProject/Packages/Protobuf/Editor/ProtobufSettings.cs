using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Google.Protobuf.Editor
{
    // 1. 创建存储设置的ScriptableObject
    public class ProtobufSettings : ScriptableObject
    {
        public const string k_SettingsPath = "Assets/ThirdPart/Protobuf/Editor/ProtobufSettings.asset";
        
        [Header("路径设置")]
        public string sourcePath = "/Users/ray/projects/csharp/proto ";
        public List<string> ignoreList = new List<string>(){"services", "resources"};
        public string destinationPath = "/Users/ray/projects/Unity/UnityProject/Assets/HotUpdate/FrameWork/Protobuf";
        
        public static ProtobufSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ProtobufSettings>(k_SettingsPath);
            if (settings == null)
            {
                settings = CreateInstance<ProtobufSettings>();
                
                // 确保目录存在
                string directory = Path.GetDirectoryName(k_SettingsPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                
                AssetDatabase.CreateAsset(settings, k_SettingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
        
        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
    }
    
    // 2. 创建SettingsProvider
    static class ProtobufSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateProtobufSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Protobuf Settings", SettingsScope.Project)
            {
                label = "Protobuf 设置",
                
                // GUI绘制逻辑
                guiHandler = (searchContext) =>
                {
                    var settings = ProtobufSettings.GetSerializedSettings();
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("路径设置", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(settings.FindProperty("sourcePath"), new GUIContent("源路径"));
                    SerializedProperty ignoreListProp = settings.FindProperty("ignoreList");
                    EditorGUILayout.PropertyField(ignoreListProp, new GUIContent("忽略列表"), false);
                    
                    EditorGUI.indentLevel++;
                    // Add button
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("添加忽略项", GUILayout.Width(120)))
                    {
                        ignoreListProp.arraySize++;
                        ignoreListProp.GetArrayElementAtIndex(ignoreListProp.arraySize - 1).stringValue = "";
                        settings.ApplyModifiedProperties(); // Apply immediately after modification
                    }
                    EditorGUILayout.EndHorizontal();

                    // List items
                    EditorGUILayout.Space();
                    int deleteIndex = -1; // Track which item to delete

                    for (int i = 0; i < ignoreListProp.arraySize; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        SerializedProperty element = ignoreListProp.GetArrayElementAtIndex(i);
                        EditorGUI.BeginChangeCheck();
                        string newValue = EditorGUILayout.TextField(element.stringValue);
                        if (EditorGUI.EndChangeCheck())
                        {
                            element.stringValue = newValue;
                            settings.ApplyModifiedProperties(); // Apply when text changes
                        }

                        if (GUILayout.Button("删除", GUILayout.Width(60)))
                        {
                            deleteIndex = i; // Mark for deletion
                        }
                        EditorGUILayout.EndHorizontal();
                    }

                    // Handle deletion outside the loop
                    if (deleteIndex >= 0)
                    {
                        ignoreListProp.DeleteArrayElementAtIndex(deleteIndex);
                        settings.ApplyModifiedProperties(); // Apply after deletion
                    }

                    EditorGUI.indentLevel--;
                    
                    EditorGUILayout.PropertyField(settings.FindProperty("destinationPath"), new GUIContent("目标路径"));
                    
                    EditorGUILayout.Space();
                   
                    settings.ApplyModifiedProperties();
                },
                
                // 关键词匹配，用于设置搜索
                keywords = new HashSet<string>(new[] { "Protobuf", "Proto"})
            };
            
            return provider;
        }
    }
}