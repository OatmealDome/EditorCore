﻿using BfresLib;
using EditorCore;
using EditorCore.Interfaces;
using EveryFileExplorer;
using Syroot.NintenTools.Byaml.Dynamic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MK8DExt
{
	class MK8DModule : IGameModule
	{
		static Dictionary<int, string> ObjIDNameList = new Dictionary<int, string>();
		public static string GetName(int Id)
		{
			if (ObjIDNameList.ContainsKey(Id)) return ObjIDNameList[Id];
			return "Undefined";
		}

		public static int GetId(string Name)
		{
			Name = Name.ToLower();
			var res = ObjIDNameList.Where(x => x.Value.ToLower() == Name);
			return res.Count() == 0 ? 0 : res.First().Key;
		}

		public string ModuleName => "Mario Kart 8 Deluxe";
		public Tuple<Type, Type>[] GetClassConverters => null;
		public string[] ReservedPropNames => new string[0];
		public string[] ModelFieldPropNames => new string[0];

		public bool IsAddListSupported => false;
		public bool IsPropertyEditingSupported => false;

		public string LevelFormatFilter => "course_muunt.byaml |*_muunt.byaml| every file | *.*";

		public EditorForm ViewForm { get; set; }
		public string[] AutoHideList => new string[0];

		public string AddObjList(ILevel level)
		{
			throw new NotImplementedException();
		}

		public IObjList CreateObjList(string name, IList<dynamic> baseList) => new ObjList(name, baseList);

		public void EditChildrenNode(ILevelObj obj)
		{
			return;
		}

		string ModelsFolder => EditorForm.ModelsFolder;
		public string GameFolder => EditorForm.GameFolder;
		public void FormLoaded()
		{
			if (!Directory.Exists(ModelsFolder))
			{
				Directory.CreateDirectory(ModelsFolder);
			}
			if (!File.Exists($"{GameFolder}Data/Objflow.byaml"))
			{
				MessageBox.Show($"Can't open {GameFolder}Data/Objflow.byaml models and object names won't be shown");
			}
			else
			{
				ObjIDNameList.Clear();
				var byml = ByamlFile.Load($"{GameFolder}Data/Objflow.byaml", true);
				foreach (var item in byml)
				{
					int ID = item["ObjId"];
					List<string> files = ((IList<object>)item["ResName"]).Cast<string>().ToList();
					if (/*files.Count > 1 || */files.Count == 0) Debugger.Break();
					ObjIDNameList.Add(ID, files[0]);
				}
				byml = null;
				GC.Collect();
			}
		}

		public bool GetModelFile(string ObjName, string path) => BfresConverter.Convert(FindBfres(ObjName), path);

		byte[] FindBfres(string objname)
		{
			if (File.Exists($"{GameFolder}MapObj\\{objname}\\{objname}.bfres"))
			{
				return File.ReadAllBytes($"{GameFolder}MapObj\\{objname}\\{objname}.bfres");
			}
			return null;
		}

		public void InitModule(EditorForm currentView)
		{
			ViewForm = currentView;
		}

		LevelObj StageDummyModel;
		public ILevel LoadLevel(string file)
		{
			var res = new Level(file);

			string stageName = new DirectoryInfo(file).Parent.Name;
			string stageModelPath = $"{ModelsFolder}\\{stageName}_stage.obj";
			if (!File.Exists(stageModelPath)) {
				byte[] CourseBfres = YAZ0.Decompress(Path.GetDirectoryName(file) + "\\course_model.szs");
				if (!BfresConverter.Convert(CourseBfres, stageModelPath)) stageModelPath = null;
			}

			if (stageModelPath != null)
			{
				StageDummyModel = new LevelObj(true,false);
				ViewForm.AddModelObj(stageModelPath, StageDummyModel,
					new System.Windows.Media.Media3D.Vector3D(),
					new System.Windows.Media.Media3D.Vector3D(1, 1, 1),
					new System.Windows.Media.Media3D.Vector3D());
			}

			GC.Collect();

			return res;
		}

		public ILevel NewLevel(string file)
		{
			MessageBox.Show("New levels can't be created");
			throw new NotImplementedException();
		}

		public ILevelObj NewObject() => new LevelObj();

		public void OpenLevelFile(string name, Stream file) //Mk8 doesn't use archives for levels
		{
			throw new NotImplementedException();
		}

		public void ParseArgs(string[] Args)
		{
			if (Args.Length > 0 && Args[0].EndsWith(".byaml"))
				LoadLevel(Args[0]);
		}
	}
}