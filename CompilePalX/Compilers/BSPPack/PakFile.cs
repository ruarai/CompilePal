using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CompilePalX.Compilers.UtilityProcess;

namespace CompilePalX.Compilers.BSPPack
{
    class PakFile
    {
        // This class is the class responsible for building the list of files to include
        // The list can be saved to a text file for use with bspzip

        // the dictionary is formated as <internalPath, externalPath>
        // matching the bspzip specification https://developer.valvesoftware.com/wiki/BSPZIP
        private IDictionary<string, string> Files;
	    private List<string> excludedFiles;
	    private List<string> excludedDirs;

        private List<string> sourceDirs;

        public int mdlcount { get; private set; }
        public int vmtcount { get; private set; }
        public int pcfcount { get; private set; }
        public int sndcount { get; private set; }
        public int vehiclescriptcount { get; private set; }
        public int effectscriptcount { get; private set; }
        public int vscriptcount { get; private set; }

        public PakFile(BSP bsp, List<string> sourceDirectories, List<string> includeFiles, List<string> excludedFiles, List<string> excludedDirs)
        {
            mdlcount = vmtcount = pcfcount = sndcount = vehiclescriptcount = effectscriptcount = 0;
            sourceDirs = sourceDirectories;

	        this.excludedFiles = excludedFiles;
	        this.excludedDirs = excludedDirs;

            Files = new Dictionary<string, string>();

            if (bsp.nav.Key != default(string))
                Files.Add(bsp.nav);

            if (bsp.detail.Key != default(string))
                Files.Add(bsp.detail);

            if (bsp.kv.Key != default(string))
                Files.Add(bsp.kv);

            if (bsp.txt.Key != default(string))
                Files.Add(bsp.txt);

            if (bsp.jpg.Key != default(string))
                Files.Add(bsp.jpg);

            if (bsp.radartxt.Key != default(string))
                Files.Add(bsp.radartxt);

            if (bsp.res.Key != default(string))
            {
                Files.Add(bsp.res);
                foreach (string material in AssetUtils.findResMaterials(bsp.res.Value))
                    AddTexture(material);
            }

            if (bsp.particleManifest.Key != default(string))
            {
                Files.Add(bsp.particleManifest);
                foreach (string particle in AssetUtils.findManifestPcfs(bsp.particleManifest.Value))
                    AddParticle(particle);
            }
            
            if (bsp.soundscape.Key != default(string))
            {
                Files.Add(bsp.soundscape);
                foreach (string sound in AssetUtils.findSoundscapeSounds(bsp.soundscape.Value))
                    if (AddFile(sound, FindExternalFile(sound)))
                        sndcount++;
            }
            
            if (bsp.soundscript.Key != default(string))
            {
                Files.Add(bsp.soundscript);
                foreach (string sound in AssetUtils.findSoundscapeSounds(bsp.soundscript.Value))
                    if (AddFile(sound, FindExternalFile(sound)))
                        sndcount++;
            }

            // find color correction files
            foreach (Dictionary<string, string> cc in bsp.entityList.Where(item => item["classname"] == "color_correction"))
                if (cc.ContainsKey("filename"))
                    AddFile(cc["filename"], FindExternalFile(cc["filename"]));

            foreach (KeyValuePair<string, string> vehicleScript in bsp.VehicleScriptList)
                if (AddFile(vehicleScript.Key, vehicleScript.Value))
                    vehiclescriptcount++;
	        foreach (KeyValuePair<string, string> effectScript in bsp.EffectScriptList)
		        if (AddFile(effectScript.Key, effectScript.Value))
			        effectscriptcount++;
            foreach (KeyValuePair<string, string> dds in bsp.radardds)
                AddFile(dds.Key, dds.Value);
            foreach (KeyValuePair<string, string> lang in bsp.languages)
                AddFile(lang.Key, lang.Value);
            foreach (string model in bsp.EntModelList)
                AddModel(model);
            for (int i = 0; i < bsp.ModelList.Count; i++)
                AddModel(bsp.ModelList[i], bsp.modelSkinList[i]);
            foreach (string vmt in bsp.TextureList)
                AddTexture(vmt);
            foreach (string vmt in bsp.EntTextureList)
                AddTexture(vmt);
            foreach (string sound in bsp.EntSoundList)
                if (AddFile(sound, FindExternalFile(sound)))
                    sndcount++;
            foreach (string vscript in bsp.vscriptList)
                if (AddFile(vscript, FindExternalFile(vscript)))
                    vscriptcount++;

			// add all manually included files
			// TODO right now the manually included files search for files it depends on. Not sure if this should be default behavior
	        foreach (var file in includeFiles)
	        {
				// try to get the source directory the file is located in
				FileInfo fileInfo = new FileInfo(file);

				// default base directory is the game folder
		        string baseDir = GameConfigurationManager.GameConfiguration.GameFolder;

		        var potentialSubDir = sourceDirs;
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
						AddFile(internalPath, file);
						sndcount++;
						break;
					default:
						AddFile(internalPath, file);
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

            if (File.Exists("files.txt"))
                File.Delete("files.txt");
            File.WriteAllLines("files.txt", outputLines);
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

        public bool AddFile(string internalPath, string externalPath)
        {
            // adds file to the pakfile list
            if (externalPath != "" && File.Exists(externalPath) 
                                   && !excludedFiles.Contains(externalPath.ToLower().Replace('/', '\\'))
                                   && !excludedDirs.Any(externalPath.ToLower().Replace('/', '\\').StartsWith))
            {
                internalPath = internalPath.Replace("\\", "/");
                if (!Files.ContainsKey(internalPath))
                {
                    Files.Add(internalPath, externalPath);
                    return true;
                }
            }
            return false;
        }

        public void AddModel(string internalPath, List<int> skins = null)
        {
            // adds mdl files and finds its dependencies
            string externalPath = FindExternalFile(internalPath);
            if (AddFile(internalPath, externalPath))
            {
                mdlcount++;
                foreach (string reference in AssetUtils.findMdlRefs(internalPath))
                {
                    string ext_path = FindExternalFile(reference);
                    AddFile(reference, FindExternalFile(reference));
                    if (reference.EndsWith(".phy"))
                        foreach (string gib in AssetUtils.findPhyGibs(ext_path))
                            AddModel(gib);
                }
				var mdlMatsAndModels = AssetUtils.findMdlMaterialsAndModels(externalPath, skins);

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
            if (AddFile(internalPath, externalPath))
            {
                vmtcount++;
                foreach (string vtf in AssetUtils.findVmtTextures(externalPath))
                    AddFile(vtf, FindExternalFile(vtf));
                foreach (string vmt in AssetUtils.findVmtMaterials(externalPath))
                    AddTexture(vmt);
            }
        }

        public void AddParticle(string internalPath)
        {
            // adds pcf files and finds its dependencies
            string externalPath = FindExternalFile(internalPath);
            PCF pcf = ParticleUtils.ReadParticle(externalPath);

            if (AddFile(internalPath, externalPath))
            {
                pcfcount++;
                foreach (string mat in pcf.MaterialNames)
                    AddTexture(mat);

                foreach (string model in pcf.ModelNames)
                {
                    AddModel(model);
                }
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
