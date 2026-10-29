using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Naninovel;
using WitchTrials.Models;

namespace WitchTrials.Views;

public class DebateTextPrinter : UITextPrinter
{
	private static readonly IntPtr NativeFieldInfoPtr__AnimationKind_k__BackingField;

	private static readonly IntPtr NativeMethodInfoPtr_get_AnimationKind_Public_Virtual_Final_New_get_DebateTextAnimationKind_0;

	private static readonly IntPtr NativeMethodInfoPtr_set_AnimationKind_Public_Virtual_Final_New_set_Void_DebateTextAnimationKind_0;

	private static readonly IntPtr NativeMethodInfoPtr__ctor_Public_Void_String_TextPrinterMetadata_0;

	public unsafe DebateTextAnimationKind _AnimationKind_k__BackingField
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__AnimationKind_k__BackingField);
			return *(DebateTextAnimationKind*)num;
		}
		set
		{
			*(DebateTextAnimationKind*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__AnimationKind_k__BackingField)) = debateTextAnimationKind;
		}
	}

	public unsafe virtual DebateTextAnimationKind AnimationKind
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_AnimationKind_Public_Virtual_Final_New_get_DebateTextAnimationKind_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return *(DebateTextAnimationKind*)IL2CPP.il2cpp_object_unbox(intPtr);
		}
		[CallerCount(0)]
		set
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = stackalloc IntPtr[1];
			*ptr = (nint)(&value);
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_set_AnimationKind_Public_Virtual_Final_New_set_Void_DebateTextAnimationKind_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		}
	}

	static DebateTextPrinter()
	{
		Il2CppClassPointerStore<DebateTextPrinter>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "WitchTrials.Views", "DebateTextPrinter");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<DebateTextPrinter>.NativeClassPtr);
		NativeFieldInfoPtr__AnimationKind_k__BackingField = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DebateTextPrinter>.NativeClassPtr, "<AnimationKind>k__BackingField");
		NativeMethodInfoPtr_get_AnimationKind_Public_Virtual_Final_New_get_DebateTextAnimationKind_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DebateTextPrinter>.NativeClassPtr, 100663436);
		NativeMethodInfoPtr_set_AnimationKind_Public_Virtual_Final_New_set_Void_DebateTextAnimationKind_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DebateTextPrinter>.NativeClassPtr, 100663437);
		NativeMethodInfoPtr__ctor_Public_Void_String_TextPrinterMetadata_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<DebateTextPrinter>.NativeClassPtr, 100663438);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 625991, XrefRangeEnd = 625992, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe DebateTextPrinter(string id, TextPrinterMetadata meta)
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<DebateTextPrinter>.NativeClassPtr))
	{
		IntPtr* ptr = stackalloc IntPtr[2];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(id);
		*(IntPtr*)((byte*)ptr + checked((nuint)1u * unchecked((nuint)sizeof(IntPtr)))) = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)meta);
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_String_TextPrinterMetadata_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	public DebateTextPrinter(IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
