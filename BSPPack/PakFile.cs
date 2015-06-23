using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSPPack
{
    class PakFile
    {
        // This class is the class responsible for building the list of files to include
        // The list can be saved to a text file for use with bspzip

        // the dictionary is formated as <internalPath, externalPath>
        // matching the bspzip specification https://developer.valvesoftware.com/wiki/BSPZIP
        private IDictionary<string, string> Files;

        private List<string> sourceDirs;

        public PakFile(BSP bsp, List<string> sourceDirectories)
        {
            Files = new Dictionary<string, string>();
            sourceDirs = sourceDirectories;

            if (bsp.nav.Key != default(string))
                Files.Add(bsp.nav);

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
                    AddFile(sound, FindExternalFile(sound));
            }

            if (bsp.detail.Key != default(string))
            {
                Files.Add(bsp.detail);
                // todo parse detail files
            }

            foreach (string model in bsp.getModelList())
                AddModel(model);
            foreach (string model in bsp.getModelListDyn())
                AddModel(model);
            foreach (string vmt in bsp.getTextureList())
                AddTexture(vmt);
            foreach (string sound in bsp.getSoundList())
                AddFile(sound, FindExternalFile(sound));
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

        public void AddModel(string internalPath)
        {
            // adds mdl files and finds its dependencies
            string externalPath = FindExternalFile(internalPath);
            if (AddFile(internalPath, externalPath))
            {
                foreach (string reference in AssetUtils.findMdlRefs(externalPath))
                    AddFile(internalPath, FindExternalFile(reference));
                    
                foreach (string mat in AssetUtils.findMdlMaterials(externalPath))
                    AddTexture(mat);
            }
        }

        public void AddTexture(string internalPath)
        {
            // adds vmt files and finds its dependencies
            string externalPath = FindExternalFile(internalPath);
            if (AddFile(internalPath, externalPath))
            {
                foreach (string vtf in AssetUtils.findVmtTextures(externalPath))
                    AddFile(vtf, FindExternalFile(vtf));
            }
        }

        public void AddParticle(string internalPath)
        {
            // adds pcf files and finds its dependencies
            string externalPath = FindExternalFile(internalPath);
            if (AddFile(internalPath, externalPath))
            {
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
