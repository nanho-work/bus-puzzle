#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class IosLaunchScreenPostprocessor
{
    private const string SourceImagePath = "Assets/BusPuzzle/Resources/UI/Boosters/main.png";
    private const string SourceIconPath = "Assets/BusPuzzle/Resources/UI/Boosters/Bus_Pop(en)_icon.png";

    [PostProcessBuild(1000)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
        {
            return;
        }

        CopyLaunchImage(pathToBuiltProject, "LaunchScreen-iPhonePortrait.png");
        CopyLaunchImage(pathToBuiltProject, "LaunchScreen-iPhoneLandscape.png");
        CopyLaunchImage(pathToBuiltProject, "LaunchScreen-iPad.png");
        PatchInfoPlist(pathToBuiltProject);
        PatchXcodeProject(pathToBuiltProject);
        PatchUnityAppController(pathToBuiltProject);
        WriteAppIcons(pathToBuiltProject);
    }

    private static void CopyLaunchImage(string buildPath, string destinationName)
    {
        string source = Path.Combine(Directory.GetCurrentDirectory(), SourceImagePath);
        if (!File.Exists(source))
        {
            Debug.LogWarning($"iOS launch screen source image was not found: {source}");
            return;
        }

        string destination = Path.Combine(buildPath, destinationName);
        File.Copy(source, destination, true);
    }

    private static void PatchInfoPlist(string buildPath)
    {
        string plistPath = Path.Combine(buildPath, "Info.plist");
        if (!File.Exists(plistPath))
        {
            Debug.LogWarning($"iOS Info.plist was not found: {plistPath}");
            return;
        }

        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict root = plist.root;
        PlistElementArray deviceFamilies = root.CreateArray("UIDeviceFamily");
        deviceFamilies.AddInteger(1);

        root.SetString("CFBundleShortVersionString", PlayerSettings.bundleVersion);
        root.SetString("CFBundleVersion", PlayerSettings.iOS.buildNumber);
        root.SetString("UILaunchStoryboardName", "LaunchScreen-iPhone");
        root.SetString("UILaunchStoryboardName~iphone", "LaunchScreen-iPhone");
        root.SetString("UILaunchStoryboardName~ipod", "LaunchScreen-iPhone");
        root.SetString("UILaunchStoryboardName~ipad", "LaunchScreen-iPad");
        root.SetString("NSUserTrackingUsageDescription", "Bus Pop uses this permission to show relevant ads and measure ad performance.");
        root.SetBoolean("UIRequiresFullScreen", true);

        SetPortraitOrientations(root, "UISupportedInterfaceOrientations");
        SetPortraitOrientations(root, "UISupportedInterfaceOrientations~iphone");
        SetPortraitOrientations(root, "UISupportedInterfaceOrientations~ipad");

        plist.WriteToFile(plistPath);
        Debug.Log("iOS launch screen images and portrait orientation were patched for Bus Pop.");
    }

    private static void SetPortraitOrientations(PlistElementDict root, string key)
    {
        PlistElementArray orientations = root.CreateArray(key);
        orientations.AddString("UIInterfaceOrientationPortrait");
    }

    private static void PatchXcodeProject(string buildPath)
    {
        string projectPath = PBXProject.GetPBXProjectPath(buildPath);
        if (!File.Exists(projectPath))
        {
            Debug.LogWarning($"iOS Xcode project was not found: {projectPath}");
            return;
        }

        var project = new PBXProject();
        project.ReadFromFile(projectPath);

        string mainTargetGuid = project.GetUnityMainTargetGuid();
        string frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();
        project.SetBuildProperty(mainTargetGuid, "TARGETED_DEVICE_FAMILY", "1");
        project.SetBuildProperty(mainTargetGuid, "MARKETING_VERSION", PlayerSettings.bundleVersion);
        project.SetBuildProperty(mainTargetGuid, "CURRENT_PROJECT_VERSION", PlayerSettings.iOS.buildNumber);
        project.SetBuildProperty(frameworkTargetGuid, "CURRENT_PROJECT_VERSION", PlayerSettings.iOS.buildNumber);
        project.AddFrameworkToProject(frameworkTargetGuid, "AppTrackingTransparency.framework", false);

        project.WriteToFile(projectPath);
        Debug.Log("iOS target device family was patched to iPhone only for Bus Pop.");
    }

    private static void PatchUnityAppController(string buildPath)
    {
        string appControllerPath = Path.Combine(buildPath, "Classes", "UnityAppController.mm");
        if (!File.Exists(appControllerPath))
        {
            Debug.LogWarning($"iOS UnityAppController.mm was not found: {appControllerPath}");
            return;
        }

        string contents = File.ReadAllText(appControllerPath);
        string rootlessAllOrientations = @"    // No rootViewController is set because we are switching from one view controller to another, all orientations should be enabled
    if ([window rootViewController] == nil)
        return UIInterfaceOrientationMaskAll;";
        string rootlessPortraitOnly = @"    // Bus Pop is portrait-only; keep the native launch window from briefly accepting reverse portrait.
    if ([window rootViewController] == nil)
        return UIInterfaceOrientationMaskPortrait;";
        string splashControllerReturn = @"    if (self.engineLoadState < kUnityEngineLoadStateAppReady)
        return [_rootController supportedInterfaceOrientations];";
        string splashPortraitOnlyReturn = @"    if (self.engineLoadState < kUnityEngineLoadStateAppReady)
        return UIInterfaceOrientationMaskPortrait;";
        string forcedMaskReturn = "    return [[window rootViewController] supportedInterfaceOrientations] | _forceInterfaceOrientationMask;";
        string portraitOnlyReturn = "    return UIInterfaceOrientationMaskPortrait;";

        bool changed = false;
        if (contents.Contains(rootlessAllOrientations))
        {
            contents = contents.Replace(rootlessAllOrientations, rootlessPortraitOnly);
            changed = true;
        }
        else if (!contents.Contains(rootlessPortraitOnly))
        {
            Debug.LogWarning("iOS UnityAppController portrait startup patch was skipped because the expected orientation block was not found.");
            return;
        }

        if (contents.Contains(splashControllerReturn))
        {
            contents = contents.Replace(splashControllerReturn, splashPortraitOnlyReturn);
            changed = true;
        }
        else if (!contents.Contains(splashPortraitOnlyReturn))
        {
            Debug.LogWarning("iOS UnityAppController splash portrait patch was skipped because the expected orientation return was not found.");
            return;
        }

        if (contents.Contains(forcedMaskReturn))
        {
            contents = contents.Replace(forcedMaskReturn, portraitOnlyReturn);
            changed = true;
        }
        else if (!contents.Contains(portraitOnlyReturn))
        {
            Debug.LogWarning("iOS UnityAppController portrait return patch was skipped because the expected orientation return was not found.");
            return;
        }

        if (changed)
        {
            File.WriteAllText(appControllerPath, contents);
            Debug.Log("iOS UnityAppController was patched to keep startup orientation portrait-only.");
        }
        else
        {
            Debug.Log("iOS UnityAppController already keeps startup orientation portrait-only.");
        }
    }

    private static void WriteAppIcons(string buildPath)
    {
        string source = Path.Combine(Directory.GetCurrentDirectory(), SourceIconPath);
        if (!File.Exists(source))
        {
            Debug.LogWarning($"iOS app icon source image was not found: {source}");
            return;
        }

        string appIconPath = Path.Combine(buildPath, "Unity-iPhone/Images.xcassets/AppIcon.appiconset");
        if (!Directory.Exists(appIconPath))
        {
            Debug.LogWarning($"iOS AppIcon asset catalog was not found: {appIconPath}");
            return;
        }

        byte[] bytes = File.ReadAllBytes(source);
        var sourceTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!sourceTexture.LoadImage(bytes))
        {
            Debug.LogWarning($"iOS app icon source image could not be loaded: {source}");
            Object.DestroyImmediate(sourceTexture);
            return;
        }

        WriteIcon(sourceTexture, appIconPath, "Icon-iPhone-120.png", 120);
        WriteIcon(sourceTexture, appIconPath, "Icon-iPhone-180.png", 180);
        WriteIcon(sourceTexture, appIconPath, "Icon-iPad-76.png", 76);
        WriteIcon(sourceTexture, appIconPath, "Icon-iPad-152.png", 152);
        WriteIcon(sourceTexture, appIconPath, "Icon-iPad-167.png", 167);
        WriteIcon(sourceTexture, appIconPath, "Icon-AppStore-1024.png", 1024);
        WriteAppIconContentsJson(appIconPath);
        Object.DestroyImmediate(sourceTexture);
    }

    private static void WriteIcon(Texture2D sourceTexture, string appIconPath, string fileName, int size)
    {
        var resizedTexture = ResizeToOpaqueTexture(sourceTexture, size);
        string destination = Path.Combine(appIconPath, fileName);
        File.WriteAllBytes(destination, resizedTexture.EncodeToPNG());
        Object.DestroyImmediate(resizedTexture);
    }

    private static Texture2D ResizeToOpaqueTexture(Texture2D sourceTexture, int size)
    {
        var result = new Texture2D(size, size, TextureFormat.RGB24, false)
        {
            name = $"Bus Pop iOS Icon {size}"
        };

        var pixels = new Color32[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var sourceX = (x + 0.5f) / size * sourceTexture.width;
                var sourceY = (y + 0.5f) / size * sourceTexture.height;
                var color = sourceTexture.GetPixelBilinear(sourceX / sourceTexture.width, sourceY / sourceTexture.height);
                color = Color.Lerp(Color.white, color, color.a);
                color.a = 1f;
                pixels[y * size + x] = color;
            }
        }

        result.SetPixels32(pixels);
        result.Apply(false, false);
        return result;
    }

    private static void WriteAppIconContentsJson(string appIconPath)
    {
        const string contentsJson = @"{
  ""images"" : [
    {
      ""filename"" : ""Icon-iPhone-120.png"",
      ""idiom"" : ""iphone"",
      ""scale"" : ""2x"",
      ""size"" : ""60x60""
    },
    {
      ""filename"" : ""Icon-iPhone-180.png"",
      ""idiom"" : ""iphone"",
      ""scale"" : ""3x"",
      ""size"" : ""60x60""
    },
    {
      ""filename"" : ""Icon-iPad-76.png"",
      ""idiom"" : ""ipad"",
      ""scale"" : ""1x"",
      ""size"" : ""76x76""
    },
    {
      ""filename"" : ""Icon-iPad-152.png"",
      ""idiom"" : ""ipad"",
      ""scale"" : ""2x"",
      ""size"" : ""76x76""
    },
    {
      ""filename"" : ""Icon-iPad-167.png"",
      ""idiom"" : ""ipad"",
      ""scale"" : ""2x"",
      ""size"" : ""83.5x83.5""
    },
    {
      ""filename"" : ""Icon-AppStore-1024.png"",
      ""idiom"" : ""ios-marketing"",
      ""scale"" : ""1x"",
      ""size"" : ""1024x1024""
    }
  ],
  ""info"" : {
    ""author"" : ""xcode"",
    ""version"" : 1
  },
  ""properties"" : {
    ""pre-rendered"" : false
  }
}
";

        File.WriteAllText(Path.Combine(appIconPath, "Contents.json"), contentsJson);
    }
}
#endif
