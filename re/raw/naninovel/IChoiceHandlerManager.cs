using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using UnityEngine;

namespace Naninovel;

public class IChoiceHandlerManager : Il2CppObjectBase
{
	private static readonly IntPtr NativeMethodInfoPtr_get_ChoiceButtonLoader_Public_Abstract_Virtual_New_get_IResourceLoader_1_GameObject_0;

	private static readonly IntPtr NativeMethodInfoPtr_PushPickedChoice_Public_Abstract_Virtual_New_Void_PlaybackSpot_PlaybackSpot_0;

	private static readonly IntPtr NativeMethodInfoPtr_PopPickedChoice_Public_Abstract_Virtual_New_PlaybackSpot_PlaybackSpot_0;

	public unsafe virtual IResourceLoader<GameObject> ChoiceButtonLoader
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_get_ChoiceButtonLoader_Public_Abstract_Virtual_New_get_IResourceLoader_1_GameObject_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<IResourceLoader<GameObject>>(intPtr) : null;
		}
	}

	static IChoiceHandlerManager()
	{
		Il2CppClassPointerStore<IChoiceHandlerManager>.NativeClassPtr = IL2CPP.GetIl2CppClass("Elringus.Naninovel.Runtime.dll", "Naninovel", "IChoiceHandlerManager");
		NativeMethodInfoPtr_get_ChoiceButtonLoader_Public_Abstract_Virtual_New_get_IResourceLoader_1_GameObject_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<IChoiceHandlerManager>.NativeClassPtr, 100663614);
		NativeMethodInfoPtr_PushPickedChoice_Public_Abstract_Virtual_New_Void_PlaybackSpot_PlaybackSpot_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<IChoiceHandlerManager>.NativeClassPtr, 100663615);
		NativeMethodInfoPtr_PopPickedChoice_Public_Abstract_Virtual_New_PlaybackSpot_PlaybackSpot_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<IChoiceHandlerManager>.NativeClassPtr, 100663616);
	}

	[CallerCount(0)]
	public unsafe virtual void PushPickedChoice(PlaybackSpot hostedAt, PlaybackSpot continueAt)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = stackalloc IntPtr[2];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)hostedAt));
		*(IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(IntPtr)))) = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)continueAt));
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_PushPickedChoice_Public_Abstract_Virtual_New_Void_PlaybackSpot_PlaybackSpot_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(0)]
	public unsafe virtual PlaybackSpot PopPickedChoice(PlaybackSpot hostedAt)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = stackalloc IntPtr[1];
		*ptr = IL2CPP.il2cpp_object_unbox(IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)hostedAt));
		Unsafe.SkipInit(out IntPtr intPtr);
		IntPtr pointer = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_PopPickedChoice_Public_Abstract_Virtual_New_PlaybackSpot_PlaybackSpot_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr);
		Il2CppException.RaiseExceptionIfNecessary(intPtr);
		return new PlaybackSpot(pointer);
	}

	public IChoiceHandlerManager(IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
