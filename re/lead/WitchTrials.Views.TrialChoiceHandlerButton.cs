using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Common.Attributes;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Naninovel.UI;

namespace WitchTrials.Views;

public class TrialChoiceHandlerButton : ChoiceHandlerButton
{
	private static readonly IntPtr NativeMethodInfoPtr__ctor_Public_Void_0;

	static TrialChoiceHandlerButton()
	{
		Il2CppClassPointerStore<TrialChoiceHandlerButton>.NativeClassPtr = IL2CPP.GetIl2CppClass("Assembly-CSharp.dll", "WitchTrials.Views", "TrialChoiceHandlerButton");
		IL2CPP.il2cpp_runtime_class_init(Il2CppClassPointerStore<TrialChoiceHandlerButton>.NativeClassPtr);
		NativeMethodInfoPtr__ctor_Public_Void_0 = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<TrialChoiceHandlerButton>.NativeClassPtr, 100664301);
	}

	[CallerCount(0)]
	[CachedScanResults(RefRangeStart = 0, RefRangeEnd = 0, XrefRangeStart = 0, XrefRangeEnd = 0, MetadataInitTokenRva = 0L, MetadataInitFlagRva = 0L)]
	public unsafe TrialChoiceHandlerButton()
		: this(IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<TrialChoiceHandlerButton>.NativeClassPtr))
	{
		IntPtr* ptr = null;
		Unsafe.SkipInit(out IntPtr intPtr2);
		IntPtr intPtr = IL2CPP.il2cpp_runtime_invoke(NativeMethodInfoPtr__ctor_Public_Void_0, IL2CPP.Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)(object)this), (void**)ptr, ref intPtr2);
		Il2CppException.RaiseExceptionIfNecessary(intPtr2);
	}

	public TrialChoiceHandlerButton(IntPtr pointer)
		: base(pointer)
	{
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.2.0.10050' (yours is '9.1.0.7988')
