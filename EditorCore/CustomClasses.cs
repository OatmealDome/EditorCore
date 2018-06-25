﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Drawing;
using EditorCore.Interfaces;

namespace EditorCore
{
	public static class ByamlTypeHelper
	{
		public delegate dynamic ConvertMethod(string inString);
		public static readonly Dictionary<Type, ConvertMethod> StringToNodeTable = new Dictionary<Type, ConvertMethod>()
		{
			{ typeof(string) , (s) => s },
			{ typeof(int) , (s) => (int.Parse(s)) },
			{ typeof(uint) , (s) =>(uint.Parse(s)) },
			{ typeof(long) , (s) => (long.Parse(s)) },
			{ typeof(ulong) , (s) => (ulong.Parse(s)) },
			{ typeof(double) , (s) =>(double.Parse(s)) },
			{ typeof(float) , (s) => (float.Parse(s)) },
		};
		public static dynamic ConvertValue(Type t, string value) => StringToNodeTable[t](value);
	}

	public static class InputDialog
    {
        public static DialogResult Show(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }
    }

	public static class DeepCloneDictArr
    {
        public static Dictionary<string, dynamic> DeepClone(Dictionary<string, dynamic> d)
        {
            var res = new Dictionary<string, dynamic>();
            foreach (string k in d.Keys)
            {
                if (d[k] is Dictionary<string, dynamic> || d[k] is List<dynamic>)
                    res.Add(k, DeepClone(d[k]));
                else if (d[k] is ICloneable)
                    res.Add(k, d[k].Clone());
                else res.Add(k, d[k]);
            }
            return res;
        }

        public static List<dynamic> DeepClone(List<dynamic> l)
        {
            var res = new List<dynamic>();
            foreach (var o in l)
            {
                if (o is Dictionary<string, dynamic> || o is List<dynamic>)
                    res.Add(DeepClone(o));
                else if (o is ICloneable)
                    res.Add(o.Clone());
                else res.Add(o);
            }
            return res;
        }
    }

	public static class IEnumerableCompare
	{

		private static bool IDictionaryIsEqual(IDictionary<string, dynamic> a, IDictionary<string, dynamic> b)
		{
			if (a.Count != b.Count) return false;
			foreach (string key in a.Keys)
			{
				if (!b.ContainsKey(key)) return false;
				if ((a[key] == null && b[key] != null) || (a[key] != null && b[key] == null)) return false;
				else if (a[key] == null && b[key] == null) continue;

				if (TypeNotEqual(a[key].GetType(), b[key].GetType())) return false;

				if (a[key] is IList<dynamic> && IListIsEqual(a[key], b[key])) continue;
				else if (a[key] is IDictionary<string, dynamic> && IDictionaryIsEqual(a[key], b[key])) continue;
				else if (a[key] == b[key]) continue;

				return false;
			}
			return true;
		}

		private static bool IListIsEqual(IList<dynamic> a, IList<dynamic> b)
		{
			if (a.Count != b.Count) return false;
			for (int i = 0; i < a.Count; i++)
			{
				if ((a[i] == null && b[i] != null) || (a[i] != null && b[i] == null)) return false;
				else if (a[i] == null && b[i] == null) continue;

				if (TypeNotEqual(a[i].GetType(), b[i].GetType())) return false;

				if (a[i] is IList<dynamic> && IListIsEqual(a[i], b[i])) continue;
				else if (a[i] is IDictionary<string, dynamic> && IDictionaryIsEqual(a[i], b[i])) continue;
				else if (a[i] == b[i]) continue;

				return false;
			}
			return true;
		}

		public static bool TypeNotEqual(Type a, Type b)
		{
			return !(a.IsAssignableFrom(b) || b.IsAssignableFrom(a)); // without this LinksNode wouldn't be equal to IDictionary<string,dynamic>
		}

		public static bool IsEqual(IEnumerable a, IEnumerable b)
		{
			if (TypeNotEqual(a.GetType(), b.GetType())) return false;
			if (a is IDictionary) return IDictionaryIsEqual((IDictionary<string, dynamic>)a, (IDictionary<string, dynamic>)b);
			else return IListIsEqual((IList<dynamic>)a, (IList<dynamic>)b);
		}
	}

	public class CustomStringWriter : System.IO.StringWriter
    {
        private readonly Encoding encoding;

        public CustomStringWriter(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public override Encoding Encoding
        {
            get { return encoding; }
        }
    }

    public class CustomStack<T> : IEnumerable<T>
    {
        private List<T> items = new List<T>();
        public int MaxItems = 50;

        public int Count
        { get { return items.Count(); } }

        public void Remove(int index)
        {
            items.RemoveAt(index);
        }

        public void Push(T item)
        {
            items.Add(item);
            if (items.Count > MaxItems)
            {
                for (int i = MaxItems; i < items.Count; i++) Remove(0);
            }
        }

        public T Pop()
        {
            if (items.Count > 0)
            {
                T tmp = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                return tmp;
            }
            else return default(T);
        }

        public void RemoveAt(int index) => items.RemoveAt(index);

        public T Peek() { return items[items.Count - 1]; }

        public T[] ToArray()
        {
            return items.ToArray();
        }

        public void Clear()
        {
            items.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }
    }

    public class UndoAction
    {
        public string actionName;
        public Action<dynamic> _act;
        public dynamic _arg;

        public void Undo()
        {
            _act.Invoke(_arg);
        }

        public override string ToString()
        {
            return actionName;
        }

        public UndoAction(string name, Action<dynamic> Act, dynamic arg)
        {
            actionName = name;
            _act = Act;
            _arg = arg;
        }

    }

    public class ClipBoardItem
    {
        public enum ClipboardType
        {
            NotSet = 0,
            Position = 1,
            Rotation = 2,
            Scale = 3,
            IntArray = 4,
            Objects = 5,
            Transform = 8,
        }

        public Transform transform;
        public int[] Args = null;
        public ClipboardType Type = 0;
        public ILevelObj[] Objs = null;

        public override string ToString()
        {
            switch (Type)
            {
                case ClipboardType.Position:
                    return $"Position - {transform.Pos}";
                case ClipboardType.Rotation:
                    return $"Rotation - {transform.Rot}";
                case ClipboardType.Scale:
                    return $"Scale - {transform.Scale}";
                case ClipboardType.IntArray:
                    return "Args[]";
                case ClipboardType.Transform:
                    return $"Transform - Pos {transform.Pos}, Rot {transform.Rot}, Scale {transform.Scale}";
                case ClipboardType.Objects:
                    return "Object[" + Objs.Length.ToString() + "]";
                default:
                    return "Not set";
            }
        }
    }
}

namespace ExtensionMethods
{
    static class Extensions
    {
        public static Vector3D ToVect(this Point3D p)
        {
            return new Vector3D(p.X, p.Y, p.Z);
        }
    }
}