﻿using System;
using System.Collections.Generic;
using System.IO;
using DataTool.Flag;
using DataTool.Helper;
using static DataTool.Helper.IO;
using static DataTool.Program;
using TankLib;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-shaderhashes", Description = "Extract shader hashes (debug)", TrackTypes = new ushort[] {0x86}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugShaderHashes : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetSoundbanks(toolFlags);
        }

        public void GetSoundbanks(ICLIFlags toolFlags) {
            const string container = "ShaderHashes";
            
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            HashSet<uint> hashes = new HashSet<uint>();

            foreach (ulong GUID in TrackedFiles[0x86]) {
                teShaderInstance instance = new teShaderInstance(IO.OpenFile(GUID));
                teShaderCode shaderCode = new teShaderCode(IO.OpenFile(instance.Header.ShaderCode));

                //if (shaderCode.Header.ShaderType != Enums.teSHADER_TYPE.PIXEL) continue;
                //if (shaderCode.Header.ShaderType != Enums.teSHADER_TYPE.VERTEX) continue;
                //if (shaderCode.Header.ShaderType != Enums.teSHADER_TYPE.COMPUTE) continue;
                
                if (instance.TextureInputs == null) continue;
                foreach (teShaderInstance.TextureInputDefinition inputDefinition in instance.TextureInputs) {
                    hashes.Add((uint)inputDefinition.TextureType);
                }
            }

            string path = Path.Combine(basePath, container, "hashes.txt");
            CreateDirectoryFromFile(path);
            using (StreamWriter writer = new StreamWriter(path)) {
                foreach (uint hash in hashes) {
                    writer.WriteLine($"{hash:X8}");
                }
            }
        }
    }
}