#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using UnityEditorInternal;

using System.Reflection;

namespace Myy
{

	public class VMD
	{
		
		
		///
		/// <summary>
		/// Used to generate rotations values that you can
		/// then input inside the MMD editor.
		/// 
		/// <para>The MMD editor and the VMD format have different
		/// ways of representing rotations.</para>
		/// <para>This function is only useful when inputing values
		/// back in the MMD editor, for checking.
		/// You generally want to combine this with Debug.Log
		/// and .eulerAngles .</para>
		/// <para>For checking the values written inside the VMD
		/// files, use <see>VMDRotation</see>.</para>
		/// </summary>
		///
		/// 
		///
		/// <param name="transformRotation">The rotation to convert.</param>
		/// <returns>A quaternion value representing the same rotation,
		/// inside the MMD Editor</returns>
		static public Quaternion MMDRotation(Quaternion transformRotation)
		{
			transformRotation.x = -transformRotation.x;
			transformRotation.y = -transformRotation.y;
			
			//transformRotation.w = -transformRotation.w;
			return transformRotation;
		}

		/// <summary>Quick utility function to convert Unity rotations,
		/// as displayed in the editor, to rotations that can be input
		/// inside the MMD editor.
		/// <param>You probably don't want to use this function, unless
		/// you're quickly checking for rotation conversions issues.</param>
		/// </summary>
		/// <param name="x">Rotation on X axis, in Degrees.</param>
		/// <param name="y">Rotation on Y axis, in Degrees.</param>
		/// <param name="z">Rotation on Z axis, in Degrees.</param>
		/// <returns>A quaternion value representing the provided rotation
		/// inside the MMD rotation.
		/// <para>You'll have to use .eulerAngles on it,
		/// to get the X,Y,Z invidual angles values back.</para></returns>
		static public Quaternion MMDRotation(float x, float y, float z)
		{
			return MMDRotation(Quaternion.Euler(x, y, z));
		}

		/// <summary>
		/// Used to generate rotations values when generating
		/// VMD files.
		/// <para>This is automatically used by the VMD Write function
		/// to convert Unity rotations to VMD rotations.</para>
		/// <para>If you want to test a Unity rotation back inside the
		/// MMD editor, directly, don't use this function. Use
		/// <see>MMDRotation</see> instead.</para></summary>
		///
		/// <param name="transformRotation">The rotation to convert.</param>
		/// <returns>A quaternion value representing the same rotation,
		/// inside a VMD file.</returns>
		static public Quaternion VMDRotation(Quaternion transformRotation)
		{
			//transformRotation.x = -transformRotation.x;
			transformRotation.y = -transformRotation.y;
			
			transformRotation.w = -transformRotation.w;
			return transformRotation;
		}



		/// <summary>Vector3 to string function.
		/// <para>It's slightly better than toString, since it displays each
		/// axis value as-is, instead of rounding them.</para></summary>
		/// <param name="prop">The Vector3 value to convert.</param>
		/// <returns>A string representing each axis of the provided vector.</returns>
		public static string StringProp(Vector3 prop)
		{
			return $"[{prop.x}, {prop.y}, {prop.z}]";
		}

		public static float ReadableAngle(float angle)
		{
			return angle <= 180 ? angle : -360 + angle;
		}

		/// <summary>Quaternion to string function.
		/// <para>It's slightly better than toString, since it converts the
		/// Quaternion to Euler Angles and then displays each
		/// axis value as-is, instead of rounding them.</para></summary>
		/// <param name="prop">The Quaternion value to convert.</param>
		/// <returns>The euler representation of the provided quaternion,
		/// without extreme rounding.</returns>

		public static string StringProp(Quaternion prop)
		{
			Vector3 angles = prop.eulerAngles;
			return $"[{ReadableAngle(angles.x)}, {ReadableAngle(angles.y)}, {ReadableAngle(angles.z)}]";
		}

		/* Most of the code fater that comes from Lox VMDMotion,
		 * https://gitlab.com/lox9973/VMDMotion/-/blob/master/Script/Common/VMD.cs
		 * which he advised me to use after trying to reimplement
		 * it here https://gist.github.com/vr-voyage/b2a4e35b9603251a3c43899df297fb53#file-animtovmd-cs
		 */

		/* Not used here */
		public class BezierInterpolator {
			public float X0, Y0, X1, Y1; // degree-3 bezier curve: (0,0),(X0,Y0),(X1,Y1),(1,1)
			public float InverseLerp(float a, float b, float x) {
				x = Mathf.InverseLerp(a, b, x);
				float t = 0.5f;
				for(float p = 0.25f; p > 1e-6f; p *= 0.5f) // binary search x(t)
					t -= p * Mathf.Sign(t*(3*(1-t)*(X0 + t*(X1-X0)) + t*t) - x);
				return t*(3*(1-t)*(Y0 + t*(Y1-Y0)) + t*t); // evaluate y(t)
			}
			public bool IsLinear => X0 == Y0 && X1 == Y1;
			public override string ToString() => new Vector4(X0,Y0,X1,Y1).ToString();


		}

		
		public class BoneKeyframe {
			public string Name;
			public int FrameNumber;
			public Vector3 Position;
			public Quaternion Rotation;
			public (BezierInterpolator X, BezierInterpolator Y, BezierInterpolator Z, BezierInterpolator Rotation) Interp;

			public BoneKeyframe() { }
			public BoneKeyframe(BinaryReader binaryReader) { Read(binaryReader); }
			public void Read(BinaryReader binaryReader) {
				Name = VMD.ReadString(binaryReader, 15);
				FrameNumber = binaryReader.ReadInt32();
				Position = VMD.ReadVector3(binaryReader);
				Rotation = VMD.ReadQuaternion(binaryReader);
				Interp = (VMD.ReadBezierXYXY(binaryReader, 4), VMD.ReadBezierXYXY(binaryReader, 4),
					VMD.ReadBezierXYXY(binaryReader, 4), VMD.ReadBezierXYXY(binaryReader, 4));
			}
			/* The main part of this tool */
			public void WriteTo(BinaryWriter binaryWriter)
			{
				Debug.Log($"Writing Bone : {Name} - {FrameNumber} - {StringProp(Position)} - {StringProp(Rotation)}");
				VMD.WriteSJISString(binaryWriter, Name, 15);
				VMD.WriteInt(binaryWriter, FrameNumber);
				VMD.WriteVector3(binaryWriter, Position);
				VMD.WriteQuaternion(binaryWriter, Rotation);
				VMD.WriteInterpolation(binaryWriter, Interp);
			}
		}
		public class FaceKeyframe {
			public string Name;
			public uint FrameNumber;
			public float Weight;

			public FaceKeyframe() { }
			public FaceKeyframe(BinaryReader binaryReader) { Read(binaryReader); }
			public void Read(BinaryReader binaryReader) {
				Name = VMD.ReadString(binaryReader, 15);
				FrameNumber = binaryReader.ReadUInt32();
				Weight = binaryReader.ReadSingle();
			}
		}

		/* Not used here */
		public class CameraKeyframe {
			public int FrameNumber;
			public float Distance;
			public Vector3 Position;
			public Vector3 EulerAngles;
			public (BezierInterpolator X, BezierInterpolator Y, BezierInterpolator Z,
					BezierInterpolator EulerAngles,
					BezierInterpolator Distance, BezierInterpolator FieldOfView) Interp;
			public float FieldOfView;
			public bool Orthographic;

			public CameraKeyframe() { }
			public CameraKeyframe(BinaryReader binaryReader) { Read(binaryReader); }
			public void Read(BinaryReader binaryReader) {
				FrameNumber = binaryReader.ReadInt32();
				Distance = binaryReader.ReadSingle();
				Position = VMD.ReadVector3(binaryReader);
				EulerAngles = VMD.ReadVector3(binaryReader) * -Mathf.Rad2Deg; // Unity convention uses degree
				Interp = (VMD.ReadBezierXXYY(binaryReader, 1), VMD.ReadBezierXXYY(binaryReader, 1),
					VMD.ReadBezierXXYY(binaryReader, 1), VMD.ReadBezierXXYY(binaryReader, 1),
					VMD.ReadBezierXXYY(binaryReader, 1), VMD.ReadBezierXXYY(binaryReader, 1));
				FieldOfView = (float)binaryReader.ReadInt32();
				Orthographic = binaryReader.ReadByte() != 0;
			}
		}

		/* Not used here */
		public class LightKeyframe {
			public int FrameNumber;
			public Color LightColor;
			public Vector3 Position;

			public LightKeyframe() { }
			public LightKeyframe(BinaryReader binaryReader) { Read(binaryReader); }
			public void Read(BinaryReader binaryReader) {
				FrameNumber = binaryReader.ReadInt32();
				LightColor = new Color(binaryReader.ReadSingle(), binaryReader.ReadSingle(), binaryReader.ReadSingle(), 1);
				Position = VMD.ReadVector3(binaryReader);
			}
		}

		/* Not used here */
		public class SelfShadowKeyframe {
			public int FrameNumber;
			public byte Type;
			public float Distance;

			public SelfShadowKeyframe() { }
			public SelfShadowKeyframe(BinaryReader binaryReader) { Read(binaryReader); }
			public void Read(BinaryReader binaryReader)  {
				FrameNumber = binaryReader.ReadInt32();
				Type = binaryReader.ReadByte();
				Distance = binaryReader.ReadSingle();
			}
		}

		public class IKKeyframe {
			public int FrameNumber;
			public bool Display;
			public (string, bool)[] IKEnable;

			public IKKeyframe() { }
			public IKKeyframe(BinaryReader binaryReader) { Read(binaryReader); }
			public void Read(BinaryReader binaryReader) {
				FrameNumber = binaryReader.ReadInt32();
				Display = binaryReader.ReadByte() != 0;
				IKEnable = new (string, bool)[binaryReader.ReadInt32()];
				for(int i = 0; i < IKEnable.Length; i++)
					IKEnable[i] = (VMD.ReadString(binaryReader, 20), binaryReader.ReadByte() != 0);
			}

			public void WriteTo(BinaryWriter binaryWriter)
			{
				//Debug.Log($"Writing Bone : {Name} - {FrameNumber} - {StringProp(Position)} - {StringProp(Rotation)}");
				VMD.WriteInt(binaryWriter, FrameNumber);
				VMD.WriteByte(binaryWriter, (byte) (Display ? 1 : 0));
				VMD.WriteInt(binaryWriter, IKEnable.Length);
				foreach ((string, bool) ikState in IKEnable)
				{
					VMD.WriteSJISString(binaryWriter, ikState.Item1, 20);
					VMD.WriteByte(binaryWriter, (byte) (ikState.Item2 ? 1 : 0));
				}
			}
		}

		public string Version, Name;

		public List<BoneKeyframe> BoneKeyframes = new List<BoneKeyframe>();

		/* Not used here */
		public List<FaceKeyframe> FaceKeyframes = new List<FaceKeyframe>();

		/* Not used here */
		public List<CameraKeyframe> CameraKeyframes = new List<CameraKeyframe>();

		/* Not used here */
		public List<LightKeyframe> LightKeyframes = new List<LightKeyframe>();

		/* Not used here */
		public List<SelfShadowKeyframe> SelfShadowKeyframes = new List<SelfShadowKeyframe>();
		public List<IKKeyframe> IKKeyframes = new List<IKKeyframe>();

		string VMDVersion;
		public string VMDName;

		/* v1 is for the 32 bits version, v2 is for the 64 bits version */
		const string VMDHeaderv1 = "Vocaloid Motion Data file";
		const string VMDHeaderv2 = "Vocaloid Motion Data 0002";

		public VMD(string path) { Read(path); }

		public VMD() {
			VMDVersion = VMDHeaderv2;
			VMDName    = "miku";
		}

		private void Read(string path) {
			using(var fileStream = File.OpenRead(path))
			using(var binaryReader = new BinaryReader(fileStream))
				Read(binaryReader);
		}

		public void Write(string path)
		{
			using(var fileStream = File.OpenWrite(path))
			{
				using (var binaryWriter = new BinaryWriter(fileStream))
				{
					Write(binaryWriter);
				}
			}
		}

		public static byte[] UTFStringToSJISBytes(string name)
		{
			// Note, this might just generate a truncated SJIS character...
			Encoding sjis = Encoding.GetEncoding("Shift_JIS");
			return sjis.GetBytes(name);
		}

		public static void WriteSJISString(
			BinaryWriter writer,
			string utfString,
			int nBytesMax)
		{
			byte[] sjisBytes = UTFStringToSJISBytes(utfString);
			int nBytes = (sjisBytes.Length < nBytesMax ? sjisBytes.Length : nBytesMax);
			writer.Write(sjisBytes, 0, nBytes);
			for (int i = nBytes; i < nBytesMax; i++)
			{
				writer.Write((byte) 0);
			}
		}

		public static void WriteInt(BinaryWriter writer, int value)
		{
			writer.Write(value);
		}

		public static void WriteByte(BinaryWriter writer, byte value)
		{
			writer.Write(value);
		}

		public static void WriteVector3(BinaryWriter writer, Vector3 value)
		{
			writer.Write(value.x);
			writer.Write(value.y);
			writer.Write(value.z);
		}

		public static void WriteQuaternion(BinaryWriter writer, Quaternion quat)
		{
			writer.Write(quat.x);
			writer.Write(quat.y);
			writer.Write(quat.z);
			writer.Write(quat.w);
		}

		/* Not used here */
		public static void WriteInterpolation(
			BinaryWriter writer,
			(BezierInterpolator X, BezierInterpolator Y, BezierInterpolator Z, BezierInterpolator Rotation) interp)
		{
			/* FIXME (Voyage) :
			 * I guess you just have write sets of 4 bytes,
			 * with the first value being a bezier coordinate
			 * multiplied by 127.
			 * But, for the moment, let's forget about that and write zeros.
			 */
			for (int i = 0; i < 64; i++)
			{
				writer.Write((byte) 0);
			}
		}



		private void Write(BinaryWriter binaryWriter)
		{
			Debug.Log($"{VMDVersion} - {VMDName}");
			VMD.WriteSJISString(binaryWriter, VMDVersion, 30);
			if (VMDVersion == VMDHeaderv1)
				VMD.WriteSJISString(binaryWriter, VMDName, 10);
			else
				VMD.WriteSJISString(binaryWriter, VMDName, 20);
			VMD.WriteInt(binaryWriter, BoneKeyframes.Count);
			foreach (BoneKeyframe bk in BoneKeyframes)
			{
				bk.WriteTo(binaryWriter);
			}
			// Face 		/* Not used here */
			VMD.WriteInt(binaryWriter, 0);
			// Camera 		/* Not used here */
			VMD.WriteInt(binaryWriter, 0);
			// Light 		/* Not used here */
			VMD.WriteInt(binaryWriter, 0);
			// SelfShadow   /* Not used here */
			VMD.WriteInt(binaryWriter, 0);
			// IK 		    /* Not used here */
			VMD.WriteInt(binaryWriter, IKKeyframes.Count);
			foreach (IKKeyframe ikf in IKKeyframes)
			{
				ikf.WriteTo(binaryWriter);
			}

		}

		/* Not used here */
		private void Read(BinaryReader binaryReader) {
			Version = VMD.ReadString(binaryReader, 30);
			if(!Version.StartsWith("Vocaloid Motion Data")) {
				Debug.LogError("Invalid vmd file");
				return;
			}

			if (Version.StartsWith(VMDHeaderv1))			
				Name = VMD.ReadString(binaryReader, 10);
			else
				Name = VMD.ReadString(binaryReader, 20);
			//Debug.Log($"{Name} ({Version})");


			VMDVersion = Version;
			VMDName = Name;


			Debug.Log($"BEFORE {binaryReader.BaseStream.Position}");
			int boneFrameCount = binaryReader.ReadInt32();
			BoneKeyframes.Clear();
			Debug.Log("AFTER");

			Debug.Log(boneFrameCount);
			for(int i = 0; i < boneFrameCount; i++)
				BoneKeyframes.Add(new BoneKeyframe(binaryReader));

			if(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length)
				return;

			var faceFrameCount = binaryReader.ReadInt32();
			Debug.Log($"faceFrameCount={faceFrameCount}");
			FaceKeyframes.Clear();
			for(int i = 0; i < faceFrameCount; i++)
				FaceKeyframes.Add(new FaceKeyframe(binaryReader));
			
			try {
				if(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length)
					return;

				var cameraFrameCount = binaryReader.ReadInt32();
				if(cameraFrameCount != 0)
					Debug.Log($"cameraFrameCount={cameraFrameCount}");
				CameraKeyframes.Clear();
				for(int i = 0; i < cameraFrameCount; i++)
					CameraKeyframes.Add(new CameraKeyframe(binaryReader));
				
				if(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length)
					return;

				var lightFrameCount = binaryReader.ReadInt32();
				if(lightFrameCount != 0)
					Debug.Log($"lightFrameCount={lightFrameCount}");
				LightKeyframes.Clear();
				for(int i = 0; i < lightFrameCount; i++)
					LightKeyframes.Add(new LightKeyframe(binaryReader));
				
				if(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length)
					return;

				var selfShadowFrameCount = binaryReader.ReadInt32();
				if(selfShadowFrameCount != 0)
					Debug.Log($"selfShadowFrameCount={selfShadowFrameCount}");
				SelfShadowKeyframes.Clear();
				for(int i = 0; i < selfShadowFrameCount; i++)
					SelfShadowKeyframes.Add(new SelfShadowKeyframe(binaryReader));
				
				if(binaryReader.BaseStream.Position == binaryReader.BaseStream.Length)
					return;

				var ikFrameCount = binaryReader.ReadInt32();
				Debug.Log($"ikFrameCount={ikFrameCount}");
				IKKeyframes.Clear();
				for(int i = 0; i < ikFrameCount; i++)
					IKKeyframes.Add(new IKKeyframe(binaryReader));
			} catch {
				Debug.LogWarning("Error in parsing VMD");
			}
		}
		static Vector3 ReadVector3(BinaryReader binaryReader) {
			var x = binaryReader.ReadSingle();
			var y = binaryReader.ReadSingle();
			var z = binaryReader.ReadSingle();
			return new Vector3(x, y, z);
		}
		static Quaternion ReadQuaternion(BinaryReader binaryReader) {
			var x = binaryReader.ReadSingle();
			var y = binaryReader.ReadSingle();
			var z = binaryReader.ReadSingle();
			var w = binaryReader.ReadSingle();
			return new Quaternion(x, y, z, w);
		}
		static string ReadString(BinaryReader binaryReader, int length) {
			return System.Text.Encoding.GetEncoding("shift_jis").GetString(binaryReader.ReadBytes(length))
					.TrimEnd('\0').TrimEnd('?').TrimEnd('\0');
		}
		static BezierInterpolator ReadBezierXYXY(BinaryReader binaryReader, int stride) {
			return new BezierInterpolator{
				X0 = binaryReader.ReadBytes(stride)[0]/127f, Y0 = binaryReader.ReadBytes(stride)[0]/127f,
				X1 = binaryReader.ReadBytes(stride)[0]/127f, Y1 = binaryReader.ReadBytes(stride)[0]/127f};
		}
		static BezierInterpolator ReadBezierXXYY(BinaryReader binaryReader, int stride) {
			return new BezierInterpolator{
				X0 = binaryReader.ReadBytes(stride)[0]/127f, X1 = binaryReader.ReadBytes(stride)[0]/127f,
				Y0 = binaryReader.ReadBytes(stride)[0]/127f, Y1 = binaryReader.ReadBytes(stride)[0]/127f};
		}

		public void AddBoneFrame(
			string name,
			int frameNumber,
			Vector3 position,
			Quaternion rotation)
		{
			BoneKeyframe bkf = new BoneKeyframe();
			bkf.Name        = name;
			bkf.FrameNumber = frameNumber;
			bkf.Position    = position;
			bkf.Rotation    =  VMDRotation(rotation);
			bkf.Interp = (new BezierInterpolator(), new BezierInterpolator(), new BezierInterpolator(), new BezierInterpolator());
			BoneKeyframes.Add(bkf);
		}

		public void AddIKFrame(
			int frameNumber,
			bool isEnabled,
			params string[] names)
		{
			int nNames = names.Length;
			(string, bool)[] iksStates = new (string, bool)[nNames];
			for (int i = 0; i < nNames; i++)
			{
				iksStates[i] = (names[i], isEnabled);
			}
			IKKeyframes.Add(
				new IKKeyframe()
				{
					FrameNumber = frameNumber,
					Display = true,
					IKEnable = iksStates
				});

		}

	}

}
#endif