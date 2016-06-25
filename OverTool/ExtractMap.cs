﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.ModelWriter;
using OverTool.ExtractLogic;

namespace OverTool {
  class ExtractMap {
    public static void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      if(args.Length < 1) {
        Console.Out.WriteLine("Usage: OverTool.exe overwatch M output [maps]");
        return;
      }

      string output = args[0];
      List<string> maps = args.Skip(1).ToList();
      for(int i = 0; i < maps.Count; ++i) {
        maps[i] = maps[i].ToLowerInvariant();
      }
      bool mapWildcard = maps.Count == 0;

      List<ulong> masters = track[0x9F];
      List<byte> LODs = new List<byte>(new byte[2] { 1, 255 });
      foreach(ulong masterKey in masters) {
        if(!map.ContainsKey(masterKey)) {
          continue;
        }
        STUD masterStud = new STUD(Util.OpenFile(map[masterKey], handler));
        MapMaster master = (MapMaster)masterStud.Instances[0];
        if(master == null) {
          continue;
        }
        
        string name = Util.GetString(master.Header.name.key, map, handler);
        if(!mapWildcard && !maps.Contains(name.ToLowerInvariant())) {
          continue;
        }

        string outputPath = string.Format("{0}{1}{2}{1}{3:X}{1}", output, Path.DirectorySeparatorChar, Util.SanitizePath(name), APM.keyToIndex(master.Header.data.key));

        if(!map.ContainsKey(master.Header.data.key)) {
          continue;
        }
        
        HashSet<ulong> parsed = new HashSet<ulong>();
        using(Stream mapStream = Util.OpenFile(map[master.Header.data.key], handler)) {
          Console.Out.WriteLine("Extracting map {0} with ID {1:X8}", name, APM.keyToIndex(master.Header.data.key));
          Map mapData = new Map(mapStream);
          OWMAPWriter owmap = new OWMAPWriter();
          Dictionary<ulong, List<string>>[] used = null;
          if(!Directory.Exists(outputPath)) {
            Directory.CreateDirectory(outputPath);
          }
          using(Stream outputStream = File.Open(string.Format("{0}{1}{2}", outputPath, Util.SanitizePath(name), owmap.Format), FileMode.Create, FileAccess.Write)) {
            used = owmap.Write(outputStream, mapData, name);
          }
          IModelWriter owmdl = new OWMDLWriter();
          IModelWriter owmat = new OWMATWriter();
          if(used != null) {
            Dictionary<ulong, List<string>> models = used[0];
            Dictionary<ulong, List<string>> materials = used[1];
            Dictionary<ulong, Dictionary<ulong, List<ImageLayer>>> cache = new Dictionary<ulong, Dictionary<ulong, List<ImageLayer>>>();

            foreach(KeyValuePair<ulong, List<string>> modelpair in models) {
              if(!map.ContainsKey(modelpair.Key)) {
                continue;
              }
              if(!parsed.Add(modelpair.Key)) {
                continue;
              }
              using(Stream modelStream = Util.OpenFile(map[modelpair.Key], handler)) {
                Model mdl = new Model(modelStream);
                foreach(string modelOutput in modelpair.Value) {
                  using(Stream outputStream = File.Open(string.Format("{0}{1}", outputPath, modelOutput), FileMode.Create, FileAccess.Write)) {
                    owmdl.Write(mdl, outputStream, LODs, new Dictionary<ulong, List<ImageLayer>>(), new object[0]);
                  Console.Out.WriteLine("Wrote model {0}", modelOutput);
                  }
                }
              }
            }
            foreach(KeyValuePair<ulong, List<string>> matpair in materials) {
              Dictionary<ulong, List<ImageLayer>> tmp = new Dictionary<ulong, List<ImageLayer>>();
              if(cache.ContainsKey(matpair.Key)) {
                tmp = cache[matpair.Key];
              } else {
                Skin.FindTextures(matpair.Key, tmp, new Dictionary<ulong, ulong>(), new HashSet<ulong>(), map, handler);
                cache.Add(matpair.Key, tmp);
              }
              
              foreach(KeyValuePair<ulong, List<ImageLayer>> kv in tmp) {
                ulong materialId = kv.Key;
                List<ImageLayer> sublayers = kv.Value;
                foreach(ImageLayer layer in sublayers) {
                  if(!parsed.Add(layer.key)) {
                    continue;
                  }
                  Skin.SaveTexture(layer.key, map, handler, string.Format("{0}{1:X12}.dds", outputPath, APM.keyToIndexID(layer.key)));
                }
              }
              
              foreach(string matOutput in matpair.Value) {
                using(Stream outputStream = File.Open(string.Format("{0}{1}", outputPath, matOutput), FileMode.Create, FileAccess.Write)) {
                  owmat.Write(null, outputStream, null, tmp, new object[0]);
                  Console.Out.WriteLine("Wrote material {0}", matOutput);
                }
              }
            }
          }
        }
      }
    }
  }
}