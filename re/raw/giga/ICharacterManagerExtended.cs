using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem.Collections.Generic;

namespace GigaCreation.NaninovelExtender.ExtendedActors;

public class ICharacterManagerExtended : Il2CppObjectBase
{
	private static readonly IntPtr NativeMethodInfoPtr_get_ScaleCoefficients_Public_Abstract_Virtual_New_get_IReadOnlyDictionary_2_String_Single_0;

	public unsafe virtual IReadOnlyDictionary<string, float> ScaleCoefficients
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_get_ScaleCoefficients_Public_Abstract_Virtual_New_get_IReadOnlyDictionary_2_String_Single_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<IReadOnlyDictionary<string, float>>(intPtr) : null;
		}
	}

	static ICharacterManagerExtended()
	{
		Il2CppClassPointerStore<ICharacterManagerExtended>.NativeClassPtr = IL2CPP.GetIl2CppClass("GigaCreation.NaninovelExtender.ExtendedActors.dll", "GigaCreation.NaninovelExtender.ExtendedActors", "ICharacterManagerExtended");
		NativeMethodInfoPtr_get_ScaleCoefficients_Public_Abstract_Virtual_New_get_IReadOnlyDictionary_2_String_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<ICharacterManagerExtended>.NativeClassPtr, 100663362);
	}

	public ICharacterManagerExtended(IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
