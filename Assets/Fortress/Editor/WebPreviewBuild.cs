using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace Fortress.Editor
{
    public static class WebPreviewBuild
    {
        public const string Output="Builds/ProjectF-Web-preview";
        public static void Build()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new InvalidOperationException("Web build module is missing.");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.WebGL)throw new InvalidOperationException("Launch Unity with -buildTarget WebGL first.");
            Directory.CreateDirectory("Logs");
            var template=PlayerSettings.WebGL.template;var compression=PlayerSettings.WebGL.compressionFormat;
            bool fallback=PlayerSettings.WebGL.decompressionFallback,hashes=PlayerSettings.WebGL.nameFilesAsHashes,close=PlayerSettings.WebGL.closeOnQuit;
            var symbols=PlayerSettings.WebGL.debugSymbolMode;bool diagnostics=PlayerSettings.WebGL.showDiagnostics;
            int width=PlayerSettings.defaultWebScreenWidth,height=PlayerSettings.defaultWebScreenHeight;
            bool pcIncluded=QualitySettings.IsPlatformIncluded("WebGL",1);
            try
            {
                PlayerSettings.WebGL.template="PROJECT:Fortress";
                PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback=true;PlayerSettings.WebGL.nameFilesAsHashes=true;
                PlayerSettings.WebGL.closeOnQuit=false;PlayerSettings.WebGL.debugSymbolMode=WebGLDebugSymbolMode.Off;
                PlayerSettings.WebGL.showDiagnostics=false;
                PlayerSettings.defaultWebScreenWidth=1280;PlayerSettings.defaultWebScreenHeight=720;
                if(pcIncluded && !QualitySettings.TryExcludePlatformAt("WebGL",1,out var qualityError))throw qualityError;
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=new[]{SessionMenu.MainScene,SessionMenu.AiScene,SessionMenu.LocalScene},
                    locationPathName=Output,target=BuildTarget.WebGL,options=BuildOptions.StrictMode });
                var s=report.summary;
                File.WriteAllText("Logs/web-build-result.json",JsonUtility.ToJson(new Result {
                    result=s.result.ToString(),errors=s.totalErrors,warnings=s.totalWarnings,bytes=s.totalSize,seconds=s.totalTime.TotalSeconds },true));
                if(s.result!=BuildResult.Succeeded)throw new InvalidOperationException("Web build failed: "+s.result);
            }
            finally
            {
                PlayerSettings.WebGL.template=template;PlayerSettings.WebGL.compressionFormat=compression;
                PlayerSettings.WebGL.decompressionFallback=fallback;PlayerSettings.WebGL.nameFilesAsHashes=hashes;
                PlayerSettings.WebGL.closeOnQuit=close;PlayerSettings.WebGL.debugSymbolMode=symbols;
                PlayerSettings.WebGL.showDiagnostics=diagnostics;
                PlayerSettings.defaultWebScreenWidth=width;PlayerSettings.defaultWebScreenHeight=height;
                if(pcIncluded && !QualitySettings.TryIncludePlatformAt("WebGL",1,out var qualityError))Debug.LogError(qualityError);
                AssetDatabase.SaveAssets();
            }
        }
        [Serializable] sealed class Result { public string result;public int errors,warnings;public ulong bytes;public double seconds; }
    }
}
