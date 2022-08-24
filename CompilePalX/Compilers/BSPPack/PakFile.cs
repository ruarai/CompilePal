using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CompilePalX.Compilers.UtilityProcess;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers.BSPPack
{
    class PakFile
    {

        private static readonly string invalidChars = Regex.Escape(new string(Path.GetInvalidPathChars()));
        private static readonly string invalidRegString = $@"([{invalidChars}]*\.+$)|([{invalidChars}]+)";
        private readonly List<string> excludedDirs;

        private readonly List<string> excludedFiles;
        private readonly List<string> excludedVpkFiles;
        private readonly string fileName;
        // This class is the class responsible for building the list of files to include
        // The list can be saved to a text file for use with bspzip

        // the dictionary is formated as <internalPath, externalPath>
        // matching the bspzip specification https://developer.valvesoftware.com/wiki/BSPZIP
        private readonly IDictionary<string, string> Files;

        private readonly List<string> sourceDirs;

        public PakFile(BSP bsp, List<string> sourceDirectories, List<string> includeFiles, List<string> excludedFiles, List<string> excludedDirs, List<string> excludedVpkFiles, string outputFile)
        {
            mdlcount = vmtcount = pcfcount = sndcount = vehiclescriptcount = effectscriptcount = panoramaMapIconCount = 0;
            sourceDirs = sourceDirectories;
            fileName = outputFile;

            this.excludedFiles = excludedFiles;
            this.excludedDirs = excludedDirs;
            this.excludedVpkFiles = excludedVpkFiles;

            Files = new Dictionary<string, string>();

            if (bsp.nav.Key != default(string))
            {
                AddFile(bsp.nav, b => b.nav = default(KeyValuePair<string, string>), bsp);
            }

            if (bsp.detail.Key != default(string))
            {
                AddFile(bsp.detail, b => b.detail = default(KeyValuePair<string, string>), bsp);
            }

            if (bsp.kv.Key != default(string))
            {
                AddFile(bsp.kv, b => b.kv = default(KeyValuePair<string, string>), bsp);
            }

            if (bsp.txt.Key != default(string))
            {
                AddFile(bsp.txt, b => b.txt = default(KeyValuePair<string, string>), bsp);
            }

            if (bsp.jpg.Key != default(string))
            {
                AddFile(bsp.jpg, b => b.jpg = default(KeyValuePair<string, string>), bsp);
            }

            if (bsp.radartxt.Key != default(string))
            {
                AddFile(bsp.radartxt, b => b.radartxt = default(KeyValuePair<string, string>), bsp);
            }

            if (bsp.res.Key != default(string))
            {
                if (AddFile(bsp.res, b => b.res = default(KeyValuePair<string, string>), bsp))
                {
                    foreach (var material in AssetUtils.findResMaterials(bsp.res.Value))
                    {
                        AddTexture(material);
                    }
                }
            }

            if (bsp.particleManifest.Key != default(string))
            {
                if (AddFile(bsp.particleManifest, b => b.particleManifest = default(KeyValuePair<string, string>), bsp))
                {
                    foreach (var particle in AssetUtils.findManifestPcfs(bsp.particleManifest.Value))
                    {
                        AddParticle(particle);
                    }
                }
            }

            if (bsp.soundscape.Key != default(string))
            {
                if (AddFile(bsp.soundscape, b => b.soundscape = default(KeyValuePair<string, string>), bsp))
                {
                    foreach (var sound in AssetUtils.findSoundscapeSounds(bsp.soundscape.Value))
                    {
                        if (AddInternalFile(sound, FindExternalFile(sound)))
                        {
                            sndcount++;
                        }
                    }
                }
            }

            if (bsp.soundscript.Key != default(string))
            {
                if (AddFile(bsp.soundscript, b => b.soundscript = default(KeyValuePair<string, string>), bsp))
                {
                    foreach (var sound in AssetUtils.findSoundscapeSounds(bsp.soundscript.Value))
                    {
                        if (AddInternalFile(sound, FindExternalFile(sound)))
                        {
                            sndcount++;
                        }
                    }
                }
            }

            // find color correction files
            foreach (var cc in bsp.entityList.Where(item => item["classname"] == "color_correction"))
            {
                if (cc.ContainsKey("filename"))
                {
                    AddInternalFile(cc["filename"], FindExternalFile(cc["filename"]));
                }
            }

            foreach (var vehicleScript in bsp.VehicleScriptList)
            {
                if (AddInternalFile(vehicleScript.Key, vehicleScript.Value))
                {
                    vehiclescriptcount++;
                }
            }
            foreach (var effectScript in bsp.EffectScriptList)
            {
                if (AddInternalFile(effectScript.Key, effectScript.Value))
                {
                    effectscriptcount++;
                }
            }
            foreach (var dds in bsp.radardds)
            {
                AddInternalFile(dds.Key, dds.Value);
            }
            foreach (var lang in bsp.languages)
            {
                AddInternalFile(lang.Key, lang.Value);
            }
            foreach (var model in bsp.EntModelList)
            {
                AddModel(model);
            }
            for (var i = 0; i < bsp.ModelList.Count; i++)
            {
                AddModel(bsp.ModelList[i], bsp.modelSkinList[i]);
            }
            foreach (var vmt in bsp.TextureList)
            {
                AddTexture(vmt);
            }
            foreach (var vmt in bsp.EntTextureList)
            {
                AddTexture(vmt);
            }
            foreach (var sound in bsp.EntSoundList)
            {
                if (AddInternalFile(sound, FindExternalFile(sound)))
                {
                    sndcount++;
                }
            }
            foreach (var vscript in bsp.vscriptList)
            {
                if (AddInternalFile(vscript, FindExternalFile(vscript)))
                {
                    vscriptcount++;
                }
            }
            foreach (var teamSelectionBackground in bsp.PanoramaMapIcons)
            {
                if (AddInternalFile(teamSelectionBackground.Key, teamSelectionBackground.Value))
                {
                    panoramaMapIconCount++;
                }
            }

            // add all manually included files
            // TODO right now the manually included files search for files it depends on. Not sure if this should be default behavior
            foreach (var file in includeFiles)
            {
                // try to get the source directory the file is located in
                var fileInfo = new FileInfo(file);

                // default base directory is the game folder
                var baseDir = GameConfigurationManager.GameConfiguration.GameFolder;

                var potentialSubDir = new List<string>(sourceDirs); // clone to prevent accidental modification
                potentialSubDir.Remove(baseDir);
                foreach (var folder in potentialSubDir)
                {
                    if (fileInfo.Directory != null
                        && fileInfo.Directory.FullName.ToLower().Contains(folder.ToLower()))
                    {
                        baseDir = folder;
                        break;
                    }
                }

                // check needed for when file does not exist in any sub directory or the base directory
                if (fileInfo.Directory != null && !fileInfo.Directory.FullName.ToLower().Contains(baseDir.ToLower()))
                {
                    CompilePalLogger.LogCompileError($"Failed to resolve internal path for {file}, skipping\n", new Error($"Failed to resolve internal path for {file}, skipping", ErrorSeverity.Error));
                    continue;
                }

                var internalPath = Regex.Replace(file, Regex.Escape(baseDir + "\\"), "", RegexOptions.IgnoreCase);

                // try to determine file type by extension
                switch (file.Split('.').Last())
                {
                    case "vmt":
                        AddTexture(internalPath);
                        break;
                    case "pcf":
                        AddParticle(internalPath);
                        break;
                    case "mdl":
                        AddModel(internalPath);
                        break;
                    case "wav":
                    case "mp3":
                        AddInternalFile(internalPath, file);
                        sndcount++;
                        break;
                    case "res":
                        AddInternalFile(internalPath, file);
                        foreach (var material in AssetUtils.findResMaterials(file))
                        {
                            AddTexture(material);
                        }
                        break;
                    default:
                        AddInternalFile(internalPath, file);
                        break;
                }
            }
        }

        public int mdlcount { get; private set; }
        public int vmtcount { get; private set; }
        public int pcfcount { get; private set; }
        public int sndcount { get; }
        public int vehiclescriptcount { get; }
        public int effectscriptcount { get; }
        public int vscriptcount { get; }
        public int panoramaMapIconCount { get; }

        private bool AddFile(string internalPath, string externalPath)
        {
            return AddFile(new KeyValuePair<string, string>(internalPath, externalPath));
        }
        // onFailure is for utility files such as nav, radar, etc which get excluded. if they are excluded, the Delegate is run. This is used for removing the files from the BSP class, so they dont appear in the summary at the end
        private bool AddFile(KeyValuePair<string, string> paths, Action<BSP> onExcluded = null, BSP bsp = null)
        {
            var externalPath = paths.Value;

            // exclude files that are excluded
            if (externalPath != "" && File.Exists(externalPath)
                                   && !excludedFiles.Contains(externalPath.ToLower().Replace('/', '\\'))
                                   && !excludedDirs.Any(externalPath.ToLower().Replace('/', '\\').StartsWith)
                                   && !excludedVpkFiles.Contains(paths.Key.ToLower().Replace('\\', '/')))
            {
                Files.Add(paths);
                return true;
            }

            if (onExcluded != null && bsp != null)
            {
                onExcluded(bsp);
            }

            return false;
        }

        public void OutputToFile()
        {
            var outputLines = new List<string>();

            foreach (var entry in Files)
            {
                outputLines.Add(entry.Key);
                outputLines.Add(entry.Value);
            }

            if (!Directory.Exists("BSPZipFiles"))
            {
                Directory.CreateDirectory("BSPZipFiles");
            }

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            File.WriteAllLines(fileName, outputLines);
        }

        public Dictionary<string, string> GetResponseFile()
        {
            var output = new Dictionary<string, string>();

            foreach (var entry in Files)
            {
                output.Add(entry.Key, entry.Value.Replace(entry.Key, ""));
            }

            return output;
        }

        public bool AddInternalFile(string internalPath, string externalPath)
        {
            internalPath = internalPath.Replace("\\", "/");
            if (!Files.ContainsKey(internalPath))
            {
                return AddFile(internalPath, externalPath);
            }

            return false;
        }

        public void AddModel(string internalPath, List<int> skins = null)
        {
            // adds mdl files and finds its dependencies
            CompilePalLogger.LogLineDebug($"Packing model: {internalPath}");
            var externalPath = FindExternalFile(internalPath);
            CompilePalLogger.LogLineDebug($"External path: {internalPath}");
            if (AddInternalFile(internalPath, externalPath))
            {
                mdlcount++;
                var vtxMaterialNames = new List<string>();
                foreach (var reference in AssetUtils.findMdlRefs(internalPath))
                {
                    var ext_path = FindExternalFile(reference);
                    AddInternalFile(reference, FindExternalFile(reference));
                    if (reference.EndsWith(".phy"))
                    {
                        foreach (var gib in AssetUtils.findPhyGibs(ext_path))
                        {
                            AddModel(gib);
                        }
                    }
                    else if (reference.EndsWith(".vtx"))
                    {
                        vtxMaterialNames.AddRange(AssetUtils.FindVtxMaterials(ext_path));
                    }
                }

                Tuple<List<string>, List<string>> mdlMatsAndModels;
                try
                {
                    mdlMatsAndModels = AssetUtils.findMdlMaterialsAndModels(externalPath, skins, vtxMaterialNames);
                }
                catch (Exception e)
                {
                    ExceptionHandler.LogException(e, false);
                    CompilePalLogger.LogCompileError($"Failed to read file {externalPath}", new Error($"Failed to read file {externalPath}", ErrorSeverity.Error));
                    return;
                }

                foreach (var mat in mdlMatsAndModels.Item1)
                {
                    AddTexture(mat);
                }

                foreach (var model in mdlMatsAndModels.Item2)
                {
                    AddModel(model);
                }

            }
        }

        public void AddTexture(string internalPath)
        {
            // adds vmt files and finds its dependencies
            var externalPath = FindExternalFile(internalPath);
            if (AddInternalFile(internalPath, externalPath))
            {
                vmtcount++;
                foreach (var vtf in AssetUtils.findVmtTextures(externalPath))
                {
                    AddInternalFile(vtf, FindExternalFile(vtf));
                }
                foreach (var vmt in AssetUtils.findVmtMaterials(externalPath))
                {
                    AddTexture(vmt);
                }
            }
        }

        public void AddParticle(string internalPath)
        {
            // adds pcf files and finds its dependencies
            var externalPath = FindExternalFile(internalPath);
            if (externalPath == string.Empty)
            {
                CompilePalLogger.LogCompileError($"Failed to find particle manifest file {internalPath}", new Error($"Failed to find particle manifest file {internalPath}", ErrorSeverity.Error));
                return;
            }

            if (AddInternalFile(internalPath, externalPath))
            {

                var pcf = ParticleUtils.ReadParticle(externalPath);
                pcfcount++;
                foreach (var mat in pcf.MaterialNames)
                {
                    AddTexture(mat);
                }

                foreach (var model in pcf.ModelNames)
                {
                    AddModel(model);
                }
            }
            else
            {
                CompilePalLogger.LogCompileError($"Failed to find particle manifest file {internalPath}", new Error($"Failed to find particle manifest file {internalPath}", ErrorSeverity.Error));
            }
        }

        private string FindExternalFile(string internalPath)
        {
            // Attempts to find the file from the internalPath
            // returns the externalPath or an empty string

            var sanitizedPath = SanitizePath(internalPath);

            foreach (var source in sourceDirs)
            {
                if (File.Exists(source + "/" + sanitizedPath))
                {
                    return source + "/" + sanitizedPath.Replace("\\", "/");
                }
            }
            return "";
        }
        private string SanitizePath(string path)
        {
            return Regex.Replace(path, invalidRegString, "");
        }
    }
}
