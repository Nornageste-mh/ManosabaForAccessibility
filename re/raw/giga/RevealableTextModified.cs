using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Runtime;
using Il2CppSystem.Text;
using Il2CppSystem.Text.RegularExpressions;
using Naninovel.UI;

namespace GigaCreation.NaninovelExtender.Ui;

public class RevealableTextModified : RevealableText
{
	private static readonly IntPtr NativeFieldInfoPtr_RubyLinkId;

	private static readonly IntPtr NativeFieldInfoPtr_LineBreakTag;

	private static readonly IntPtr NativeFieldInfoPtr_s_captureRubyRegex;

	private static readonly IntPtr NativeFieldInfoPtr__marginTopWhenFirstLineHasRuby;

	private static readonly IntPtr NativeFieldInfoPtr__stringBuilder;

	private static readonly IntPtr NativeMethodInfoPtr_get_AdditionalSpaceWidth_Private_get_Single_0;

	private static readonly IntPtr NativeMethodInfoPtr_ProcessRubyTags_Protected_Virtual_String_String_0;

	private static readonly IntPtr NativeMethodInfoPtr_UpdateMargin_Private_Void_Boolean_0;

	private static readonly IntPtr NativeMethodInfoPtr_GetIndexOfEndOfFirstLine_Private_Static_Int32_String_0;

	private static readonly IntPtr NativeMethodInfoPtr_PrependLineHeightTagToAllLines_Private_Static_String_String_0;

	private static readonly IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

	public unsafe static string RubyLinkId
	{
		get
		{
			Unsafe.SkipInit(out IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_RubyLinkId, (void*)(&intPtr));
			return IL2CPP.Il2CppStringToManaged(intPtr);
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_RubyLinkId, (void*)IL2CPP.ManagedStringToIl2Cpp(text));
		}
	}

	public unsafe static string LineBreakTag
	{
		get
		{
			Unsafe.SkipInit(out IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_LineBreakTag, (void*)(&intPtr));
			return IL2CPP.Il2CppStringToManaged(intPtr);
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_LineBreakTag, (void*)IL2CPP.ManagedStringToIl2Cpp(text));
		}
	}

	public unsafe static Regex s_captureRubyRegex
	{
		get
		{
			Unsafe.SkipInit(out IntPtr intPtr);
			IL2CPP.il2cpp_field_static_get_value(NativeFieldInfoPtr_s_captureRubyRegex, (void*)(&intPtr));
			IntPtr intPtr2 = intPtr;
			return (intPtr2 != (IntPtr)0) ? Il2CppObjectPool.Get<Regex>(intPtr2) : null;
		}
		set
		{
			IL2CPP.il2cpp_field_static_set_value(NativeFieldInfoPtr_s_captureRubyRegex, (void*)IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)regex));
		}
	}

	public unsafe float _marginTopWhenFirstLineHasRuby
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__marginTopWhenFirstLineHasRuby);
			return *(float*)num;
		}
		set
		{
			*(float*)((nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__marginTopWhenFirstLineHasRuby)) = num;
		}
	}

	public unsafe StringBuilder _stringBuilder
	{
		get
		{
			nint num = (nint)IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this) + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__stringBuilder);
			IntPtr intPtr = *(IntPtr*)num;
			return (intPtr != (IntPtr)0) ? Il2CppObjectPool.Get<StringBuilder>(intPtr) : null;
		}
		set
		{
			IntPtr num = IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IL2CPP.il2cpp_gc_wbarrier_set_field(num, (IntPtr)((nint)num + (int)IL2CPP.il2cpp_field_get_offset(NativeFieldInfoPtr__stringBuilder)), IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)stringBuilder));
		}
	}

	public unsafe float AdditionalSpaceWidth
	{
		[CallerCount(0)]
		get
		{
			IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
			IntPtr* ptr = null;
			Unsafe.SkipInit(out IntPtr intPtr2);
			IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_get_AdditionalSpaceWidth_Private_get_Single_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
			Il2CppException.RaiseExceptionIfNecessary(intPtr2);
			return *(float*)IL2CPP.il2cpp_object_unbox(intPtr);
		}
	}

	static RevealableTextModified()
	{
		Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr = IL2CPP.GetIl2CppClass("GigaCreation.NaninovelExtender.UI.dll", "GigaCreation.NaninovelExtender.Ui", "RevealableTextModified");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr);
		NativeFieldInfoPtr_RubyLinkId = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr, "RubyLinkId");
		NativeFieldInfoPtr_LineBreakTag = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr, "LineBreakTag");
		NativeFieldInfoPtr_s_captureRubyRegex = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr, "s_captureRubyRegex");
		NativeFieldInfoPtr__marginTopWhenFirstLineHasRuby = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr, "_marginTopWhenFirstLineHasRuby");
		NativeFieldInfoPtr__stringBuilder = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr, "_stringBuilder");
		NativeMethodInfoPtr_get_AdditionalSpaceWidth_Private_get_Single_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr, 100663464);
		NativeMethodInfoPtr_ProcessRubyTags_Protected_Virtual_String_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr, 100663465);
		NativeMethodInfoPtr_UpdateMargin_Private_Void_Boolean_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr, 100663466);
		NativeMethodInfoPtr_GetIndexOfEndOfFirstLine_Private_Static_Int32_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr, 100663467);
		NativeMethodInfoPtr_PrependLineHeightTagToAllLines_Private_Static_String_String_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr, 100663468);
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr, 100663469);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 926441, XrefRangeEnd = 926528, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe override string ProcessRubyTags(string content)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = stackalloc IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(content);
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(IL2CPP.il2cpp_object_get_virtual_method(IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)(object)this), NativeMethodInfoPtr_ProcessRubyTags_Protected_Virtual_String_String_0), IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		return IL2CPP.Il2CppStringToManaged(intPtr);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 926528, XrefRangeEnd = 926529, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe void UpdateMargin(bool hasRubyInFirstLine)
	{
		IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this);
		IntPtr* ptr = stackalloc IntPtr[1];
		*ptr = (nint)(&hasRubyInFirstLine);
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_UpdateMargin_Private_Void_Boolean_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	[CallerCount(1)]
	[CachedScanResults(RefRangeStart = 926537, RefRangeEnd = 926538, XrefRangeStart = 926529, XrefRangeEnd = 926537, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static int GetIndexOfEndOfFirstLine(string content)
	{
		IntPtr* ptr = stackalloc IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(content);
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_GetIndexOfEndOfFirstLine_Private_Static_Int32_String_0, (IntPtr)0, (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		return *(int*)IL2CPP.il2cpp_object_unbox(intPtr);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 926538, XrefRangeEnd = 926546, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe static string PrependLineHeightTagToAllLines(string content)
	{
		IntPtr* ptr = stackalloc IntPtr[1];
		*ptr = IL2CPP.ManagedStringToIl2Cpp(content);
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr_PrependLineHeightTagToAllLines_Private_Static_String_String_0, (IntPtr)0, (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
		return IL2CPP.Il2CppStringToManaged(intPtr);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 926546, XrefRangeEnd = 926560, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe RevealableTextModified()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<RevealableTextModified>.NativeClassPtr))
	{
		IntPtr* ptr = null;
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	public RevealableTextModified(IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
