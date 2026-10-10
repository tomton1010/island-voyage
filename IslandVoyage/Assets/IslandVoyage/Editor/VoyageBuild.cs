using System;
using System.IO;
using IslandVoyage;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class VoyageBuild
{
    const string ScenePath="Assets/Scenes/IslandVoyage.unity";
    [InitializeOnLoadMethod]
    static void AutoSetup(){EditorApplication.delayCall+=()=>{if(!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)Setup();};}

    [MenuItem("Tools/Island Voyage/Prepare Project")]
    public static void Setup()
    {
        Directory.CreateDirectory("Assets/Settings");Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Builds");AssetDatabase.Refresh();
        const string rendererPath="Assets/Settings/OceanRenderer.asset",pipelinePath="Assets/Settings/OceanPipeline.asset";
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if(pipeline==null)
        {
            var renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,rendererPath);
            pipeline=UniversalRenderPipelineAsset.Create(renderer);pipeline.name="Ocean URP";pipeline.msaaSampleCount=4;pipeline.shadowDistance=85;pipeline.shadowCascadeCount=2;pipeline.mainLightShadowmapResolution=2048;
            var serialized=new SerializedObject(pipeline);serialized.FindProperty("m_MainLightShadowsSupported").boolValue=true;serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(pipeline,pipelinePath);
        }
        var lighting=new SerializedObject(pipeline);lighting.FindProperty("m_SoftShadowsSupported").boolValue=true;lighting.ApplyModifiedPropertiesWithoutUndo();
        pipeline.supportsHDR=true;
        var sceneRenderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        sceneRenderer.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
        if(sceneRenderer.postProcessData==null || sceneRenderer.postProcessData.shaders.uberPostPS==null || sceneRenderer.postProcessData.shaders.bloomPS==null)
            throw new InvalidOperationException("URP post-processing shader resources are missing.");
        sceneRenderer.SetDirty();EditorUtility.SetDirty(sceneRenderer);
        if(!sceneRenderer.rendererFeatures.Exists(f=>f is ScreenSpaceAmbientOcclusion))
        {
            var ao=ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();ao.name="Coastal contact shading";
            var settings=new SerializedObject(ao);var options=settings.FindProperty("m_Settings");
            options.FindPropertyRelative("Source").enumValueIndex=0;options.FindPropertyRelative("AfterOpaque").boolValue=true;
            options.FindPropertyRelative("Downsample").boolValue=true;options.FindPropertyRelative("Intensity").floatValue=1.1f;
            options.FindPropertyRelative("Radius").floatValue=.35f;settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.AddObjectToAsset(ao,sceneRenderer);sceneRenderer.rendererFeatures.Add(ao);sceneRenderer.SetDirty();EditorUtility.SetDirty(sceneRenderer);
        }
        const string lookPath="Assets/Resources/Blender/LookProfile.asset";
        var look=AssetDatabase.LoadAssetAtPath<VolumeProfile>(lookPath);
        if(look==null)
        {
            look=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(look,lookPath);
            var tone=look.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
            var bloom=look.Add<Bloom>(true);bloom.threshold.Override(1.1f);bloom.intensity.Override(.12f);bloom.scatter.Override(.55f);
            var grade=look.Add<ColorAdjustments>(true);grade.contrast.Override(6);grade.saturation.Override(-4);grade.postExposure.Override(0);
            var vignette=look.Add<Vignette>(true);vignette.intensity.Override(.10f);vignette.smoothness.Override(.55f);
            foreach(var component in look.components)AssetDatabase.AddObjectToAsset(component,look);
        }
        GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
        // The bootstrap enables linear fog at runtime; keep it in player builds.
        var graphics=new SerializedObject(GraphicsSettings.GetGraphicsSettings());
        graphics.FindProperty("m_FogStripping").intValue=1;
        graphics.FindProperty("m_FogKeepLinear").boolValue=true;
        graphics.FindProperty("m_FogKeepExp").boolValue=false;
        graphics.FindProperty("m_FogKeepExp2").boolValue=false;
        graphics.ApplyModifiedPropertiesWithoutUndo();
        PlayerSettings.companyName="Island Voyage";PlayerSettings.productName="潮汐群岛";PlayerSettings.bundleVersion="0.5.0";
        PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;PlayerSettings.WebGL.decompressionFallback=false;
        PlayerSettings.WebGL.template="PROJECT:IslandVoyage";
        QualitySettings.vSyncCount=0;
        if(!File.Exists(ScenePath))
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Island Voyage · start here",typeof(GameBootstrap));EditorSceneManager.SaveScene(scene,ScenePath);
        }
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        AssetDatabase.SaveAssets();Debug.Log("VOYAGE_SETUP_COMPLETE: Unity "+Application.unityVersion);
    }
    [MenuItem("Tools/Island Voyage/Validate Fishing Rules")]
    public static void ValidateRules()
    {
        void Check(bool value,string label){if(!value)throw new InvalidOperationException("Rule validation failed: "+label);}
        Check(VoyageRules.IsThreat(4,8),"Fast threshold and warning boundary");Check(!VoyageRules.IsThreat(3.99f,8),"Medium speed does not scare");Check(!VoyageRules.IsThreat(6,8.01f),"Outside warning radius");
        Check(VoyageRules.CanFish(2,12,SchoolMood.Calm,1,0),"Slow angling boundary");Check(!VoyageRules.CanFish(2.01f,12,SchoolMood.Calm,1,0),"Cannot cast while too fast");Check(!VoyageRules.CanFish(0,12.01f,SchoolMood.Calm,1,0),"Cannot cast out of range");
        Check(!VoyageRules.CanFish(0,1,SchoolMood.Fleeing,12,0),"Frightened fish cannot be caught");Check(!VoyageRules.CanFish(0,1,SchoolMood.Returning,12,0),"Recovering fish cannot be caught");Check(!VoyageRules.CanFish(0,1,SchoolMood.Calm,0,0),"Empty school");Check(!VoyageRules.CanFish(0,1,SchoolMood.Calm,1,.1f),"Cooldown");
        var results=new System.Collections.Generic.List<string>();
        for(int species=0;species<8;species++)
        {
            foreach(float frame in new[]{.02f,.1f})
            {
                var fight=new FishingFight(species,12,1.3f);bool reel=true;
                for(int i=0;i<Mathf.CeilToInt(65/frame) && fight.Outcome==FightOutcome.Playing;i++)
                {if(fight.Tension>.63f)reel=false;else if(fight.Tension<.33f)reel=true;fight.Advance(frame,reel,2.8f);}
                Check(fight.Outcome==FightOutcome.Landable,"Skillful tension control lands species "+species+" dt="+frame);
                results.Add($"Species {species}: controlled reel/release landed in {fight.Elapsed:0.0}s at dt {frame}");
            }
            var held=new FishingFight(species,12,1.3f);var loose=new FishingFight(species,12,1.3f);
            for(int i=0;i<600;i++){held.Advance(.02f,true,1);loose.Advance(.02f,false,1);}
            Check(held.Outcome==FightOutcome.LineBroken,"Holding reel continuously breaks the line");
            Check(loose.Outcome==FightOutcome.HookLost,"Leaving slack loses the fish");
        }
        Debug.Log("VOYAGE_RULES_PASSED: 8 species, controlled reel/release wins at 50 and 10 fps; constant reel breaks line, slack loses hook; speed/range/mood/cooldown verified");
        File.WriteAllText("Builds/RuleValidation.txt",$"Unity {Application.unityVersion}\nSkill-based angling replaces fixed catch probability.\nBoundary checks: passed\n"+string.Join("\n",results));
    }
    [MenuItem("Tools/Island Voyage/Build Windows")]
    public static void Windows()
    {
        Setup();ValidateRules();BlenderArtPipeline.ValidateAll();Directory.CreateDirectory("Builds/Windows");
        Build(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Windows/IslandVoyage.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
    }
    [MenuItem("Tools/Island Voyage/Build Web")]
    public static void Web()
    {
        Setup();ValidateRules();BlenderArtPipeline.ValidateAll();Directory.CreateDirectory("Builds/Web");
        Build(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Web",target=BuildTarget.WebGL,options=BuildOptions.None});
    }
    static void Build(BuildPlayerOptions options)
    {
        var report=BuildPipeline.BuildPlayer(options);
        if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Build failed: "+report.summary.result);
        foreach(string guid in AssetDatabase.FindAssets("t:Shader",new[]{"Assets/Resources"}))
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(guid));
            foreach(var message in ShaderUtil.GetShaderMessages(shader))
                if(message.severity.ToString()=="Error")throw new InvalidOperationException("Shader compilation failed: "+shader.name+" · "+message.message);
        }
        Debug.Log("VOYAGE_BUILD_PASSED: "+report.summary.platform+" "+report.summary.totalSize+" bytes");
    }
}
