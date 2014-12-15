﻿using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

namespace CsSandbox.Sandbox
{
	[Serializable]
	public class Sandboxer : MarshalByRefObject
	{

		public Tuple<LimitedStringWriter, LimitedStringWriter> ExecuteUntrustedCode(MethodInfo entryPoint, TextReader stdin)
		{
			var stdout = new LimitedStringWriter();
			var stderr = new LimitedStringWriter();
			(new PermissionSet(PermissionState.Unrestricted)).Assert(); // Need to setup streams and invoke non-public Main in non-public class
			Console.SetIn(stdin);
			Console.SetOut(stdout);
			Console.SetError(stderr);
			entryPoint.Invoke(null, null);
			CodeAccessPermission.RevertAssert();
			return Tuple.Create(stdout, stderr);
		}

	}
}