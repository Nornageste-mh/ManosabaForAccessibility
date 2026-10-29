using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem;
using Naninovel.UI;

namespace GigaCreation.NaninovelExtender.Ui;

[System.Serializable]
public class CustomChoiceButtonData : Il2CppSystem.Object
{
	private static readonly System.IntPtr NativeFieldInfoPtr__path;

	private static readonly System.IntPtr NativeFieldInfoPtr__customButtonPrefab;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_Path_Public_get_String_0;

	private static readonly System.IntPtr NativeMethodInfoPtr_get_CustomButtonPrefab_Public_get_ChoiceHandlerButton_0;

	private static readonly System.IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

	public unsafe string _path
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__path);
			return IL2CPP.Il2CppStringToManaged(*(System.IntPtr*)num);
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__path)), IL2CPP.ManagedStringToIl2Cpp(text));
		}
	}

	public unsafe ChoiceHandlerButton _customButtonPrefab
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__customButtonPrefab);
			System.IntPtr intPtr = *(System.IntPtr*)num;
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ChoiceHandlerButton>(intPtr) : null;
		}
		set
		{
			System.IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (System.IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__customButtonPrefab)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)choiceHandlerButton));
		}
	}

	public unsafe string Path
	{
		[CallerCount(2)]
		[CachedScanResults(RefRangeStart = 374, RefRangeEnd = 376, XrefRangeStart = 374, XrefRangeEnd = 376, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			System.IntPtr* ptr = null;
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_Path_Public_get_String_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return IL2CPP.Il2CppStringToManaged(intPtr);
		}
	}

	public unsafe ChoiceHandlerButton CustomButtonPrefab
	{
		[CallerCount(6)]
		[CachedScanResults(RefRangeStart = 154, RefRangeEnd = 160, XrefRangeStart = 154, XrefRangeEnd = 160, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			System.IntPtr* ptr = null;
			Unsafe.SkipInit(out System.IntPtr intPtr2);
			System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_CustomButtonPrefab_Public_get_ChoiceHandlerButton_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return (intPtr != (System.IntPtr)0) ? Il2CppObjectPool.Get<ChoiceHandlerButton>(intPtr) : null;
		}
	}

	static CustomChoiceButtonData()
	{
		Il2CppClassPointerStore<CustomChoiceButtonData>.NativeClassPtr = IL2CPP.GetIl2CppClass("GigaCreation.NaninovelExtender.UI.dll", "GigaCreation.NaninovelExtender.Ui", "CustomChoiceButtonData");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<CustomChoiceButtonData>.NativeClassPtr);
		NativeFieldInfoPtr__path = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CustomChoiceButtonData>.NativeClassPtr, "_path");
		NativeFieldInfoPtr__customButtonPrefab = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<CustomChoiceButtonData>.NativeClassPtr, "_customButtonPrefab");
		NativeMethodInfoPtr_get_Path_Public_get_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<CustomChoiceButtonData>.NativeClassPtr, 100663323);
		NativeMethodInfoPtr_get_CustomButtonPrefab_Public_get_ChoiceHandlerButton_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<CustomChoiceButtonData>.NativeClassPtr, 100663324);
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<CustomChoiceButtonData>.NativeClassPtr, 100663325);
	}

	[CallerCount(0)]
	public unsafe CustomChoiceButtonData()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<CustomChoiceButtonData>.NativeClassPtr))
	{
		System.IntPtr* ptr = null;
		Unsafe.SkipInit(out System.IntPtr intPtr2);
		System.IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	public CustomChoiceButtonData(System.IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
