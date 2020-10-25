using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using CompilePalX.Compilers.UtilityProcess;
using CompilePalX.Compiling;

namespace CompilePalX.Compilers.BSPPack
{
    class PakFile
    {
        // This class is the class responsible for building the list of files to include
        // The list can be saved to a text file for use with bspzip

        // the dictionary is formated as <internalPath, externalPath>
        // matching the bspzip specification https://developer.valvesoftware.com/wiki/BSPZIP
        private IDictionary<string, string> Files;

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
                                   && !excludedDirs.Any(externalPath.ToLower().Replace('/', '\\').StartsWith))
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

        private List<string> excludedFiles;
	    private List<string> excludedDirs;

        private List<string> sourceDirs;
        private string fileName;

        public int mdlcount { get; private set; }
        public int vmtcount { get; private set; }
        public int pcfcount { get; private set; }
        public int sndcount { get; private set; }
        public int vehiclescriptcount { get; private set; }
        public int effectscriptcount { get; private set; }
        public int vscriptcount { get; private set; }

        public PakFile(BSP bsp, List<string> sourceDirectories, List<string> includeFiles, List<string> excludedFiles, List<string> excludedDirs, string outputFile)
        {
            mdlcount = vmtcount = pcfcount = sndcount = vehiclescriptcount = effectscriptcount = 0;
            sourceDirs = sourceDirectories;
            fileName = outputFile;

	        this.excludedFiles = excludedFiles;
	        this.excludedDirs = excludedDirs;

            Files = new Dictionary<string, string>();

            if (bsp.nav.Key != default(string))
                AddFile(bsp.nav, (b => b.nav = default), bsp);

            if (bsp.detail.Key != default(string))
                AddFile(bsp.detail, (b => b.detail = default), bsp);

            if (bsp.kv.Key != default(string))
                AddFile(bsp.kv, (b => b.kv = default), bsp);

            if (bsp.txt.Key != default(string))
                AddFile(bsp.txt, (b => b.txt = default), bsp);

            if (bsp.jpg.Key != default(string))
                AddFile(bsp.jpg, (b => b.jpg = default), bsp);

            if (bsp.radartxt.Key != default(string))
                AddFile(bsp.radartxt, (b => b.radartxt = default), bsp);

            if (bsp.res.Key != default(string))
            {
                if (AddFile(bsp.res, (b => b.res = default), bsp))
                {
                    foreach (string material in AssetUtils.findResMaterials(bsp.res.Value))
                        AddTexture(material);
                }
            }

            if (bsp.particleManifest.Key != default(string))
            {
                if (AddFile(bsp.particleManifest, (b => b.particleManifest = default), bsp))
                {
                    foreach (string particle in AssetUtils.findManifestPcfs(bsp.particleManifest.Value))
                        AddParticle(particle);
                }
            }
            
            if (bsp.soundscape.Key != default(string))
            {
                if (AddFile(bsp.soundscape, (b => b.soundscape = default), bsp))
                {
                    foreach (string sound in AssetUtils.findSoundscapeSounds(bsp.soundscape.Value))
                        if (AddInternalFile(sound, FindExternalFile(sound)))
                            sndcount++;
                }
            }
            
            if (bsp.soundscript.Key != default(string))
            {
                if (AddFile(bsp.soundscript, (b => b.soundscript = default), bsp))
                {
                    foreach (string sound in AssetUtils.findSoundscapeSounds(bsp.soundscript.Value))
                        if (AddInternalFile(sound, FindExternalFile(sound)))
                            sndcount++;
                }
            }

            // find color correction files
            foreach (Dictionary<string, string> cc in bsp.entityList.Where(item => item["classname"] == "color_correction"))
                if (cc.ContainsKey("filename"))
                    AddInternalFile(cc["filename"], FindExternalFile(cc["filename"]));

            foreach (KeyValuePair<string, string> vehicleScript in bsp.VehicleScriptList)
                if (AddInternalFile(vehicleScript.Key, vehicleScript.Value))
                    vehiclescriptcount++;
	        foreach (KeyValuePair<string, string> effectScript in bsp.EffectScriptList)
		        if (AddInternalFile(effectScript.Key, effectScript.Value))
			        effectscriptcount++;
            foreach (KeyValuePair<string, string> dds in bsp.radardds)
                AddInternalFile(dds.Key, dds.Value);
            foreach (KeyValuePair<string, string> lang in bsp.languages)
                AddInternalFile(lang.Key, lang.Value);
            foreach (string model in bsp.EntModelList)
                AddModel(model);
            for (int i = 0; i < bsp.ModelList.Count; i++)
                AddModel(bsp.ModelList[i], bsp.modelSkinList[i]);
            foreach (string vmt in bsp.TextureList)
                AddTexture(vmt);
            foreach (string vmt in bsp.EntTextureList)
                AddTexture(vmt);
            foreach (string sound in bsp.EntSoundList)
                if (AddInternalFile(sound, FindExternalFile(sound)))
                    sndcount++;
            foreach (string vscript in bsp.vscriptList)
                if (AddInternalFile(vscript, FindExternalFile(vscript)))
                    vscriptcount++;

			// add all manually included files
			// TODO right now the manually included files search for files it depends on. Not sure if this should be default behavior
	        foreach (var file in includeFiles)
	        {
				// try to get the source directory the file is located in
				FileInfo fileInfo = new FileInfo(file);

				// default base directory is the game folder
		        string baseDir = GameConfigurationManager.GameConfiguration.GameFolder;

		        var potentialSubDir = new List<string>(sourceDirs); // clone to prevent accidental modification
				potentialSubDir.Remove(baseDir);
		        foreach (var folder in potentialSubDir)
		        {
			        if (fileInfo.Directory != null 
			            && fileInfo.Directory.FullName.Contains(folder))
			        {
				        baseDir = folder;
						break;
			        }
		        }

                // check needed for when file does not exist in any sub directory or the base directory
                if (fileInfo.Directory != null && !fileInfo.Directory.FullName.Contains(baseDir))
                {
                    CompilePalLogger.LogCompileError($"Failed to resolve internal path for {file}, skipping\n", new Error($"Failed to resolve internal path for {file}, skipping", ErrorSeverity.Error));
                    continue;
                }

		        string internalPath = file.Replace(baseDir + "\\", "");

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
					default:
						AddInternalFile(internalPath, file);
						break;
		        }
	        }
		}

        public void OutputToFile()
        {
            var outputLines = new List<string>();

            foreach (KeyValuePair<string, string> entry in Files)
            {
                outputLines.Add(entry.Key);
                outputLines.Add(entry.Value);
            }

            if (!Directory.Exists("BSPZipFiles"))
                Directory.CreateDirectory("BSPZipFiles");

            if (File.Exists(fileName))
                File.Delete(fileName);
            File.WriteAllLines(fileName, outputLines);
        }

        public Dictionary<string,string> GetResponseFile()
        {
            var output = new Dictionary<string,string>();

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
            string externalPath = FindExternalFile(internalPath);
            if (AddInternalFile(internalPath, externalPath))
            {
                mdlcount++;
                List<string> vtxMaterialNames = new List<string>();
                foreach (string reference in AssetUtils.findMdlRefs(internalPath))
                {
                    string ext_path = FindExternalFile(reference);
                    AddInternalFile(reference, FindExternalFile(reference));
                    if (reference.EndsWith(".phy"))
                        foreach (string gib in AssetUtils.findPhyGibs(ext_path))
                            AddModel(gib);
                    else if (reference.EndsWith(".vtx"))
                        vtxMaterialNames.AddRange(AssetUtils.FindVtxMaterials(ext_path));
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

	            foreach (string mat in mdlMatsAndModels.Item1)
					AddTexture(mat);

	            foreach (var model in mdlMatsAndModels.Item2)
					AddModel(model);

            }
        }

        public void AddTexture(string internalPath)
        {
            // adds vmt files and finds its dependencies
            string externalPath = FindExternalFile(internalPath);
            if (AddInternalFile(internalPath, externalPath))
            {
                vmtcount++;
                foreach (string vtf in AssetUtils.findVmtTextures(externalPath))
                    AddInternalFile(vtf, FindExternalFile(vtf));
                foreach (string vmt in AssetUtils.findVmtMaterials(externalPath))
                    AddTexture(vmt);
            }
        }

        public void AddParticle(string internalPath)
        {
            // adds pcf files and finds its dependencies
            string externalPath = FindExternalFile(internalPath);
            if (externalPath == String.Empty)
            {
				CompilePalLogger.LogCompileError($"Failed to find particle manifest file {internalPath}", new Error($"Failed to find particle manifest file {internalPath}", ErrorSeverity.Error));
				return;
            }

            if (AddInternalFile(internalPath, externalPath))
            {

				PCF pcf = ParticleUtils.ReadParticle(externalPath);
                pcfcount++;
                foreach (string mat in pcf.MaterialNames)
                    AddTexture(mat);

                foreach (string model in pcf.ModelNames)
                {
                    AddModel(model);
                }
            }
            else
            {
				CompilePalLogger.LogCompileError($"Failed to find particle manifest file {internalPath}", new Error($"Failed to find particle manifest file {internalPath}", ErrorSeverity.Error));
				return;
            }
        }

        private string FindExternalFile(string internalPath)
        {
            // Attempts to find the file from the internalPath
            // returns the externalPath or an empty string

	        var sanitizedPath = SanitizePath(internalPath);

			foreach (string source in sourceDirs)
                if (File.Exists(source +"/"+ sanitizedPath))
                    return source + "/" + sanitizedPath.Replace("\\", "/");
            return "";
        }

		private static readonly string invalidChars = Regex.Escape(new string(Path.GetInvalidPathChars()));
		private static readonly string invalidRegString = $@"([{invalidChars}]*\.+$)|([{invalidChars}]+)";
		private string SanitizePath(string path)
	    {
		    return Regex.Replace(path, invalidRegString, "");
	    }
    }
}
