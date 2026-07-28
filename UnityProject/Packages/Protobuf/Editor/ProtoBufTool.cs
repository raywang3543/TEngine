using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf.Editor;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class ProtoBufTool
    {
        [MenuItem("Tools/ProtoBuf/复制Protobuf文件到项目")]
        public static void Run3()
        {
            var settings = ProtobufSettings.GetOrCreateSettings();

            if (!CopyProtobufFiles(settings)) return;

            UnityEngine.Debug.Log("<color=green>ProtoBuf流程执行完成！</color>");
        }

        private static bool CopyProtobufFiles(ProtobufSettings settings)
        {
            if (!CopyFilesToUnityProject(settings.sourcePath, settings.destinationPath, settings.ignoreList))
                return ShowError("Copy Error", "Failed to copy Protobuf files to Unity project");

            return true;
        }

        private static bool ShowError(string title, string message)
        {
            EditorUtility.DisplayDialog(title, message, "OK");
            return false;
        }

        private static bool CopyFilesToUnityProject(string sourcePath, string destinationPath, List<string> ignoreList)
        {
            try
            {
                // Ensure destination directory exists
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }

                // Get all files from source directory
                if (!Directory.Exists(sourcePath))
                {
                    UnityEngine.Debug.LogError($"<color=red>Source directory not found: {sourcePath}</color>");
                    return false;
                }

                //将destinationPath路径中的除google文件夹的所有文件删除
                if (Directory.Exists(destinationPath))
                {

                    // Get the full path to the google directory
                    string googleDir = Path.Combine(destinationPath, "google");

                    // Delete all directories except google
                    foreach (string dir in Directory.GetDirectories(destinationPath, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (!dir.Equals(googleDir, StringComparison.OrdinalIgnoreCase))
                        {
                            Directory.Delete(dir, true); // recursive delete
                        }
                    }

                    // Delete all files in the root directory
                    foreach (string file in Directory.GetFiles(destinationPath, "*.*", SearchOption.TopDirectoryOnly))
                    {
                        File.Delete(file);
                    }
                }

                string[] sourceFiles = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories);
                int copiedFiles = 0;

                foreach (string sourceFile in sourceFiles)
                {
                    // Get relative path to maintain directory structure
                    string relativePath = sourceFile.Substring(sourcePath.Length + 1);
                    //去除拓展名
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(relativePath);
                    bool shouldIgnore = ignoreList.Contains(fileNameWithoutExtension) || 
                                        ignoreList.Any(pattern => pattern.StartsWith("*") && 
                                                                  fileNameWithoutExtension.Contains(pattern.Substring(1)));
                    if (shouldIgnore)
                    {
                        continue;
                    }

                    string destinationFile = Path.Combine(destinationPath, relativePath);
                    string destinationDir = Path.GetDirectoryName(destinationFile);

                    // Create destination subdirectories if needed
                    if (!Directory.Exists(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    // Copy the file
                    File.Copy(sourceFile, destinationFile, true);
                    copiedFiles++;
                }

                UnityEngine.Debug.Log($"<color=green>Successfully copied {copiedFiles} files to Unity project</color>");
                AssetDatabase.Refresh(); // Refresh AssetDatabase to see the new files in Unity
                return true;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"<color=red>Failed to copy files: {ex.Message}</color>");
                return false;
            }
        }
    }
}
