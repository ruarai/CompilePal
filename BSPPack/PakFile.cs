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
        private List<string> Files;
        List<string> sourceDirs;

        public PakFile(BSP bsp, List<string> sourceDirectories)
        {
            Files = new List<string>();
            sourceDirs = sourceDirectories;

            if (bsp.nav != "")
                AddFile(bsp.nav);

            if (bsp.particleManifest != "")
            {
                foreach (string particle in AssetUtils.findManifestPcfs(bsp.particleManifest))
                    AddParticle(particle);
            }
            
            if (bsp.soundscape != "")
            {
                foreach (string sound in AssetUtils.findSoundscapeSounds(bsp.soundscape))
                    AddFile(sound);
            }

            if (bsp.detail != "") {} // todo parse detail files

            bsp.getModelList();
            bsp.getModelListDyn();
            bsp.getTextureList();
            bsp.getSoundList();
        }

        public void AddFile(string fullpath)
        {
            // adds file to the pakfile list, path must be complete
            if (fullpath != "" && File.Exists(fullpath) && !Files.Contains(fullpath))
                Files.Add(fullpath);
        }

        public void AddModel(string rawpath)
        {
            // adds mdl files and finds its dependencies
            string fullpath = FindRawFile(rawpath);
            if (fullpath != "")
            {
                AddFile(fullpath);
                foreach (string reference in AssetUtils.findMdlRefs(fullpath))
                    AddFile(FindRawFile(reference));
                    
                foreach (string mat in AssetUtils.findMdlMaterials(fullpath))
                    AddTexture(mat);
            }
        }

        public void AddTexture(string rawpath)
        {
            // adds vmt files and finds its dependencies
            String fullpath = FindRawFile(rawpath);
            if (fullpath != "")
            {
                AddFile(fullpath);
                foreach (string vtf in AssetUtils.findVmtTextures(fullpath))
                    AddFile(FindRawFile(vtf));
            }
        }

        public void AddParticle(string rawpath)
        {
            // adds pcf files and finds its dependencies
            String fullpath = FindRawFile(rawpath);
            if (fullpath != "")
            {
                AddFile(fullpath);
                foreach (string mat in AssetUtils.findPcfMaterials(fullpath))
                    AddTexture(mat);
            }
        }

        private string FindRawFile(string rawpath)
        {
            // Attempts to find the file from it's raw/incomplete path
            // returns the full path or empty string

            foreach (string source in sourceDirs)
                if (File.Exists(source +"//"+ rawpath))
                    return source +"//"+ rawpath;
            return "";
        }
    }
}
