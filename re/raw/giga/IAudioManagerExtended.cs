using System;
using System.Runtime.CompilerServices;
using GigaCreation.Essentials.Input;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;

namespace GigaCreation.NaninovelExtender.Audio;

public class IAudioManagerExtended : Il2CppObjectBase
{
	private static readonly IntPtr NativeMethodInfoPtr_PlayResponseSfx_Public_Abstract_Virtual_New_Void_ResponseKind_0;

	static IAudioManagerExtended()
	{
		Il2CppClassPointerStore<IAudioManagerExtended>.NativeClassPtr = IL2CPP.GetIl2CppClass("GigaCreation.NaninovelExtender.Audio.dll", "GigaCreation.NaninovelExtender.Audio", "IAudioManagerExtended");
		NativeMethodInfoPtr_PlayResponseSfx_Public_Abstract_Virtual_New_Void_ResponseKind_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<IAudioManagerExtended>.NativeClassPtr, 100663310);
	}

	[CallerCount(0)]
	public unsafe virtual void PlayResponseSfx(ResponseKind responseKind)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = stackalloc IntPtr[1];
		*ptr = (nint)(&responseKind);
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_PlayResponseSfx_Public_Abstract_Virtual_New_Void_ResponseKind_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	public IAudioManagerExtended(IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
