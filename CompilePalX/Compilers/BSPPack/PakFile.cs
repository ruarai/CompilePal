using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompilePalX.Compilers.BSPPack
{
    class PakFile
    {
        // This class is the class responsible for building the list of files to include
        // The list can be saved to a text file for use with bspzip

        // the dictionary is formated as <internalPath, externalPath>
        // matching the bspzip specification https://developer.valvesoftware.com/wiki/BSPZIP
        private IDictionary<string, string> Files;

        private List<string> sourceDirs;

        public int mdlcount { get; private set; }
        public int vmtcount { get; private set; }
        public int pcfcount { get; private set; }
        public int sndcount { get; private set; }

        public PakFile(BSP bsp, List<string> sourceDirectories)
        {
            mdlcount = vmtcount = pcfcount = sndcount = 0;
            sourceDirs = sourceDirectories;
            
            Files = new Dictionary<string, string>();

            if (bsp.nav.Key != default(string))
                Files.Add(bsp.nav);

            if (bsp.detail.Key != default(string))
                Files.Add(bsp.detail);

            if (bsp.kv.Key != default(string))
                Files.Add(bsp.kv);

            if (bsp.txt.Key != default(string))
                Files.Add(bsp.txt);

            if (bsp.radartxt.Key != default(string))
                Files.Add(bsp.radartxt);

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
                AddFile(cc["filename"], FindExternalFile(cc["filename"]));

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

        public bool AddFile(string internalPath, string externalPath)
        {
            // adds file to the pakfile list
            if (externalPath != "" && File.Exists(externalPath))
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
                    AddFile(reference, FindExternalFile(reference));
                    
                foreach (string mat in AssetUtils.findMdlMaterials(externalPath, skins))
                    AddTexture(mat);
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
            if (AddFile(internalPath, externalPath))
            {
                pcfcount++;
                foreach (string mat in AssetUtils.findPcfMaterials(externalPath))
                    AddTexture(mat);
            }
        }

        private string FindExternalFile(string internalPath)
        {
            // Attempts to find the file from the internalPath
            // returns the externalPath or an empty string

            foreach (string source in sourceDirs)
                if (File.Exists(source +"/"+ internalPath))
                    return source + "/" + internalPath.Replace("\\", "/");
            return "";
        }
    }
}
